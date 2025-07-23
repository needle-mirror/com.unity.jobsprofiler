using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal class DisplayDropdownMenu : GenericDropdownMenu
{
    private ListView m_listView;
    private Button m_button;
    private TimelineSettings m_settings;

    const string k_zoomWhenChangeName = "Zoom when changing event";
    const string k_showDependsOnName = "Show Depends On";
    const string k_showDependantOnName = "Show Dependant On";
    const string k_showCompletedByWaitName = "Show Completed by (Wait)";
    const string k_showCompletedByNoWaitName = "Show Completed by (No Wait)";
    const string k_showFullDependencyChainName = "Show full dependency chain";

    internal DisplayDropdownMenu(TimelineSettings timelineSettings)
    {
        // Create and configure the ListView
        m_listView = new ListView
        {
            itemsSource = new string[]
            {
                k_zoomWhenChangeName,
                k_showDependsOnName,
                k_showDependantOnName,
                k_showCompletedByWaitName,
                k_showCompletedByNoWaitName,
                k_showFullDependencyChainName
            },
            fixedItemHeight = 16,
            makeItem = MakeItem,
            bindItem = BindItem
        };

        // Set ListView's container to the contentContainer of GenericDropdownMenu
        contentContainer.Add(m_listView);
        m_settings = timelineSettings;
    }

    void ChangeValue(ChangeEvent<bool> evt)
    {
        var t = evt.currentTarget as Toggle;

        if (t.text == k_zoomWhenChangeName)
            m_settings.zoomOnEventFocus = evt.newValue;
        else if (t.text == k_showDependsOnName)
            m_settings.showDependsOn = evt.newValue;
        else if (t.text == k_showDependantOnName)
            m_settings.showDependantOn = evt.newValue;
        else if (t.text == k_showCompletedByWaitName)
            m_settings.showCompletedByWait = evt.newValue;
        else if (t.text == k_showCompletedByNoWaitName)
            m_settings.showCompletedByNoWait = evt.newValue;
        else if (t.text == k_showFullDependencyChainName)
            m_settings.showFullDependencyChain = evt.newValue;
    }

    // Method to create a Toggle item
    private VisualElement MakeItem()
    {
        var t = new Toggle();
        t.RegisterCallback<ChangeEvent<bool>>(ChangeValue);
        return t;
    }

    // Method to bind data to the Toggle item
    private void BindItem(VisualElement element, int index)
    {
        var toggle = element as Toggle;
        if (toggle != null)
        {
            toggle.text = m_listView.itemsSource[index] as string;

            if (index == 0)
                toggle.value = m_settings.zoomOnEventFocus;
            else if (index == 1)
                toggle.value = m_settings.showDependsOn;
            else if (index == 2)
                toggle.value = m_settings.showDependantOn;
            else if (index == 3)
                toggle.value = m_settings.showCompletedByWait;
            else if (index == 4)
                toggle.value = m_settings.showCompletedByNoWait;
            else if (index == 5)
                toggle.value = m_settings.showFullDependencyChain;
        }
    }
    internal Button CreateButton(VisualElement parent)
    {
        m_button = new Button();
        m_button.text = "Display Settings";
        m_button.style.width = 224;
        m_button.style.minWidth = 224;
        m_button.clickable.clicked += () =>
        {
            DropDown(m_button.worldBound, m_button, true);
        };

        parent.Add(m_button);
        return m_button;
    }
    internal TimelineSettings TimelineSettings { get { return m_settings; } }
}
