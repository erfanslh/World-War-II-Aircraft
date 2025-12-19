using UnityEngine;

public class AircraftDataPoint : MonoBehaviour
{
    public AircraftRecord record;      // filled at runtime
    public Renderer pointRenderer;     // optional, for highlight

    public void SetHighlight(bool on)
    {
        if (pointRenderer == null) return;
        pointRenderer.material.color = on ? Color.yellow : Color.black;
    }
}
