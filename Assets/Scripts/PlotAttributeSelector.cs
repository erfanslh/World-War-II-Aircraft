using UnityEngine;
using UnityEngine.UI;

public class PlotAttributeSelector : MonoBehaviour
{
    public Dropdown xDropdown;
    public Dropdown yDropdown;
    public Dropdown zDropdown;

    private AircraftPlotRootController _controller;

    private void Start()
    {
        xDropdown.onValueChanged.AddListener(_ => ApplySelection());
        yDropdown.onValueChanged.AddListener(_ => ApplySelection());
        zDropdown.onValueChanged.AddListener(_ => ApplySelection());
    }

    private void Update()
    {
        if (_controller == null && AircraftPlotRootController.Instance != null)
        {
            _controller = AircraftPlotRootController.Instance;
            ApplySelection();   // build initial plot
        }
    }

    private void ApplySelection()
    {
        if (_controller == null) return;

        NumericAttribute xAttr = (NumericAttribute)xDropdown.value;
        NumericAttribute yAttr = (NumericAttribute)yDropdown.value;
        NumericAttribute zAttr = (NumericAttribute)zDropdown.value;

        _controller.BuildPlot(xAttr, yAttr, zAttr);
    }
}
