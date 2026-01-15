#region   OldCode

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;

//public class DataPointSelector : MonoBehaviour
//{
//    public Camera mainCamera;
//    public GameObject detailCardPrefab;

//    // Layout for multiple cards in front of the camera
//    [Header("Card layout")]
//    public float cardDistance = 0.7f;         // how far in front of the camera (meters)
//    public float cardHorizontalSpacing = 0.35f;
//    public float cardVerticalSpacing = 0.3f;
//    public int cardsPerRow = 3;

//    private readonly Dictionary<AircraftDataPoint, AircraftDetailCard> _openCards =
//            new Dictionary<AircraftDataPoint, AircraftDetailCard>();

//    private bool _selectionEnabled = false;

//    private void Awake()
//    {
//        if (mainCamera == null)
//            mainCamera = Camera.main;
//    }

//    public void EnableSelection()
//    {
//        _selectionEnabled = true;
//    }
//    private void Update()
//    {
//        if (!_selectionEnabled)
//            return;

//#if UNITY_EDITOR
//        // Mouse click in Editor
//        if (Input.GetMouseButtonDown(0))
//        {
//            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
//            HandleRay(ray);
//        }
//#else
//    // Touch on device
//    if (Input.touchCount > 0)
//    {
//        var touch = Input.GetTouch(0);
//        if (touch.phase == TouchPhase.Began)
//        {
//            Ray ray = mainCamera.ScreenPointToRay(touch.position);
//            HandleRay(ray);
//        }
//    }
//#endif
//    }

//    private void HandleRay(Ray ray)
//    {
//        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
//        {
//            var dataPoint = hit.collider.GetComponentInParent<AircraftDataPoint>();
//            if (dataPoint != null && dataPoint.record != null)
//            {
//                ToggleSelection(dataPoint, hit.point, hit.normal);
//            }
//        }
//    }
//    #region ToggleSelection Logic[multiple selection Card]
//    // If this point is already selected, unselect it (remove highlight + close card).
//    // If not selected yet, highlight it and spawn a new detail card.

//    private void ToggleSelection(AircraftDataPoint point, Vector3 hitPos, Vector3 hitNormal)
//    {
//        // already have a card for this point -> close it (unselect)
//        if (_openCards.TryGetValue(point, out var existingCard) && existingCard != null)
//        {
//            point.SetHighlighted(false);
//            Destroy(existingCard.gameObject);
//            _openCards.Remove(point);
//            return;
//        }

//        //NewSelection
//        Debug.Log("[DataPointSelector] Toggling ON selection: " + point.record.Name);

//        point.SetHighlighted(true);
//        int index = _openCards.Count;
//        var newCard = CreateCard(point, index);
//        if (newCard != null)
//        {
//            _openCards[point] = newCard;
//        }
//    }
//    #endregion
//    private AircraftDetailCard CreateCard(AircraftDataPoint point, int index)
//    {
//        if (detailCardPrefab == null)
//        {
//            Debug.LogWarning("[DataPointSelector] No DetailCard prefab assigned.");
//            return null;
//        }

//        if (mainCamera == null)
//            mainCamera = Camera.main;

//        var camTransform = mainCamera.transform;

//        // Grid layout: columns and rows based on index
//        int col = index % cardsPerRow;
//        int row = index / cardsPerRow;

//        // Center of the card grid in front of the camera
//        Vector3 center = camTransform.position + camTransform.forward * cardDistance;

//        // Horizontal offset: spread cards around center
//        float halfCols = (cardsPerRow - 1) * 0.5f;
//        Vector3 horizOffset = camTransform.right * ((col - halfCols) * cardHorizontalSpacing);

//        // Vertical offset: next rows go down
//        Vector3 vertOffset = -camTransform.up * (row * cardVerticalSpacing);

//        Vector3 spawnPos = center + horizOffset + vertOffset;

//        GameObject cardGO = Instantiate(detailCardPrefab, spawnPos, Quaternion.identity);

//        // Make card face the camera
//        Vector3 toCam = camTransform.position - spawnPos;
//        if (toCam.sqrMagnitude < 0.0001f)
//            toCam = -camTransform.forward;

//        cardGO.transform.rotation = Quaternion.LookRotation(-toCam, camTransform.up);

