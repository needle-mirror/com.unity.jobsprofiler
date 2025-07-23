using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.Collections;
using System.Text;
using System;
using UnityEngine.UIElements.Experimental;

internal class JobsList
{
    struct JobListData
    {
        internal string name;
        internal float time;
        internal int markerId;
        internal int eventIndex;
        internal int frameIndex;
    }

    const int kRowShift = 16;
    const int kRowMask = (1 << kRowShift) - 1;
    ClickLinkEvent m_clickOrHoverEvent = new ClickLinkEvent();
    JobSelection m_jobSelection = new JobSelection();

    MultiColumnListView m_view;
    List<JobListData> m_data = new List<JobListData>();

    static string[] m_columnNames =
    {
        "Name",
        "Time",
    };

    void ColumnData(VisualElement element, int row)
    {
        Label label = element as Label;
        int columnIndex = (int)element.userData;

        if (columnIndex == 0)
        {
            label.text = String.Format("<link=\"{0}\"><color=#40a0ff><u>{1}</u></color></link>",
                                                (row << kRowShift) + columnIndex, m_data[row].name);
        }
        else if (columnIndex == 1)
        {
            label.text = String.Format("<link=\"{0}\"><color=#40a0ff><u>{1:0.000} ms</u></color></link>",
                                      (row << kRowShift) + columnIndex, m_data[row].time);
        }
    }

    internal JobsList(VisualElement root, StyleSheet styleSheet, string serializeName)
    {
        int i = 0;

        var columns = new Columns();

        foreach (var name in m_columnNames)
        {
            var c = new Column();
            c.name = name;
            c.title = name;
            var index = i++;
            c.makeCell = () =>
            {
                var label = new Label();
                label.RegisterCallback<PointerUpLinkTagEvent>(LinkUpClicked);
                label.RegisterCallback<PointerOverLinkTagEvent>(HyperlinkOnPointerOver);
                label.RegisterCallback<PointerOutLinkTagEvent>(HyperlinkOnPointerOut);
                label.styleSheets.Add(styleSheet);
                label.userData = index;
                return label;
            };
            c.bindCell = ColumnData;
            c.stretchable = true;
            columns.Add(c);
        }

        m_view = new MultiColumnListView(columns);
        m_view.viewDataKey = serializeName;
        m_view.itemsSource = m_data;
        m_view.style.flexGrow = 1;
#if UNITY_2023_3_OR_NEWER
        m_view.sortingMode = ColumnSortingMode.Custom;
#else
        m_view.sortingEnabled = true;
#endif
        m_view.columnSortingChanged += SortingChanged;
        m_view.style.borderTopColor = new StyleColor(Color.gray);
        m_view.style.borderTopWidth = 1;
        m_view.style.borderLeftWidth = 1;
        m_view.style.borderRightWidth = 1;
        m_view.style.borderBottomWidth = 1;
        m_view.style.borderLeftColor = new StyleColor(Color.black);
        m_view.style.borderRightColor = new StyleColor(Color.black);
        m_view.style.borderBottomColor = new StyleColor(Color.black);

        root.Add(m_view);
    }

    void SortingChanged()
    {
        foreach (var v in m_view.sortedColumns)
        {
            if (v.columnName == "Name")
                if (v.direction == SortDirection.Ascending)
                    m_data.Sort((a, b) => (a.name.CompareTo(b.name)));
                else
                    m_data.Sort((a, b) => (b.name.CompareTo(a.name)));
            else if (v.columnName == "Time")
                if (v.direction == SortDirection.Ascending)
                    m_data.Sort((a, b) => (a.time.CompareTo(b.time)));
                else
                    m_data.Sort((a, b) => (b.time.CompareTo(a.time)));
        }

        m_view.RefreshItems();
    }

    internal void UpdateData(in NativeList<DependJobInfo> jobs, FrameCache frameCache, JobSelection jobSelection)
    {
        m_data.Clear();
        m_jobSelection = jobSelection;

        for (int i = 0; i < jobs.Length; i++)
        {
            DependJobInfo info = jobs[i];
            if (info.eventIndex == -1)
                continue;

            FrameData frame;

            if (!frameCache.GetFrame(info.frameIndex, out frame))
                continue;

            int markerId = frame.events[info.eventIndex].markerId;
            float time = frame.events[(int)info.eventIndex].time;

            var text = frameCache.GetEventStringForFrame(info.frameIndex, info.eventIndex);

            var data = new JobListData
            {
                name = text,
                time = time,
                markerId = markerId,
                eventIndex = info.eventIndex,
                frameIndex = info.frameIndex,
            };

            m_data.Add(data);
        }

        m_view.RefreshItems();
    }

    internal void Clear()
    {
        m_data.Clear();
        m_view.RefreshItems();
    }

    void UpdateSelection(int markerId, int eventIndex, int frameIndex, bool hover)
    {
        m_clickOrHoverEvent.jobId = markerId;
        m_clickOrHoverEvent.frameIndex = frameIndex;
        m_clickOrHoverEvent.eventIndex = eventIndex;
        m_clickOrHoverEvent.hover = hover;
    }

    void HoverOrClick(int linkID, bool hover)
    {
        var row = linkID >> kRowShift;
        JobListData data = m_data[row];
        UpdateSelection(data.markerId, data.eventIndex, data.frameIndex, hover);
    }

