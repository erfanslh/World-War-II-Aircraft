using System;
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
        // RaycastAll so we see *all* hits along the ray, not just the first
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        if (hits == null || hits.Length == 0)
            return;

        // sort by distance, closest first
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // pick the first thing that actually has an AircraftDataPoint somewhere up its hierarchy
        foreach (var h in hits)
        {
            var dataPoint = h.collider.GetComponentInParent<AircraftDataPoint>();
            if (dataPoint != null && dataPoint.record != null)
            {
                // your existing toggle that only needs the point
                ToggleSelection(dataPoint);
                return;
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
            RecomputeSelectionSummary();
        }
    }

    /// <summary>
    /// Create a DetailCard for the given aircraft. Initial position is roughly in
    /// front of the camera; exact layout is handled later by RepositionCards().
    /// </summary>
    private AircraftDetailCard CreateCard(AircraftDataPoint point)
    {
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
            var renderer = point.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                card.SetLinkMaterial(renderer.material);   // flag material
            }
            // so the line knows where to start
            card.SetLinkTarget(point.transform);          
            // IMPORTANT: tell the card who owns it and who the selector is,
            // so Close() can notify us and we can stop breathing.
            card.Initialize(point, this);
        }
        else
        {
            Debug.LogWarning("[DataPointSelector] DetailCard prefab is missing AircraftDetailCard component.");
        }

        return card;
    }

    public void OnCardClosed(AircraftDetailCard card, AircraftDataPoint point)
    {
        // 1) clear highlight + dictionary entry if tracked
    if (point != null && _openCards.TryGetValue(point, out var existingCard))
    {
        if (existingCard == card)
        {
            point.SetHighlighted(false);
            _openCards.Remove(point);
        }
    }

    // 2) remove from layout list
    if (card != null)
    {
        _cardList.Remove(card);
    }

    // 3) update layout + summaries for remaining cards
    RepositionCards();
    RecomputeSelectionSummary();
    }

    private void RepositionCards()
    {
        if (_cardList.Count == 0)
            return;

        Transform camT = mainCamera != null ? mainCamera.transform : Camera.main.transform;
        if (camT == null)
            return;

        int n = _cardList.Count;

        // -------- layout parameters you can tweak --------
        float radius = 1.0f;            // distance of cards from the head
        float eyeHeightOffset = 0.0f;   // 0 = same Y as camera
        float angleStepDeg = 30.0f;     // separation between neighbouring cards in degrees
        float maxAngleDeg = 120.0f;       // optional clamp so it doesn’t wrap behind you

        // Flatten camera forward/right onto horizontal plane
        Vector3 forwardFlat = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 0.0001f)
            forwardFlat = camT.forward;

        Vector3 rightFlat = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;

        // center of the “equator” circle
        Vector3 center = camT.position + Vector3.up * eyeHeightOffset;

        // total angular span we need for n cards
        float totalSpan = (n - 1) * angleStepDeg;
        float startAngle = -totalSpan * 0.5f;   // so they’re centered in front

        for (int i = 0; i < n; i++)
        {
            var card = _cardList[i];
            if (card == null) continue;

            // angle for this card
            float angleDeg = startAngle + i * angleStepDeg;

            // optionally clamp so they don’t go too far to the sides
            angleDeg = Mathf.Clamp(angleDeg, -maxAngleDeg, maxAngleDeg);

            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 dir =
                forwardFlat * Mathf.Cos(angleRad) +
                rightFlat * Mathf.Sin(angleRad);

            Vector3 targetPos = center + dir * radius;
            card.transform.position = targetPos;

            // face the camera
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

    // ============================================================
    //  SELECTION-ONLY COMPARISON SUMMARY
    // ============================================================
    private void RecomputeSelectionSummary()
    {
        var controller = AircraftPlotRootController.Instance;
        if (controller == null)
            return;

        if (_openCards.Count == 0)
            return;

        // Which axes are currently used by the plot?
        if (!controller.TryGetCurrentAxes(out var xAttr, out var yAttr, out var zAttr))
        {
            // No active mapping => clear selection summaries
            foreach (var kvp in _openCards)
            {
                var card = kvp.Value;
                if (card != null)
                    card.SetSelectionSummary("");
            }
            return;
        }

        int total = _openCards.Count;

        // If only one card is open, just show a hint
        if (total < 2)
        {
            foreach (var kvp in _openCards)
            {
                var card = kvp.Value;
                if (card != null)
                    card.SetSelectionSummary("Select multiple aircraft to compare them here.");
            }
            return;
        }

        // Build ranks per axis (1 = highest value)
        var rankX = BuildAxisRanks(xAttr);
        var rankY = BuildAxisRanks(yAttr);
        var rankZ = BuildAxisRanks(zAttr);

        // Update each open card
        foreach (var kvp in _openCards)
        {
            var point = kvp.Key;
            var card = kvp.Value;
            if (point == null || card == null || point.record == null)
                continue;

            string summary = BuildCardSummaryText(
                point,
                total,
                xAttr, yAttr, zAttr,
                rankX, rankY, rankZ
            );

            card.SetSelectionSummary(summary);
        }
    }

    private Dictionary<AircraftDataPoint, int> BuildAxisRanks(NumericAttribute attr)
    {
        var result = new Dictionary<AircraftDataPoint, int>();
        var list = new List<(AircraftDataPoint point, float val)>();

        foreach (var kvp in _openCards)
        {
            var p = kvp.Key;
            if (p == null || p.record == null) continue;

            float v = AircraftPlotRootController.GetValue(p.record, attr);
            list.Add((p, v));
        }

        // Higher value -> smaller rank number (1 = biggest)
        list.Sort((a, b) => b.val.CompareTo(a.val));

        for (int i = 0; i < list.Count; i++)
        {
            result[list[i].point] = i + 1;
        }

        return result;
    }

    private string BuildCardSummaryText(
        AircraftDataPoint point,
        int total,
        NumericAttribute xAttr,
        NumericAttribute yAttr,
        NumericAttribute zAttr,
        Dictionary<AircraftDataPoint, int> rankX,
        Dictionary<AircraftDataPoint, int> rankY,
        Dictionary<AircraftDataPoint, int> rankZ)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Comparing {total} selected aircraft:");

        AppendAxisLine(sb, point, xAttr, rankX, total);
        AppendAxisLine(sb, point, yAttr, rankY, total);
        AppendAxisLine(sb, point, zAttr, rankZ, total);

        return sb.ToString();
    }

    private void AppendAxisLine(
        System.Text.StringBuilder sb,
        AircraftDataPoint point,
        NumericAttribute attr,
        Dictionary<AircraftDataPoint, int> rankMap,
        int total)
    {
        if (!rankMap.TryGetValue(point, out int rank))
            return;

        string axisName = AxisDisplayName(attr);
        string ordinal = ToOrdinal(rank);

        // Color code: 1st = green, last = red, others = amber
        string color;
        string suffix;
        if (rank == 1)
        {
            color = "#4CAF50"; // dark green
            suffix = " (highest)";
        }
        else if (rank == total)
        {
            color = "#F44336"; // red
            suffix = " (lowest)";
        }
        else
        {
            color = "#FFC107"; // amber
            suffix = "";
        }

        sb.AppendLine(
            $"• {axisName}: <color={color}>{ordinal} of {total}{suffix}</color>");
    }

    private static string AxisDisplayName(NumericAttribute attr)
    {
        switch (attr)
        {
            case NumericAttribute.ActiveSince: return "Active Since";
            case NumericAttribute.MaxSpeed: return "Max Speed";
            case NumericAttribute.Number: return "Number Built";
            case NumericAttribute.Wingspan: return "Wingspan";
            case NumericAttribute.Length: return "Length";
            case NumericAttribute.Crew: return "Crew";
            default: return attr.ToString();
        }
    }

    private static string ToOrdinal(int n)
    {
        if (n <= 0) return n.ToString();

        int rem100 = n % 100;
        if (rem100 >= 11 && rem100 <= 13)
            return n + "th";

        switch (n % 10)
        {
            case 1: return n + "st";
            case 2: return n + "nd";
            case 3: return n + "rd";
            default: return n + "th";
        }
    }



}

