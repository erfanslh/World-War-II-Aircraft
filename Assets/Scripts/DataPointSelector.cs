using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DataPointSelector : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject detailCardPrefab;

    private readonly Dictionary<AircraftDataPoint, AircraftDetailCard> _openCards =
            new Dictionary<AircraftDataPoint, AircraftDetailCard>();

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
        // Mouse click in Editor
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            HandleRay(ray);
        }
#else
    // Touch on device
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
                ToggleSelection(dataPoint, hit.point, hit.normal);
            }
        }
    }
    #region ToggleSelection Logic[multiple selection Card]
    // If this point is already selected, unselect it (remove highlight + close card).
    // If not selected yet, highlight it and spawn a new detail card.

    private void ToggleSelection(AircraftDataPoint point, Vector3 hitPos, Vector3 hitNormal)
    {
        // already have a card for this point -> close it (unselect)
        if (_openCards.TryGetValue(point, out var existingCard) && existingCard != null)
        {
            Debug.Log("[DataPointSelector] Toggling OFF selection: " + point.record.Name);

            point.SetHighlighted(false);
            Destroy(existingCard.gameObject);
            _openCards.Remove(point);
            return;
        }

        //NewSelection
        Debug.Log("[DataPointSelector] Toggling ON selection: " + point.record.Name);

        point.SetHighlighted(true);
        var newCard = CreateCard(point, hitPos, hitNormal);
        if (newCard != null)
        {
            _openCards[point] = newCard;
        }
    }
    #endregion
    private AircraftDetailCard CreateCard(AircraftDataPoint point, Vector3 hitPos, Vector3 hitNormal)
    {
        if (detailCardPrefab == null)
        {
            Debug.LogWarning("[DataPointSelector] No DetailCard prefab assigned.");
            return null;
        }

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

        var card = cardGO.GetComponent<AircraftDetailCard>();
        if (card != null)
        {
            card.Setup(point.record, mainCamera);
        }
        else
        {
            Debug.LogWarning("[DataPointSelector] DetailCard prefab is missing AircraftDetailCard component.");
        }

        return card;
    }

    // Clear Highlight when disabled
    private void OnDisable()
    {
        // clean up highlights & cards if selector gets disabled
        foreach (var kvp in _openCards)
        {
            var point = kvp.Key;
            var card = kvp.Value;

            if (point != null)
                point.SetHighlighted(false);

            if (card != null)
                Destroy(card.gameObject);
        }

        _openCards.Clear();
    }
}