    internal void HyperlinkOnPointerOver(PointerOverLinkTagEvent evt)
    {
        var linkID = int.Parse(evt.linkID);
        HoverOrClick(linkID, true);

        (evt.currentTarget as Label).AddToClassList("link-cursor");
    }
    internal void HyperlinkOnPointerOut(PointerOutLinkTagEvent evt)
    {
        UpdateSelection(
            m_jobSelection.eventIndex,
            m_jobSelection.eventIndex,
            m_jobSelection.frameIndex,
            false);

        (evt.target as Label).RemoveFromClassList("link-cursor");
    }

    void LinkUpClicked(PointerUpLinkTagEvent evt)
    {
        var linkID = int.Parse(evt.linkID);
        HoverOrClick(linkID, false);
    }

    internal ClickLinkEvent ClickOrHoverEvent { get { return m_clickOrHoverEvent; } }

    internal void ClearClickOrHoverEvent()
    {
        m_clickOrHoverEvent.jobId = 0;
        m_clickOrHoverEvent.frameIndex = -1;
        m_clickOrHoverEvent.eventIndex = -1;
        m_clickOrHoverEvent.hover = false;
    }
}

internal class JobsInfoPanel
{
    Label m_jobName;
    Label m_jobTime;
    Label m_scheduledBy;
    Label m_completedBy;

    JobsList m_dependsOn;
    JobsList m_dependantOn;

    VisualElement m_infoPanelData;

    internal JobsInfoPanel(in VisualElement root, StyleSheet labelStyleSheet)
    {
        m_jobName = root.Query<Label>("job_name").First();

        m_scheduledBy = root.Query<Label>("scheduled_by").First();
        m_completedBy = root.Query<Label>("completed_by").First();
        m_jobTime = root.Query<Label>("job_time").First();
        m_infoPanelData = root.Query<VisualElement>("jobs_info_data");

        m_scheduledBy.RegisterCallback<PointerOverLinkTagEvent>(Stats.HyperlinkOnPointerOver);
        m_scheduledBy.RegisterCallback<PointerOutLinkTagEvent>(Stats.HyperlinkOnPointerOut);

        m_completedBy.RegisterCallback<PointerOverLinkTagEvent>(Stats.HyperlinkOnPointerOver);
        m_completedBy.RegisterCallback<PointerOutLinkTagEvent>(Stats.HyperlinkOnPointerOut);

        var dependsOnArea = root.Query<VisualElement>("depends_on_list_area").First();
        var dependantOnArea = root.Query<VisualElement>("dependant_on_list_area").First();

        m_dependsOn = new JobsList(dependsOnArea, labelStyleSheet, "JobsProfiler.jobsDependsOn");
        m_dependantOn = new JobsList(dependantOnArea, labelStyleSheet, "JobsProfiler.jobsDependantOn");
    }

    internal void Update(
        in NativeList<DependJobInfo> dependsOnJobs,
        in NativeList<DependJobInfo> dependantOnJobs,
        JobSelection jobSelection,
        FrameCache frameCache)
    {
        m_dependsOn.UpdateData(dependsOnJobs, frameCache, jobSelection);
        m_dependantOn.UpdateData(dependantOnJobs, frameCache, jobSelection);
    }

    internal ClickLinkEvent GetClickOrHoverEvent()
    {
        ClickLinkEvent e0 = m_dependsOn.ClickOrHoverEvent;

        if (e0.jobId != 0)
        {
            m_dependsOn.ClearClickOrHoverEvent();
            return e0;
        }

        ClickLinkEvent e1 = m_dependantOn.ClickOrHoverEvent;

        if (e1.jobId != 0)
        {
            m_dependantOn.ClearClickOrHoverEvent();
            return e1;
        }

        return new ClickLinkEvent
        {
            jobId = 0,
            frameIndex = -1,
            eventIndex = -1,
            hover = false,
        };
    }

    internal void Activate()
    {
        m_scheduledBy.SetEnabled(true);
        m_completedBy.SetEnabled(true);
        m_jobTime.SetEnabled(true);
        m_jobName.SetEnabled(true);
        m_infoPanelData.visible = true;
    }

    internal void Deactivate()
    {
        m_jobName.text = "No event selected.";
        m_jobTime.text = "";
        m_scheduledBy.text = "N/A";
        m_completedBy.text = "N/A";
        m_infoPanelData.visible = false;
        m_jobTime.SetEnabled(false);
        m_jobName.SetEnabled(false);
        ClearLists();
    }

    internal void ClearLists()
    {
        m_scheduledBy.SetEnabled(false);
        m_completedBy.SetEnabled(false);
        m_dependsOn.Clear();
        m_dependantOn.Clear();
    }

    internal void ClearData()
    {
        m_jobName.text = string.Empty;
        m_scheduledBy.text = string.Empty;
        m_completedBy.text = string.Empty;
    }

    internal Label JobName { get { return m_jobName; } }
    internal Label JobTime { get { return m_jobTime; } }

    internal Label CompletedBy { get { return m_completedBy; } }
    internal Label ScheduledBy { get { return m_scheduledBy; } }

    internal JobsList DependsOn { get { return m_dependsOn; } }
    internal JobsList DependantOn { get { return m_dependantOn; } }
}
