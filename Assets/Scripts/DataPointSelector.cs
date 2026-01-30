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
    #region oldcode
    //private void HandleRay(Ray ray)
    //{
    //    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
    //    {
    //        var dataPoint = hit.collider.GetComponentInParent<AircraftDataPoint>();
    //        if (dataPoint != null && dataPoint.record != null)
    //        {
    //            ToggleSelection(dataPoint);
    //        }
    //    }
    //}
    #endregion

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

    #region V 1.0  RepositionCard

    //private void RepositionCards()
    //{
    //    if (_cardList.Count == 0)
    //        return;

    //    Transform camT = mainCamera != null ? mainCamera.transform : Camera.main?.transform;
    //    if (camT == null) return;

    //    Vector3 center;
    //    Vector3 rightDir;

    //    var plot = AircraftPlotRootController.Instance;
    //    if (plot != null)
    //    {
    //        Transform plotT = plot.transform;

    //        // world-space center of the plot
    //        var localCenter = new Vector3(plot.width * 0.5f,
    //                                      plot.height * 0.5f,
    //                                      plot.depth * 0.5f);
    //        Vector3 plotCenter = plotT.TransformPoint(localCenter);

    //        Vector3 camToPlot = plotCenter - camT.position;
    //        if (camToPlot.sqrMagnitude < 0.0001f)
    //            camToPlot = camT.forward;

    //        Vector3 dir = camToPlot.normalized;

    //        float extraDistance = Mathf.Max(plot.width, plot.depth) * 0.7f;
    //        Vector3 baseCenter = plotCenter + dir * extraDistance;

    //        // 🔹 keep X/Z behind the plot, but Y locked to camera (eye level)
    //        center = new Vector3(baseCenter.x, camT.position.y, baseCenter.z);

    //        // horizontal direction (sideways) for card row
    //        rightDir = Vector3.Cross(Vector3.up, dir);
    //        if (rightDir.sqrMagnitude < 0.0001f)
    //            rightDir = camT.right;
    //        else
    //            rightDir.Normalize();
    //    }
    //    else if (cardRowOrigin != null)
    //    {
    //        center = cardRowOrigin.position;
    //        // also force it to camera height so it’s not too low/high
    //        center.y = camT.position.y;
    //        rightDir = cardRowOrigin.right;
    //    }
    //    else
    //    {
    //        float distance = 0.9f;
    //        center = camT.position + camT.forward * distance;
    //        // this already has camera.y so no extra change needed
    //        rightDir = camT.right;
    //    }

    //    float spacing = 0.45f;
    //    int n = _cardList.Count;
    //    float startOffset = -(n - 1) * 0.5f * spacing;

    //    for (int i = 0; i < n; i++)
    //    {
    //        var card = _cardList[i];
    //        if (card == null) continue;

    //        Vector3 targetPos = center + rightDir * (startOffset + i * spacing);
    //        card.transform.position = targetPos;

    //        // face camera
    //        card.transform.LookAt(camT.position, Vector3.up);
    //        card.transform.Rotate(0f, 180f, 0f, Space.Self);
    //    }
    //}

    #endregion

    /// <summary>
    /// Arrange all open cards on a horizontal arc (equator) around the user.
    /// Cards are placed at a fixed radius, at roughly eye height, in the
    /// front hemisphere only.
    /// </summary>
    #region V2.0 RepositionCard
    //private void RepositionCards()
    //{
    //    if (_cardList.Count == 0)
    //        return;

    //    Transform camT = mainCamera != null ? mainCamera.transform : Camera.main.transform;
    //    if (camT == null)
    //        return;

    //    int n = _cardList.Count;

    //    // -------- layout parameters you can tweak --------
    //    float radius = 1.0f;          // distance of cards from the head
    //    float eyeHeightOffset = 0.0f; // 0 = same Y as camera, positive = slightly above
    //    float maxAngleDeg = 90f;      // spread across -maxAngle .. +maxAngle in front

    //    // Define a horizontal plane using camera forward/right projected on world-up
    //    Vector3 forwardFlat = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
    //    if (forwardFlat.sqrMagnitude < 0.0001f)
    //        forwardFlat = camT.forward;

    //    Vector3 rightFlat = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;

    //    // The “equator” center is basically the camera position (plus optional offset)
    //    Vector3 center = camT.position + Vector3.up * eyeHeightOffset;

    //    for (int i = 0; i < n; i++)
    //    {
    //        var card = _cardList[i];
    //        if (card == null) continue;

    //        // t = 0..1 along the set of cards => map to -maxAngle..+maxAngle
    //        float t = (n <= 1) ? 0.5f : (float)i / (n - 1);
    //        float angleDeg = Mathf.Lerp(-maxAngleDeg, maxAngleDeg, t);
    //        float angleRad = angleDeg * Mathf.Deg2Rad;

    //        // Direction on the horizontal plane
    //        Vector3 dir =
    //            forwardFlat * Mathf.Cos(angleRad) +
    //            rightFlat * Mathf.Sin(angleRad);

    //        Vector3 targetPos = center + dir * radius;
    //        card.transform.position = targetPos;

    //        // Make sure card faces the camera
    //        card.transform.LookAt(camT.position, Vector3.up);
    //        card.transform.Rotate(0f, 180f, 0f, Space.Self);
    //    }
    //}
    #endregion

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



}

