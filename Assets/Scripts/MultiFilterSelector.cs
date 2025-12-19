using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiFilterSelector : MonoBehaviour
{
    [Header("Country toggles")]
    public Toggle[] countryToggles;

    [Header("Role toggles")]
    public Toggle[] roleToggles;

    [Header("State toggles")]
    public Toggle[] stateToggles;

    private AircraftPlotRootController _controller;

    private void Start()
    {
        _controller = AircraftPlotRootController.Instance;
        if (_controller == null)
        {
            Debug.LogWarning("[MultiFilterSelector] No AircraftPlotRootController.Instance at Start.");
        }

        // Set everything to OFF initially
        SetAllOff(countryToggles);
        SetAllOff(roleToggles);
        SetAllOff(stateToggles);

        // Hook listeners
        HookToggles(countryToggles, "Country");
        HookToggles(roleToggles, "Role");
        HookToggles(stateToggles, "State");

        // Apply once -> empty lists => no filters => show all data
        ApplyFilters();
    }

    private void SetAllOff(Toggle[] toggles)
    {
        if (toggles == null) return;
        foreach (var t in toggles)
        {
            if (t == null) continue;
            t.SetIsOnWithoutNotify(false);
        }
    }

    private void HookToggles(Toggle[] toggles, string groupName)
    {
        if (toggles == null) return;

        foreach (var t in toggles)
        {
            if (t == null) continue;

            t.onValueChanged.AddListener(_ =>
            {
                Debug.Log($"[MultiFilterSelector] {groupName} toggle changed: {GetToggleLabel(t)} = {t.isOn}");
                ApplyFilters();
            });
        }
    }

    private string GetToggleLabel(Toggle t)
    {
        var tmp = t.GetComponentInChildren<TMP_Text>();
        return tmp != null ? tmp.text.Trim() : t.name;
    }

    private List<string> GetSelected(Toggle[] toggles)
    {
        var result = new List<string>();
        if (toggles == null) return result;

        foreach (var t in toggles)
        {
            if (t != null && t.isOn)
                result.Add(GetToggleLabel(t));
        }

        return result;
    }

    private void ApplyFilters()
    {
        if (_controller == null)
        {
            _controller = AircraftPlotRootController.Instance;
            if (_controller == null)
            {
                Debug.LogWarning("[MultiFilterSelector] Still no controller, skipping ApplyFilters.");
                return;
            }
        }

        var selectedCountries = GetSelected(countryToggles);
        var selectedRoles = GetSelected(roleToggles);
        var selectedStates = GetSelected(stateToggles);

        Debug.Log($"[MultiFilterSelector] ApplyFilters => " +
                  $"{selectedCountries.Count} countries, " +
                  $"{selectedRoles.Count} roles, " +
                  $"{selectedStates.Count} states");

        _controller.SetMultiFilters(selectedCountries, selectedRoles, selectedStates);
    }

    // Reset button
    public void ClearFilters()
    {
        SetAllOff(countryToggles);
        SetAllOff(roleToggles);
        SetAllOff(stateToggles);
        ApplyFilters();   // back to full dataset
    }
}

