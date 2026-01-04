using UnityEngine;

public class AircraftDataPoint : MonoBehaviour
{
    public AircraftRecord record;

    [Header("Highlight (breathing)")]
    [Tooltip("How big the pulse is, as a percentage of the original size")]
    public float pulseAmount = 0.15f;   // 15% bigger at peak
    [Tooltip("How fast the breathing animation is")]
    public float pulseSpeed = 3f;

    private Vector3 _baseScale;
    private bool _isHighlighted;

    // IMPORTANT: use Start, not Awake
    private void Start()
    {
        // At this point, BuildPlot has already set the pointScale
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        if (!_isHighlighted)
        {
            // make sure we’re back to normal scale
            if (transform.localScale != _baseScale)
                transform.localScale = _baseScale;
            return;
        }

        // Breathing scale: 1 (+) or (-) pulseAmount
        float t = Mathf.Sin(Time.time * pulseSpeed);    // -1 .. +1
        float scaleFactor = 1f + t * pulseAmount;       // e.g. 0.85 .. 1.15

        transform.localScale = _baseScale * scaleFactor;
    }


    // Called by DataPointSelector when this data point is selected / deselected.
    public void SetHighlighted(bool value)
    {
        _isHighlighted = value;

        if (!value)
        {
            // reset scale immediately when deselected
            transform.localScale = _baseScale;
        }
    }
}

