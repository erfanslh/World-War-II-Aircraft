using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Drawing;
using Color = UnityEngine.Color;

public class AircraftPlotRootController : MonoBehaviour
{

    #region For Cube Country Material
    [System.Serializable]
    public class CountryFlagMaterial
    {
        public string countryName;   // must match rec.Country
        public Material material;    // material with the flag texture
    }
    [Header("Flag Materials")]
    public Material defaultFlagMaterial;                  // used if country not found
    public CountryFlagMaterial[] countryFlagMaterials;    // assign in Inspector

    private Dictionary<string, Material> _countryFlagMap = new();

    #endregion
    public static AircraftPlotRootController Instance { get; private set; }

    [Header("Data")]
    [Tooltip("Name of the JSON file in Resources (without .json extension)")]
    public string jsonResourceName = "ww2aircraft";
    public GameObject dataPointPrefab;

    [Header("Scatterplot Size (meters)")]
    [Tooltip("Length of the X axis in meters")]
    public float width = 1.5f;  // X
    [Tooltip("Length of the Y axis in meters")]
    public float height = 1.5f;  // Y
    [Tooltip("Length of the Z axis in meters")]
    public float depth = 1.5f;  // Z
    public Vector3 pointScale = new Vector3(0.03f, 0.03f, 0.03f);


    [Header("Axis Visuals")]
    public Renderer xAxisRenderer;
    public Renderer yAxisRenderer;
    public Renderer zAxisRenderer;

    [Header("Axis Labels")]
    public TMP_Text xAxisNameLabel;
    public TMP_Text xAxisMinLabel;
    public TMP_Text xAxisMaxLabel;

    public TMP_Text yAxisNameLabel;
    public TMP_Text yAxisMinLabel;
    public TMP_Text yAxisMaxLabel;

    public TMP_Text zAxisNameLabel;
    public TMP_Text zAxisMinLabel;
    public TMP_Text zAxisMaxLabel;

    [Header("Axis Label Offsets")]
    public float xLabelHeightOffset = 0.05f;  // how far above the X axis
    public float yLabelSideOffset = 0.05f;  // how far in front of Y axis
    public float zLabelSideOffset = 0.05f;  // how far beside Z axis


    public Color xAxisColor = Color.red;
    public Color yAxisColor = Color.green;
    public Color zAxisColor = Color.blue;

    private List<AircraftRecord> _records = new();
    private readonly List<GameObject> _spawnedPoints = new();
    private readonly Dictionary<string, Color> _countryColors = new();

    [HideInInspector] public List<string> allowedCountries = new();
    [HideInInspector] public List<string> allowedRoles = new();
    [HideInInspector] public List<string> allowedStates = new();




    // remember last mapping so filters can rebuild
    private NumericAttribute _lastXAttr;
    private NumericAttribute _lastYAttr;
    private NumericAttribute _lastZAttr;
    private bool _hasLastMapping;

