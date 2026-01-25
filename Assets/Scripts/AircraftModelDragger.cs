using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AircraftModelDragger : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public ARRaycastManager raycastManager;

    [Header("Drag start")]
    [Tooltip("Hold time (seconds) before we force drag start. Drag can also start earlier if you move your finger.")]
    public float longPressTime = 0.35f;

    [Tooltip("Screen pixels of movement that will also start a drag, even if longPressTime has not elapsed.")]
    public float dragStartPixels = 15f;

    [Header("Tap-to-delete")]
    public bool tapDeletesModel = true;

    [Tooltip("Max time (seconds) for a tap to count as delete.")]
    public float tapMaxTime = 0.25f;

    [Tooltip("Max movement (pixels) for a tap to count as delete.")]
    public float tapMaxMovement = 10f;

    // Global flag so TapToPlaceRoot / DataPointSelector can ignore touches during model gestures
    public static bool AnyModelInGesture { get; private set; }

    // --- press / drag state ---
    private bool _isPressing;
    private bool _isDragging;
    private bool _dragStarted;
    private int _fingerId = -1;

    private float _pressStartTime;
    private Vector2 _pressStartPos;
    private Vector2 _lastTouchPos;
    private bool _hadMultiTouch;

    // --- pinch / rotate state ---
    private bool _pinchActive;
    private float _initialPinchDistance;
    private Vector3 _initialScale;
    private float _initialTwistAngle;
    private float _initialYRotation;

    private static readonly List<ARRaycastHit> Hits = new();

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
    }

    // Called from AircraftDetailCard when we spawn the model
    public void Init(Camera cam, ARRaycastManager ar)
    {
        if (cam != null) mainCamera = cam;
        if (ar != null) raycastManager = ar;
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // =========================================================
    //  MOUSE (Editor) – simple long-press drag
    // =========================================================
    private void HandleMouse()
    {
        if (mainCamera == null) return;

        if (Input.GetMouseButtonDown(0) && !_isPressing)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (HitThisModel(ray))
            {
                _isPressing = true;
                _pressStartTime = Time.time;
                _pressStartPos = Input.mousePosition;
                _lastTouchPos = _pressStartPos;
                _dragStarted = false;
            }
        }

        if (_isPressing && Input.GetMouseButton(0))
        {
            _lastTouchPos = Input.mousePosition;

            if (!_dragStarted)
            {
                float held = Time.time - _pressStartTime;
                float moved = Vector2.Distance(_pressStartPos, _lastTouchPos);
                if (held >= longPressTime || moved >= dragStartPixels)
                {
                    BeginDrag();
                }
            }

            if (_isDragging)
            {
                UpdateDrag(Input.mousePosition);
            }
        }

        if (_isPressing && Input.GetMouseButtonUp(0))
        {
            EndPress();   // in editor we don’t use tap-to-delete
        }
    }

    // =========================================================
    //  TOUCH (device) – drag + pinch + tap delete
    // =========================================================
    private void HandleTouch()
    {
        if (mainCamera == null) return;
        if (Input.touchCount == 0) return;

        // If at any time during the press there is more than 1 touch,
        // we remember that so this cannot count as a "tap to delete".
        if (_isPressing && Input.touchCount > 1)
            _hadMultiTouch = true;

        foreach (var touch in Input.touches)
        {
            // --- start press ---
            if (touch.phase == TouchPhase.Began && !_isPressing)
            {
                Ray ray = mainCamera.ScreenPointToRay(touch.position);
                if (HitThisModel(ray))
                {
                    _isPressing = true;
                    _fingerId = touch.fingerId;
                    _pressStartTime = Time.time;
                    _pressStartPos = touch.position;
                    _lastTouchPos = touch.position;
                    _dragStarted = false;
                    _hadMultiTouch = false;
                }
            }
            // --- continue press / drag with same finger ---
            else if (_isPressing && touch.fingerId == _fingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    _lastTouchPos = touch.position;

                    // decide when drag really begins
                    if (!_dragStarted)
                    {
                        float held = Time.time - _pressStartTime;
                        float moved = Vector2.Distance(_pressStartPos, _lastTouchPos);

                        if (held >= longPressTime || moved >= dragStartPixels)
                        {
                            BeginDrag();
                        }
                    }

                    if (_isDragging)
                    {
                        UpdateDrag(touch.position);
                        HandlePinchAndRotate();   // look for second finger
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndPress();
                }
            }
        }
    }

    // =========================================================
    //  CORE HELPERS
    // =========================================================
    private bool HitThisModel(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    private void BeginDrag()
    {
        _dragStarted = true;
        _isDragging = true;
        AnyModelInGesture = true;

        // detach from detail card so it becomes an AR object
        transform.SetParent(null, true);

        _pinchActive = false;

        Debug.Log("[AircraftModelDragger] Begin drag: " + gameObject.name);
    }

    private void UpdateDrag(Vector2 screenPos)
    {
        if (raycastManager != null &&
            raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = Hits[0].pose;
            transform.position = pose.position;
            transform.rotation = pose.rotation;
        }
        else if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            transform.position = ray.origin + ray.direction * 0.5f;
        }
    }

    private void HandlePinchAndRotate()
    {
        if (Input.touchCount < 2)
        {
            _pinchActive = false;
            return;
        }

        // primary = our drag finger, secondary = any other finger
        Touch? primary = null;
        Touch? secondary = null;

        foreach (var t in Input.touches)
        {
            if (t.fingerId == _fingerId)
                primary = t;
            else if (secondary == null)
                secondary = t;
        }

        if (primary == null || secondary == null)
        {
            _pinchActive = false;
            return;
        }

        var t0 = primary.Value;
        var t1 = secondary.Value;

        float currentDistance = Vector2.Distance(t0.position, t1.position);
        float currentAngle = GetTouchesAngle(t0.position, t1.position);

        if (!_pinchActive)
        {
            _pinchActive = true;
            _initialPinchDistance = currentDistance;
            _initialScale = transform.localScale;
            _initialTwistAngle = currentAngle;
            _initialYRotation = transform.eulerAngles.y;
            return;
        }

        // scale
        if (_initialPinchDistance > 1f)
        {
            float scaleFactor = currentDistance / _initialPinchDistance;
            transform.localScale = _initialScale * scaleFactor;
        }

        // rotate around Y
        float angleDelta = currentAngle - _initialTwistAngle;
        float targetY = _initialYRotation + angleDelta;
        transform.rotation = Quaternion.Euler(0f, targetY, 0f);
    }

    private float GetTouchesAngle(Vector2 p0, Vector2 p1)
    {
        Vector2 dir = p1 - p0;
        return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
    }

    private void EndPress()
    {
        bool wasTap =
            !_dragStarted &&
            !_hadMultiTouch &&
            tapDeletesModel &&
            (Time.time - _pressStartTime <= tapMaxTime) &&
            (Vector2.Distance(_pressStartPos, _lastTouchPos) <= tapMaxMovement);

        if (wasTap)
        {
            Debug.Log("[AircraftModelDragger] Tap => destroy model: " + gameObject.name);
            Destroy(gameObject);
        }
        else if (_isDragging)
        {
            Debug.Log("[AircraftModelDragger] End drag, model dropped at: " + transform.position);
        }

        _isPressing = false;
        _isDragging = false;
        _dragStarted = false;
        _pinchActive = false;
        _fingerId = -1;
        _pressStartTime = 0f;
        _hadMultiTouch = false;

        AnyModelInGesture = false;
    }
}
