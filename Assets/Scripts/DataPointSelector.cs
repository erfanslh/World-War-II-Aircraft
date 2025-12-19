using UnityEngine;

public class DataPointSelector : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject detailCardPrefab;

    private AircraftDetailCard _currentCard;

    private bool _selectionEnabled = false;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void EnableSelection()
    {
        _selectionEnabled = true;
    }

    private void Update()
    {
        if (!_selectionEnabled)
            return;

#if UNITY_EDITOR
        // in Editor 
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            HandleRay(ray);
        }
#else
        // on Device
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = mainCamera.ScreenPointToRay(touch.position);
                HandleRay(ray);
            }
        }
#endif
    }

    private void HandleRay(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var dataPoint = hit.collider.GetComponentInParent<AircraftDataPoint>();
            if (dataPoint != null && dataPoint.record != null)
            {
                ShowDetails(dataPoint, hit.point, hit.normal);
            }
        }
    }

    private void ShowDetails(AircraftDataPoint point, Vector3 hitPos, Vector3 hitNormal)
    {
        if (detailCardPrefab == null)
        {
            Debug.LogWarning("[DataPointSelector] No DetailCard prefab assigned.");
            return;
        }

        if (_currentCard != null)
            Destroy(_currentCard.gameObject);

        var camTransform = mainCamera != null ? mainCamera.transform : Camera.main.transform;

        const float surfaceOffset = 0.02f;
        const float forwardOffset = 0.20f;

        Vector3 spawnPos =
            hitPos
            + hitNormal.normalized * surfaceOffset
            + camTransform.forward.normalized * forwardOffset;

        GameObject cardGO = Instantiate(detailCardPrefab, spawnPos, Quaternion.identity);

        Vector3 toCam = camTransform.position - spawnPos;
        Vector3 forwardOnSurface = Vector3.ProjectOnPlane(toCam, hitNormal);
        if (forwardOnSurface.sqrMagnitude < 0.0001f)
            forwardOnSurface = camTransform.forward;

        cardGO.transform.rotation = Quaternion.LookRotation(forwardOnSurface, hitNormal);

        _currentCard = cardGO.GetComponent<AircraftDetailCard>();
        if (_currentCard != null)
        {
            _currentCard.Setup(point.record, mainCamera);
        }
        else
        {
            Debug.LogWarning("[DataPointSelector] DetailCard prefab is missing AircraftDetailCard component.");
        }
    }
}