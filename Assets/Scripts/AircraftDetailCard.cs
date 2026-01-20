using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Visuals")]
    public Image previewImage;     
    public Transform modelAnchor;   

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

        // store camera reference
        _camera = cam != null ? cam : Camera.main;

        // hook the world-space canvas to the camera
        if (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace)
        {
            _canvas.worldCamera = _camera;
        }

        // ----- Text fields -----
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
        //2D Image
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
        //3D Model
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

    public void Close()
    {
        // tell selector that this card is closing so it can stop breathing
        if (_selector != null && _ownerPoint != null)
        {
            _selector.OnCardClosed(this, _ownerPoint);
        }

        Destroy(gameObject);
    }
}
