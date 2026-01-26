using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class AircraftDetailCard : MonoBehaviour
{

    [Header("Text Fields")]
    public TMP_Text nameText;
    public TMP_Text roleText;
    public TMP_Text manufacturerText;
    public TMP_Text countryText;
    public TMP_Text numberText;
    public TMP_Text activeSinceText;
    public TMP_Text lastBuiltText;
    public TMP_Text retiredText;
    public TMP_Text stateText;
    public TMP_Text crewText;
    public TMP_Text lengthText;
    public TMP_Text wingspanText;
    public TMP_Text heightText;
    public TMP_Text wingAreaText;
    public TMP_Text maxSpeedText;

    [Header("Axis-focused summary")]
    public TMP_Text axesSummaryText;
    public TMP_Text countrySummaryText;
    public TMP_Text roleSummaryText;

    [Header("Visuals")]
    public Image previewImage;     
    public Transform modelAnchor; 
    public Image countryFlagRenderer;

    [Header("Controls")]
    public Button closeButton;      

    private GameObject _spawnedModel;
    private Camera _camera; 
    private Canvas _canvas;           // world-space canvas on this prefab

    [Header("Link to selected cube")]
    [SerializeField] private LineRenderer linkLine;   // line object on the card
    [SerializeField] private Transform linkAnchor;    // where the line starts on the card

    private AircraftDataPoint _ownerPoint;
    private DataPointSelector _selector;

    //Define Colors for Values
    [Header("Axis rank colors")]
    public Color lowColor = new Color(0.85f, 0.25f, 0.25f);  // red-ish
    public Color mediumColor = new Color(0.95f, 0.8f, 0.25f);  // yellow-ish
    public Color highColor = new Color(0.25f, 0.85f, 0.25f);  // green-ish

    private Color GetRankColor(RankBand band)
    {
        // Detect "all zero" = not configured → use hard-coded defaults
        bool allBlack =
            lowColor == default(Color) &&
            mediumColor == default(Color) &&
            highColor == default(Color);

        if (allBlack)
        {
            lowColor = new Color(0.85f, 0.25f, 0.25f);  // red-ish
            mediumColor = new Color(0.95f, 0.80f, 0.25f);  // yellow-ish
            highColor = new Color(0.25f, 0.85f, 0.25f);  // green-ish
        }

        return band switch
        {
            RankBand.Low => lowColor,
            RankBand.Medium => mediumColor,
            RankBand.High => highColor,
            _ => mediumColor
        };
    }

    // Put this inside AircraftDetailCard, above BuildAxisRankLine for example
    private float ComputePercentile(
        NumericAttribute attr,
        float value,
        List<AircraftRecord> domain)
    {
        if (domain == null || domain.Count == 0)
            return 0.5f;

        List<float> values = new List<float>(domain.Count);
        foreach (var rec in domain)
        {
            float v = AircraftPlotRootController.GetValue(rec, attr);
            if (v > 0f)
                values.Add(v);
        }

        if (values.Count == 0)
            return 0.5f;

        values.Sort();
        int n = values.Count;

        if (value <= values[0]) return 0f;
        if (value >= values[n - 1]) return 1f;

        int hiIndex = 1;
        while (hiIndex < n && values[hiIndex] < value)
            hiIndex++;

        int loIndex = hiIndex - 1;
        float a = values[loIndex];
        float b = values[hiIndex];

        float t = Mathf.Approximately(a, b) ? 0f : (value - a) / (b - a);
        float p = (loIndex + t) / (n - 1);

        // no special-case flip here – ActiveSince will be
        // mapped to "early / average / late" by GetRankWord.
        return Mathf.Clamp01(p);
    }


    private string BuildAxisRankLine(
        string axisLetter,
        NumericAttribute attr,
        float value,
        List<AircraftRecord> domain)
    {
        // 1) compute percentile from domain (what you already do)
        float percentile = ComputePercentile(attr, value, domain); // 0..1

        // 2) turn into band + word + color
        RankBand band = GetRankBand(percentile);
        string rankWord = GetRankWord(attr, band);
        Color bandColor = GetRankColor(band);
        string hexColor = ColorUtility.ToHtmlStringRGB(bandColor);

        // 3) format numeric value the same way as axis labels
        string formattedVal = AircraftPlotRootController.FormatAxisNumber(attr, value);

        // 4) wrap both number + word in the same color tag
        return $"{axisLetter} ({GetAttributeDisplayName(attr)}): " +
               $"<color=#{hexColor}>{formattedVal} {rankWord}</color>";
    }


    public void Initialize(AircraftDataPoint ownerPoint, DataPointSelector selector)
    {
        _ownerPoint = ownerPoint;
        _selector = selector;

        // make sure line is enabled only if we have a point
        if (linkLine != null)
            linkLine.enabled = (_ownerPoint != null);
    }

    private void Awake()
    {
        // cache the card’s canvas
        _canvas = GetComponentInChildren<Canvas>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Setup(AircraftRecord r, Camera cam)
    {
        if (r == null) return;

        _camera = cam;

        // ----- Text fields (already have this) -----
        if (nameText) nameText.text = r.Name;
        if (roleText) roleText.text = r.PrimaryRole;
        if (manufacturerText) manufacturerText.text = r.Manufacturer;
        if (countryText) countryText.text = r.Country;
        if (numberText) numberText.text = r.Number.ToString("0");
        if (activeSinceText) activeSinceText.text = r.ActiveSince.ToString("0");
        if (lastBuiltText) lastBuiltText.text = r.LastBuilt.ToString("0");
        if (retiredText) retiredText.text = r.Retired.ToString("0");
        if (stateText) stateText.text = r.State;
        if (crewText) crewText.text = r.Crew.ToString("0");
        if (lengthText) lengthText.text = r.Length.ToString("0.0");
        if (wingspanText) wingspanText.text = r.Wingspan.ToString("0.0");
        if (heightText) heightText.text = r.Height.ToString("0.00");
        if (wingAreaText) wingAreaText.text = r.WingArea.ToString("0.00");
        if (maxSpeedText) maxSpeedText.text = r.MaxSpeed.ToString("0");

        // ----- 2D aircraft image (your existing logic) -----
        if (previewImage != null)
        {
            var sprite = Resources.Load<Sprite>("AircraftImages/" + r.Name);
            if (sprite != null)
            {
                previewImage.sprite = sprite;
                previewImage.enabled = true;
            }
            else
            {
                previewImage.enabled = false;
            }
        }

        // ----- COUNTRY FLAG BEHIND MODEL (NEW) -----
        if (countryFlagRenderer != null)
        {
            var flagController = AircraftPlotRootController.Instance;
            if (flagController != null)
            {
                // reuse the same function as cubes
                Material flagMat = flagController.GetFlagMaterialForCountry(r.Country);

                Debug.Log($"[DetailCard] Country={r.Country}, flagMat={(flagMat ? flagMat.name : "null")}");

                if (flagMat != null)
                {
                    countryFlagRenderer.material = flagMat;
                    countryFlagRenderer.gameObject.SetActive(true);
                }
                else
                {
                    countryFlagRenderer.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("[DetailCard] No AircraftPlotRootController.Instance; cannot get flag material.");
                countryFlagRenderer.gameObject.SetActive(false);
            }
        }

        // ----- 3D MODEL -----
        if (modelAnchor != null)
        {
            if (_spawnedModel != null)
                Destroy(_spawnedModel);

            GameObject modelPrefab =
                Resources.Load<GameObject>("AircraftModels/" + r.Name);

            if (modelPrefab != null)
            {
                _spawnedModel = Instantiate(modelPrefab, modelAnchor);
                _spawnedModel.transform.localPosition = Vector3.zero;
                _spawnedModel.transform.localRotation = Quaternion.identity;
                _spawnedModel.transform.localScale = Vector3.one;

                // enable drag-to-surface on this model
                var dragger = _spawnedModel.GetComponent<AircraftModelDragger>();
                if (dragger == null)
                {
                    dragger = _spawnedModel.AddComponent<AircraftModelDragger>();
                }

                // Pass camera and ARRaycastManager (same AR environment as the plot)
                var arRay = FindObjectOfType<ARRaycastManager>();
                dragger.Init(_camera, arRay);
            }
        }
        // ---- Axis-focused analytics ----
        var controller = AircraftPlotRootController.Instance;
        if (controller != null && controller.TryGetCurrentAxes(out var xAttr, out var yAttr, out var zAttr))
        {
            // 1) header: show which attributes are on axes
            FillAxesSummary(r);

            // 2) stats within same country
            var xCountry = controller.ComputeCountryAxisStats(r, xAttr);
            var yCountry = controller.ComputeCountryAxisStats(r, yAttr);
            var zCountry = controller.ComputeCountryAxisStats(r, zAttr);

            if (countrySummaryText != null)
            {
                countrySummaryText.text = BuildSummaryLine(
                    "Within Country (" + r.Country + ")",
                    xCountry, yCountry, zCountry);
            }

            // 3) stats within same role
            var xRole = controller.ComputeRoleAxisStats(r, xAttr);
            var yRole = controller.ComputeRoleAxisStats(r, yAttr);
            var zRole = controller.ComputeRoleAxisStats(r, zAttr);

            if (roleSummaryText != null)
            {
                roleSummaryText.text = BuildSummaryLine(
                    "Within Role (" + r.PrimaryRole + ")",
                    xRole, yRole, zRole);
            }
        }
    }


    private void LateUpdate()
    {
        if (_camera == null) return;

        // keep the card facing the camera
        Transform camT = _camera.transform;
        transform.LookAt(camT.position, Vector3.up);
        transform.Rotate(0f, 180f, 0f, Space.Self);

        // --- update line to the cube ---
        if (linkLine != null && _ownerPoint != null)
        {
            // start of line: card anchor (or card root if not set)
            Vector3 startPos = linkAnchor != null ? linkAnchor.position : transform.position;
            // end of line: cube position
            Vector3 endPos = _ownerPoint.transform.position;

            linkLine.positionCount = 2;
            linkLine.SetPosition(0, startPos);
            linkLine.SetPosition(1, endPos);
            linkLine.enabled = true;
        }
        else if (linkLine != null)
        {
            linkLine.enabled = false;
        }
    }

    // Simple label like "Max Speed"
    private string GetAttributeDisplayName(NumericAttribute attr)
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

    // Turn normalized 0..1 into "low / medium / high"
    private string DescribeLevel(float normalized)
    {
        if (normalized >= 0.66f) return "<color=#00FF00>high</color>";    // green
        if (normalized >= 0.33f) return "<color=#FFFF00>medium</color>";  // yellow
        return "<color=#FF0000>low</color>";                               // red
    }

    private void FillAxesSummary(AircraftRecord rec)
    {
        var ctrl = AircraftPlotRootController.Instance;
        if (ctrl == null || axesSummaryText == null) return;

        var domain = ctrl.lastFilteredRecords;
        if (domain == null || domain.Count < 3) return;

        NumericAttribute xAttr = ctrl.lastXAttr;
        NumericAttribute yAttr = ctrl.lastYAttr;
        NumericAttribute zAttr = ctrl.lastZAttr;

        float xVal = AircraftPlotRootController.GetValue(rec, xAttr);
        float yVal = AircraftPlotRootController.GetValue(rec, yAttr);
        float zVal = AircraftPlotRootController.GetValue(rec, zAttr);

        string xLine = BuildAxisRankLine("X", xAttr, xVal, domain);
        string yLine = BuildAxisRankLine("Y", yAttr, yVal, domain);
        string zLine = BuildAxisRankLine("Z", zAttr, zVal, domain);

        axesSummaryText.text =
            $"Within Current Plot \n" +
            $"{xLine}\n{yLine}\n{zLine}";
    }

    // Build a multi-line summary for 3 axes
    private string BuildSummaryLine(
    string header,
    AircraftPlotRootController.AxisStats x,
    AircraftPlotRootController.AxisStats y,
    AircraftPlotRootController.AxisStats z)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(header);

        void AppendAxis(char axisLetter, AircraftPlotRootController.AxisStats s)
        {
            if (!s.valid) return;

            // band & color from normalized 0..1
            RankBand band = GetRankBand(s.normalized);
            string rankWord = GetRankWord(s.attr, band);
            Color bandColor = GetRankColor(band);
            string hexColor = ColorUtility.ToHtmlStringRGB(bandColor);

            string formattedVal = AircraftPlotRootController.FormatAxisNumber(s.attr, s.value);

            sb.AppendLine(
                $"{axisLetter} ({GetAttributeDisplayName(s.attr)}): " +
                $"<color=#{hexColor}>{formattedVal} {rankWord}</color>");
        }

        AppendAxis('X', x);
        AppendAxis('Y', y);
        AppendAxis('Z', z);

        return sb.ToString();
    }


    public void Close()
    {
        // tell selector that this card is closing so it can stop breathing
        if (_selector != null && _ownerPoint != null)
        {
            _selector.OnCardClosed(this, _ownerPoint);
        }

        Destroy(gameObject);
    }

    #region ActiveSince_Rank
    private enum RankBand { Low, Medium, High }

    private string GetRankWord(NumericAttribute attr, RankBand band)
    {
        // Special wording for years
        if (attr == NumericAttribute.ActiveSince)
        {
            return band switch
            {
                RankBand.Low => "early",
                RankBand.Medium => "average",
                RankBand.High => "late",
                _ => "average"
            };
        }

        // Default wording
        return band switch
        {
            RankBand.Low => "low",
            RankBand.Medium => "medium",
            RankBand.High => "high",
            _ => "medium"
        };
    }

    private RankBand GetRankBand(float percentile)
    {
        if (percentile < 1f / 3f) return RankBand.Low;
        else if (percentile < 2f / 3f) return RankBand.Medium;
        else return RankBand.High;
    }

    #endregion


}
