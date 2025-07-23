using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal class ExperimentalSettings : GenericDropdownMenu
{
    private ListView m_listView;
    private Button m_button;
    private List<Toggle> m_list = new List<Toggle>();
    bool m_verticalZoom;
    bool m_zoomOnEventHover;

    internal ExperimentalSettings()
    {
        // Create and configure the ListView
        m_listView = new ListView
        {
            itemsSource = new string[]
            {
                "Vertical Zoom",
                "Zoom on event hover",
            },
            fixedItemHeight = 16,
            makeItem = MakeItem,
            bindItem = BindItem
        };

        // Set ListView's container to the contentContainer of GenericDropdownMenu
        contentContainer.Add(m_listView);
    }

    void ChangeValue(ChangeEvent<bool> evt)
    {
        m_verticalZoom = m_list[0].value;
        m_zoomOnEventHover = m_list[1].value;
    }

    // Method to create a Toggle item
    private VisualElement MakeItem()
    {
        var t = new Toggle();
        t.RegisterCallback<ChangeEvent<bool>>(ChangeValue);
        m_list.Add(t);
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
                toggle.value = m_verticalZoom;
            else if (index == 1)
                toggle.value = m_zoomOnEventHover;
        }
    }
    internal Button CreateButton(VisualElement parent)
    {
        m_button = new Button();
        m_button.text = "Experimental Settings";
        m_button.style.width = 224;
        m_button.style.minWidth = 224;
        m_button.clickable.clicked += () =>
        {
            DropDown(m_button.worldBound, m_button, true);
        };

        parent.Add(m_button);
        return m_button;
    }
    internal bool VerticalZoom { get { return m_verticalZoom; } }
    internal bool ZoomOnEventHover { get { return m_zoomOnEventHover; } }
}

