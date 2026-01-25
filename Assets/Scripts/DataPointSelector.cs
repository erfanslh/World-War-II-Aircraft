using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DataPointSelector : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject detailCardPrefab;

    [Tooltip("Optional: where the row of cards should be centered. If null, we use in front of the camera.")]
    public Transform cardRowOrigin;

    // Which cube has which card open
    private readonly Dictionary<AircraftDataPoint, AircraftDetailCard> _openCards =
        new Dictionary<AircraftDataPoint, AircraftDetailCard>();

    // Ordered list so we can lay cards out left-to-right
    private readonly List<AircraftDetailCard> _cardList =
        new List<AircraftDetailCard>();

    private bool _selectionEnabled = false;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void EnableSelection()
    {
        _selectionEnabled = true;
        Debug.Log("[DataPointSelector] Selection enabled.");
    }

    private void Update()
    {
        if (!_selectionEnabled)
            return;

        if (AircraftModelDragger.AnyModelInGesture)
            return;

#if UNITY_EDITOR
        // --- Mouse click in Editor ---
        if (Input.GetMouseButtonDown(0))
        {
            // Optional: ignore if click starts over UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            HandleRay(ray);
        }
#else
        // --- Touch on device ---
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
    /// Toggle selection for a given cube.
    /// If it was selected -> unselect and close its card.
    /// If it was not selected -> open a new card and highlight it.
    /// </summary>
    private void ToggleSelection(AircraftDataPoint point)
    {
        // Already have a card for this point -> close it (unselect)
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

        // New selection
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
    /// Create a DetailCard for the given aircraft. Initial position is roughly in
    /// front of the camera; exact layout is handled later by RepositionCards().
    /// </summary>
    private AircraftDetailCard CreateCard(AircraftDataPoint point)
    {

        #region OldFunc
        if (detailCardPrefab == null)
        {
            Debug.LogWarning("[DataPointSelector] No DetailCard prefab assigned.");
            return null;
        }

        Transform camT = mainCamera != null ? mainCamera.transform : Camera.main.transform;

        // Spawn somewhere in front of the camera so it doesn’t appear at (0,0,0)
        Vector3 spawnPos = camT.position + camT.forward * 0.8f;

        GameObject cardGO = Instantiate(detailCardPrefab, spawnPos, Quaternion.identity);

        var card = cardGO.GetComponent<AircraftDetailCard>();
        if (card != null)
        {
            card.Setup(point.record, mainCamera);
            // IMPORTANT: tell the card who owns it and who the selector is,
            // so Close() can notify us and we can stop breathing.
            card.Initialize(point, this);
        }
        else
        {
            Debug.LogWarning("[DataPointSelector] DetailCard prefab is missing AircraftDetailCard component.");
        }

        return card;
        #endregion
    }

    public void OnCardClosed(AircraftDetailCard card, AircraftDataPoint point)
    {
        // 1) Clear highlight + dictionary entry if this point/card is tracked
        if (point != null && _openCards.TryGetValue(point, out var existingCard))
        {
            // Only clear if the card we’re closing is the same one we stored
            if (existingCard == card)
            {
                // stop breathing highlight
                point.SetHighlighted(false);

                // forget this selection
                _openCards.Remove(point);
            }
        }

        // 2) Remove this card from the layout list
        if (card != null)
        {
            _cardList.Remove(card);
        }

        // 3) Rebuild positions for the remaining cards
        RepositionCards();
    }


    /// <summary>
    /// Arrange all open cards in a horizontal row in front of the camera (or
    /// around cardRowOrigin if you set that in the inspector).
    /// </summary>
    private void RepositionCards()
    {
        if (_cardList.Count == 0)
            return;

        Transform camT = mainCamera != null ? mainCamera.transform : Camera.main.transform;

        Vector3 center;
        Vector3 rightDir;

        if (cardRowOrigin != null)
        {
            center = cardRowOrigin.position;
            rightDir = cardRowOrigin.right;
        }
        else
        {
            float distance = 0.9f;
            center = camT.position + camT.forward * distance;
            rightDir = camT.right;
        }

        float spacing = 0.45f;   // distance between cards
        int n = _cardList.Count;
        float startOffset = -(n - 1) * 0.5f * spacing;

        for (int i = 0; i < n; i++)
        {
            var card = _cardList[i];
            if (card == null) continue;

            Vector3 targetPos = center + rightDir * (startOffset + i * spacing);
            card.transform.position = targetPos;

            // make sure card faces the camera
            card.transform.LookAt(camT.position, Vector3.up);
            card.transform.Rotate(0f, 180f, 0f, Space.Self);
        }
    }

    // Clear highlights and cards if selector gets disabled
    private void OnDisable()
    {
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