//        var card = cardGO.GetComponent<AircraftDetailCard>();
//        if (card != null)
//        {
//            card.Setup(point.record, mainCamera);
//        }
//        else
//        {
//            Debug.LogWarning("[DataPointSelector] DetailCard prefab is missing AircraftDetailCard component.");
//        }

//        return card;
//    }

//    // Clear Highlight when disabled
//    private void OnDisable()
//    {
//        // clean up highlights & cards if selector gets disabled
//        foreach (var kvp in _openCards)
//        {
//            var point = kvp.Key;
//            var card = kvp.Value;

//            if (point != null)
//                point.SetHighlighted(false);

//            if (card != null)
//                Destroy(card.gameObject);
//        }

//        _openCards.Clear();
//    }
//}

#endregion

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DataPointSelector : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject detailCardPrefab;

    // For multi-selection: each point has its card
    private readonly Dictionary<AircraftDataPoint, AircraftDetailCard> _openCards =
        new Dictionary<AircraftDataPoint, AircraftDetailCard>();

    // Just to keep a stable order for layout
    private readonly List<AircraftDetailCard> _cardList = new List<AircraftDetailCard>();

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
        if (Input.GetMouseButtonDown(0))
        {
            // Don’t raycast into the plot if the click started on UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            HandleRay(ray);
        }
#else
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

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
                ToggleSelection(dataPoint);
            }
        }
    }

    /// <summary>
    /// If this point already has a card -> unselect it.
    /// Otherwise create a new card and add it to the row.
    /// </summary>
    private void ToggleSelection(AircraftDataPoint point)
    {
        // already open -> close it
        if (_openCards.TryGetValue(point, out var existingCard) && existingCard != null)
        {
            Debug.Log("[DataPointSelector] Toggling OFF selection: " + point.record.Name);

            point.SetHighlighted(false);
            _openCards.Remove(point);
            _cardList.Remove(existingCard);
            Destroy(existingCard.gameObject);

            RepositionCards();
            return;
        }

        // new selection
        Debug.Log("[DataPointSelector] Toggling ON selection: " + point.record.Name);

        point.SetHighlighted(true);
        var newCard = CreateCard(point);
        if (newCard != null)
        {
            _openCards[point] = newCard;
            _cardList.Add(newCard);

            RepositionCards();
        }
    }

    /// <summary>
    /// Instantiate a DetailCard and fill it with this aircraft’s data.
    /// Position is handled later in RepositionCards().
    /// </summary>
    private AircraftDetailCard CreateCard(AircraftDataPoint point)
    {
        if (detailCardPrefab == null)
        {
            Debug.LogWarning("[DataPointSelector] No DetailCard prefab assigned.");
            return null;
        }

        GameObject cardGO = Instantiate(detailCardPrefab, Vector3.zero, Quaternion.identity);

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

    /// <summary>
    /// Arrange all open cards in a horizontal row in front of the camera,
    /// so they sit next to each other for easy comparison.
    /// </summary>
    private void RepositionCards()
    {
        if (_cardList.Count == 0)
            return;

        Transform camT = mainCamera != null ? mainCamera.transform : Camera.main.transform;

        // --- layout tuning ---
        float distance = 0.9f;      // meters in front of the camera
        float verticalOffset = 0.0f; // shift up or down relative to camera
        float spacing = 0.45f;      // distance between card centers (adjust to match card width)
        // ---------------------

        float totalWidth = spacing * (_cardList.Count - 1);
        float startOffset = -totalWidth * 0.5f;  // center the row

        for (int i = 0; i < _cardList.Count; i++)
        {
            var card = _cardList[i];
            if (card == null) continue;

            float offset = startOffset + spacing * i;

            // base “center” point for the row
            Vector3 rowCenter =
                camT.position +
                camT.forward * distance +
                camT.up * verticalOffset;

            Vector3 pos = rowCenter + camT.right * offset;

            card.transform.position = pos;
            // Let the card face the camera; AircraftDetailCard.LateUpdate will keep it facing
            card.transform.rotation = Quaternion.LookRotation(camT.forward, Vector3.up);
        }
    }

    private void OnDisable()
    {
        // Clear highlights & destroy all open cards
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
        _cardList.Clear();
    }
}
