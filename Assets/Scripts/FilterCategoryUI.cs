using UnityEngine.UI;
using UnityEngine;

public class FilterCategoryUI : MonoBehaviour
{
    public Button headerButton;
    public GameObject optionsPanel;

    private bool _isOpen;

    private void Awake()
    {
        // Start closed
        SetOpen(false);

        if (headerButton != null)
            headerButton.onClick.AddListener(ToggleOpen);
    }

    public void ToggleOpen()
    {
        SetOpen(!_isOpen);
        Debug.Log($"[FilterCategoryUI] {name} clicked; open now = {!_isOpen}");
        SetOpen(!_isOpen);
    }

    public void SetOpen(bool open)
    {
        _isOpen = open;

        if (optionsPanel != null)
            optionsPanel.SetActive(open);

        var parent = transform.parent as RectTransform;
        if (parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }
}
