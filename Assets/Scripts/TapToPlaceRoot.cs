using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;


[RequireComponent(typeof(ARRaycastManager))]
public class TapToPlaceRoot : MonoBehaviour
{
    [Tooltip("Prefab that contains your visualization root")]
    public GameObject objectToPlace;

    [Header("Plane / Scanning visuals")]
    public ARPlaneManager planeManager;      // drag from XR Origin
    public GameObject scanningUI;           // optional overlay / text

    private ARRaycastManager _raycastManager;
    private static readonly List<ARRaycastHit> Hits = new();

    private GameObject _spawnedObject;

    // Data for pinch / rotate gestures
    private float _initialDistance;
    private Vector3 _initialScale;
    private float _initialRotationOffsetY;

    // Hold on ground to Move(respawn) Plot
    [Header("Long-press move")]
    public float longPressDuration = 2.0f;   // seconds to hold before moving

    private bool _holdCandidate;             // finger is down and could become a long-press
    private int _holdFingerId = -1;
    private float _touchStartTime;
    private Vector2 _touchStartPos;
    public float maxTapMove = 15f;         

    private void Awake()
    {
        _raycastManager = GetComponent<ARRaycastManager>();

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
    }

    private void Update()
    {
        int touchCount = Input.touchCount;
        if (touchCount == 0)
        {
            // reset on no touches
            _holdCandidate = false;
            _holdFingerId = -1;
            return;
        }

        //  Scale or Rotate : using 2Fingers
        if (touchCount == 2 && _spawnedObject != null)
        {
            // If a second finger appears, cancel any pending long-press
            _holdCandidate = false;
            _holdFingerId = -1;

            HandleTwoFingerGesture(Input.GetTouch(0), Input.GetTouch(1));
            return;
        }

        // ONE FINGER: TAP (spawn once) + LONG PRESS (move)
        if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Ignore if starting over UI
            if (touch.phase == TouchPhase.Began &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    HandleTouchHold(touch);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnd(touch);
                    break;
            }
        }
    }

    //  SINGLE FINGER LOGIC 
    private void HandleTouchBegan(Touch touch)
    {
        // First ever placement: simple tap on plane
        if (_spawnedObject == null)
        {
            TryPlace(touch.position);
        }

        // Start tracking this finger as a potential long-press
        _holdCandidate = true;
        _holdFingerId = touch.fingerId;
        _touchStartTime = Time.time;
        _touchStartPos = touch.position;
    }

    private void HandleTouchHold(Touch touch)
    {
        if (!_holdCandidate || touch.fingerId != _holdFingerId)
            return;

        if (_spawnedObject == null)
            return; // nothing to move yet

        // If finger moved too far, cancel long-press
        if (Vector2.Distance(touch.position, _touchStartPos) > maxTapMove)
        {
            _holdCandidate = false;
            _holdFingerId = -1;
            return;
        }

        // Check if long-press threshold is passed
        if (Time.time - _touchStartTime >= longPressDuration)
        {
            // Long-press recognized -> move the plot under this finger
            MoveRootTo(touch.position);
        }
    }

    private void HandleTouchEnd(Touch touch)
    {
        if (touch.fingerId == _holdFingerId)
        {
            _holdCandidate = false;
            _holdFingerId = -1;
        }
    }

    // ---------------- AR RAYCAST HELPERS ----------------

    private void TryPlace(Vector2 screenPosition)
    {
        if (objectToPlace == null)
        {
            Debug.LogError("TapToPlaceRoot: objectToPlace is not set!");
            return;
        }

        if (_raycastManager.Raycast(screenPosition, Hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = Hits[0].pose;
            if (_spawnedObject == null)
            {
                _spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                Debug.Log("[TapToPlaceRoot] Plot spawned at " + hitPose.position);

                //make Cubes selectable after spawning
                 StartCoroutine(EnableSelectionNextFrame());
                DisableScanningVisuals();
            }

        }
    }
    //Method for disable Scanning surface
    private void DisableScanningVisuals()
    {
        // Hide extra overlay (e.g. "Move your device" panel)
        if (scanningUI != null)
            scanningUI.SetActive(false);

        if (planeManager == null)
            return;

        // Hide all currently detected plane GameObjects
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        // Option A: completely stop plane updates (no more scanning)
        planeManager.enabled = false;

        // If later you decide you still want plane updates for some reason,
        // comment out the line above and only keep the plane meshes disabled.
    }

    private void MoveRootTo(Vector2 screenPosition)
    {
        if (_raycastManager.Raycast(screenPosition, Hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = Hits[0].pose;
            if (_spawnedObject != null)
            {
                _spawnedObject.transform.position = hitPose.position;
                // Keep current rotation (set by user via 2-fingers twist)
                Debug.Log("[TapToPlaceRoot] Plot moved to " + hitPose.position);
            }
        }
    }

    // TWO-FINGER SCALE / ROTATE 

    private void HandleTwoFingerGesture(Touch t0, Touch t1)
    {
        if (_spawnedObject == null) return;

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            _initialDistance = Vector2.Distance(t0.position, t1.position);
            _initialScale = _spawnedObject.transform.localScale;

            float touchesAngle = GetTouchesAngle(t0.position, t1.position);
            _initialRotationOffsetY = touchesAngle - _spawnedObject.transform.eulerAngles.y;
        }
        else
        {
            // Pinch to scale
            float currentDistance = Vector2.Distance(t0.position, t1.position);
            if (_initialDistance > 0.01f)
            {
                float scaleFactor = currentDistance / _initialDistance;
                _spawnedObject.transform.localScale = _initialScale * scaleFactor;
            }

            // Rotate by twisting fingers
            float angle = GetTouchesAngle(t0.position, t1.position);
            float targetY = angle - _initialRotationOffsetY;
            _spawnedObject.transform.rotation = Quaternion.Euler(0f, targetY, 0f);
        }
    }

    // Angle between two touch positions, in degrees
    private float GetTouchesAngle(Vector2 p0, Vector2 p1)
    {
        Vector2 dir = p1 - p0;
        return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
    }
    private IEnumerator EnableSelectionNextFrame()
    {
        // wait for a frame - after touching is done
        yield return null;   

        var selector = Object.FindFirstObjectByType<DataPointSelector>();
        if (selector != null)
        {
            selector.gameObject.SetActive(true);   
            selector.EnableSelection();           
            Debug.Log("[TapToPlaceRoot] DataPointSelector enabled.");
        }
    }
}