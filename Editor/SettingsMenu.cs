using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Combined settings menu with kebab button (vertical dots) that contains
/// Display Settings and Experimental Settings in a single dropdown.
/// Uses native ToolbarMenu for proper sizing and checkmark support.
/// </summary>
internal class SettingsMenu
{
    private ToolbarMenu m_toolbarMenu;

    // Display settings
    bool m_zoomOnEventFocus = true;
    bool m_showDependsOn = true;
    bool m_showDependantOn = true;
    bool m_showCompletedByWait = true;
    bool m_showCompletedByNoWait = true;
    bool m_showFullDependencyChain;

    // Experimental settings
    bool m_verticalZoom;
    bool m_zoomOnEventHover;
    bool m_showFoldedGroupPreview = true;

    internal SettingsMenu()
    {
    }

    void RebuildMenu()
    {
        var menu = m_toolbarMenu.menu;

        // Clear existing items
        var count = menu.MenuItems().Count;
        for (int i = 0; i < count; i++)
            menu.RemoveItemAt(0);

        // Display Settings section
        menu.AppendAction("Display Settings", null, DropdownMenuAction.Status.Disabled);

        menu.AppendAction("Zoom when changing event",
            a => { m_zoomOnEventFocus = !m_zoomOnEventFocus; },
            a => m_zoomOnEventFocus ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show Depends On",
            a => { m_showDependsOn = !m_showDependsOn; },
            a => m_showDependsOn ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show Dependant On",
            a => { m_showDependantOn = !m_showDependantOn; },
            a => m_showDependantOn ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show Completed by (Wait)",
            a => { m_showCompletedByWait = !m_showCompletedByWait; },
            a => m_showCompletedByWait ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show Completed by (No Wait)",
            a => { m_showCompletedByNoWait = !m_showCompletedByNoWait; },
            a => m_showCompletedByNoWait ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show full dependency chain",
            a => { m_showFullDependencyChain = !m_showFullDependencyChain; },
            a => m_showFullDependencyChain ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        // Separator and Experimental Settings section
        menu.AppendSeparator();
        menu.AppendAction("Experimental Settings", null, DropdownMenuAction.Status.Disabled);

        menu.AppendAction("Vertical Zoom",
            a => { m_verticalZoom = !m_verticalZoom; },
            a => m_verticalZoom ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Zoom on event hover",
            a => { m_zoomOnEventHover = !m_zoomOnEventHover; },
            a => m_zoomOnEventHover ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendAction("Show folded group preview",
            a => { m_showFoldedGroupPreview = !m_showFoldedGroupPreview; },
            a => m_showFoldedGroupPreview ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
    }

    internal ToolbarMenu CreateKebabButton(VisualElement parent)
    {
        m_toolbarMenu = new ToolbarMenu
        {
            text = "\u22EE", // Vertical ellipsis (kebab menu icon)
            variant = ToolbarMenu.Variant.Popup
        };
        m_toolbarMenu.style.fontSize = 24;
        m_toolbarMenu.style.unityFontStyleAndWeight = FontStyle.Bold;
        m_toolbarMenu.style.paddingLeft = 6;
        m_toolbarMenu.style.paddingRight = 6;

        // Hide the dropdown arrow - we only want the kebab icon
        var arrow = m_toolbarMenu.Q(className: "unity-toolbar-menu__arrow");
        if (arrow != null)
            arrow.style.display = DisplayStyle.None;

        RebuildMenu();

        parent.Add(m_toolbarMenu);
        return m_toolbarMenu;
    }

    // Properties to access settings (used by Timeline)
    internal bool ZoomOnEventFocus => m_zoomOnEventFocus;
    internal bool ShowDependsOn => m_showDependsOn;
    internal bool ShowDependantOn => m_showDependantOn;
    internal bool ShowCompletedByWait => m_showCompletedByWait;
    internal bool ShowCompletedByNoWait => m_showCompletedByNoWait;
    internal bool ShowFullDependencyChain => m_showFullDependencyChain;
    internal bool VerticalZoom => m_verticalZoom;
    internal bool ZoomOnEventHover => m_zoomOnEventHover;
    internal bool ShowFoldedGroupPreview => m_showFoldedGroupPreview;
}
