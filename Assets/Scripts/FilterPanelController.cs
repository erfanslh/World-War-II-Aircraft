using UnityEngine;
using UnityEngine.UI;

public class FilterPanelController : MonoBehaviour
{
    [Header("References")]
    public GameObject filterPanel;     // FiltersColumn
    public Button openButton;          // OpenFiltersButton
    public Button closeButton;         // CloseFiltersButton

    private void Awake()
    {
        Debug.Log("[FilterPanelController] Awake");

        if (openButton != null)
        {
            openButton.onClick.AddListener(ShowPanel);
            Debug.Log("[FilterPanelController] Hooked OPEN button");
        }
        else
        {
            Debug.LogWarning("[FilterPanelController] openButton is NULL");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
            Debug.Log("[FilterPanelController] Hooked CLOSE button");
        }
        else
        {
            Debug.LogWarning("[FilterPanelController] closeButton is NULL");
        }
    }

    private void Start()
    {
        Debug.Log("[FilterPanelController] Start -> HidePanel");
        HidePanel();
    }

    public void ShowPanel()
    {
        Debug.Log("[FilterPanelController] ShowPanel called");
        if (filterPanel != null) filterPanel.SetActive(true);
    }

    public void HidePanel()
    {
        Debug.Log("[FilterPanelController] HidePanel called");
        if (filterPanel != null) filterPanel.SetActive(false);
    }
}