    private void Awake()
    {
        Instance = this;
        LoadData(); 
        BuildCountryFlagMap();
        ApplyAxisColors();
    }
    private void BuildCountryFlagMap()
    {
        _countryFlagMap.Clear();

        if (countryFlagMaterials == null) return;

        foreach (var entry in countryFlagMaterials)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.countryName)) continue;
            if (entry.material == null) continue;

            string key = entry.countryName.Trim();
            if (!_countryFlagMap.ContainsKey(key))
            {
                _countryFlagMap.Add(key, entry.material);
            }
        }

        Debug.Log($"[AircraftPlotRootController] Country flag materials loaded: {_countryFlagMap.Count}");
    }
    private Material GetFlagMaterialForCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return defaultFlagMaterial;

        string key = country.Trim();

        if (_countryFlagMap.TryGetValue(key, out var mat))
            return mat;

        // not found → fallback
        return defaultFlagMaterial;
    }

    private void Start()
    {
        if (_records.Count > 0 && dataPointPrefab != null)
        {
            Debug.Log("[AircraftPlotRootController] Start() building default plot");
            BuildPlot(NumericAttribute.ActiveSince,
                      NumericAttribute.MaxSpeed,
                      NumericAttribute.Number);
        }
        else
        {
            Debug.LogWarning("[AircraftPlotRootController] Start(): missing records or datapoint prefab");
        }

    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LoadData()
    {
        TextAsset json = Resources.Load<TextAsset>(jsonResourceName);
        if (json == null)
        {
            Debug.LogError($"[AircraftPlotRootController] Could not find JSON resource '{jsonResourceName}' in Resources.");
            return;
        }

        try
        {
            AircraftRecord[] arr = JsonArrayHelper.FromJson<AircraftRecord>(json.text);
            _records = new List<AircraftRecord>(arr);
            Debug.Log($"[AircraftPlotRootController] Loaded aircraft records: {_records.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError("[AircraftPlotRootController] Failed to parse aircraft JSON: " + e);
        }
    }

    // Building plot

    public void BuildPlot(NumericAttribute xAttr, NumericAttribute yAttr, NumericAttribute zAttr)
    {
        Debug.Log($"[AircraftPlotRootController] BuildPlot called: X={xAttr}, Y={yAttr}, Z={zAttr}");

        if (_records == null || _records.Count == 0)
        {
            Debug.LogWarning("[AircraftPlotRootController] BuildPlot: no records loaded.");
            return;
        }
        if (dataPointPrefab == null)
        {
            Debug.LogWarning("[AircraftPlotRootController] BuildPlot: dataPointPrefab not set.");
            return;
        }

        _lastXAttr = xAttr;
        _lastYAttr = yAttr;
        _lastZAttr = zAttr;
        _hasLastMapping = true;

        foreach (var p in _spawnedPoints)
            if (p != null) Destroy(p);
        _spawnedPoints.Clear();

        List<AircraftRecord> filtered = _records.Where(PassesFilters).ToList();
        Debug.Log($"[AircraftPlotRootController] Filtered count: {filtered.Count}");

        if (filtered.Count == 0)
        {
            Debug.Log("[AircraftPlotRootController] BuildPlot: no records after filtering.");
            UpdateAxisLabels(xAttr, yAttr, zAttr, 0, 1, 0, 1, 0, 1);
            return;
        }

        GetMinMax(filtered, xAttr, out float minX, out float maxX);
        GetMinMax(filtered, yAttr, out float minY, out float maxY);
        GetMinMax(filtered, zAttr, out float minZ, out float maxZ);

        // labels get REAL min/max
        UpdateAxisLabels(xAttr, yAttr, zAttr, minX, maxX, minY, maxY, minZ, maxZ);

        // padded range just for positions
        float padX = (maxX - minX) * 0.05f;
        float padY = (maxY - minY) * 0.05f;
        float padZ = (maxZ - minZ) * 0.05f;

        float minXPadded = minX - padX;
        float maxXPadded = maxX + padX;
        float minYPadded = minY - padY;
        float maxYPadded = maxY + padY;
        float minZPadded = minZ - padZ;
        float maxZPadded = maxZ + padZ;

        foreach (var rec in filtered)
        {
            float xVal = GetValue(rec, xAttr);
            float yVal = GetValue(rec, yAttr);
            float zVal = GetValue(rec, zAttr);

            float tX = Mathf.InverseLerp(minXPadded, maxXPadded, xVal);
            float tY = Mathf.InverseLerp(minYPadded, maxYPadded, yVal);
            float tZ = Mathf.InverseLerp(minZPadded, maxZPadded, zVal);

            tX = Mathf.Lerp(0.05f, 0.95f, tX);
            tY = Mathf.Lerp(0.05f, 0.95f, tY);
            tZ = Mathf.Lerp(0.05f, 0.95f, tZ);

            float x = tX * width;
            float y = tY * height;
            float z = tZ * depth;

            GameObject pointGO = Instantiate(dataPointPrefab, transform);
            pointGO.transform.localPosition = new Vector3(x, y, z);
            pointGO.transform.localScale = pointScale;

            var renderer = pointGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material flagMat = GetFlagMaterialForCountry(rec.Country);
                if (flagMat != null)
                    renderer.material = flagMat;
                else
                    renderer.material.color = Color.black;   // safety fallback
            }

            // attach the record to this point
            var dp = pointGO.GetComponent<AircraftDataPoint>();
            if (dp != null)
                dp.record = rec;

            _spawnedPoints.Add(pointGO);
        }

    }

    public static float GetValue(AircraftRecord r, NumericAttribute attr)
    {
        switch (attr)
        {
            case NumericAttribute.ActiveSince: return r.ActiveSince;
            case NumericAttribute.MaxSpeed: return r.MaxSpeed;
            case NumericAttribute.Number: return r.Number;
            case NumericAttribute.Wingspan: return r.Wingspan;
            case NumericAttribute.Length: return r.Length;
            case NumericAttribute.Crew: return r.Crew;
            default: return 0f;
        }
    }

    private static void GetMinMax(
        List<AircraftRecord> recs,
        NumericAttribute attr,
        out float min, out float max)
    {
        min = float.PositiveInfinity;
        max = float.NegativeInfinity;

        foreach (var r in recs)
        {
            float v = GetValue(r, attr);
            if (v < min) min = v;
            if (v > max) max = v;
        }

        if (float.IsInfinity(min) || float.IsInfinity(max))
        {
            min = 0f;
            max = 1f;
        }
    }

    #region  Filters

    private bool PassesFilters(AircraftRecord rec)
    {
        // Country filter
        if (allowedCountries != null && allowedCountries.Count > 0 &&
            !allowedCountries.Contains(rec.Country))
            return false;

        // Role filter
        if (allowedRoles != null && allowedRoles.Count > 0 &&
            !allowedRoles.Contains(rec.PrimaryRole))
            return false;

        // State filter
        if (allowedStates != null && allowedStates.Count > 0 &&
            !allowedStates.Contains(rec.State))
            return false;

        return true;
    }
    #endregion

    // helper method for UI to call
    public void SetMultiFilters(
        List<string> countries,
        List<string> roles,
        List<string> states)
    {
        allowedCountries = countries ?? new List<string>();
        allowedRoles = roles ?? new List<string>();
        allowedStates = states ?? new List<string>();

        ApplyFiltersAndRebuild();
    }

    public void ApplyFiltersAndRebuild()
    {
        if (_hasLastMapping)
            BuildPlot(_lastXAttr, _lastYAttr, _lastZAttr);
    }




    //  AXIS VISUALS 

    private void ApplyAxisColors()
    {
        if (xAxisRenderer != null) xAxisRenderer.material.color = xAxisColor;
        if (yAxisRenderer != null) yAxisRenderer.material.color = yAxisColor;
        if (zAxisRenderer != null) zAxisRenderer.material.color = zAxisColor;
    }

    private void UpdateAxisLabels(
    NumericAttribute xAttr, NumericAttribute yAttr, NumericAttribute zAttr,
    float minX, float maxX,
    float minY, float maxY,
    float minZ, float maxZ)
    {
        string xName = GetAttributeDisplayName(xAttr);
        string yName = GetAttributeDisplayName(yAttr);
        string zName = GetAttributeDisplayName(zAttr);

        // Axis names (shown next to the cylinders)
        if (xAxisNameLabel != null) xAxisNameLabel.text = xName;
        if (yAxisNameLabel != null) yAxisNameLabel.text = yName;
        if (zAxisNameLabel != null) zAxisNameLabel.text = zName;

        // Min / Max numbers at ends of each axis
        if (xAxisMinLabel != null) xAxisMinLabel.text = $"{minX:0.#}";
        if (xAxisMaxLabel != null) xAxisMaxLabel.text = $"{maxX:0.#}";

        if (yAxisMinLabel != null) yAxisMinLabel.text = $"{minY:0.#}";
        if (yAxisMaxLabel != null) yAxisMaxLabel.text = $"{maxY:0.#}";

        if (zAxisMinLabel != null) zAxisMinLabel.text = $"{minZ:0.#}";
        if (zAxisMaxLabel != null) zAxisMaxLabel.text = $"{maxZ:0.#}";
    }


    private static string GetAttributeDisplayName(NumericAttribute attr)
    {
        // You can customize these if you want spaces
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

    private static string FormatRange(float min, float max)
    {
        // round to one decimal if needed
        return $"{min:0.#} – {max:0.#}";
    }
}
