using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

/// <summary>
/// Base class for labels with fold buttons
/// </summary>
internal abstract class FoldableLabel : VisualElement
{
    internal Foldout m_foldout;
    private Toggle m_toggle;

    protected FoldableLabel(string text, bool isBold = false)
    {
        style.position = Position.Absolute;
        usageHints = UsageHints.DynamicTransform;
        style.flexGrow = 1.0f;
        style.flexDirection = FlexDirection.Row;

        m_foldout = new Foldout();
        m_foldout.text = text;
        m_foldout.value = true; // Expanded by default
        if (isBold)
            m_foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
        Add(m_foldout);

        // Cache the toggle element for showing/hiding
        m_toggle = m_foldout.Q<Toggle>();
    }

    internal string text
    {
        get => m_foldout.text;
        set => m_foldout.text = value;
    }

    /// <summary>
    /// Shows or hides the fold toggle. When hidden, only the label text is displayed.
    /// </summary>
    internal void SetFoldable(bool foldable)
    {
        if (m_toggle != null)
        {
            m_toggle.style.display = foldable ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

internal class GroupLabel : FoldableLabel
{
    internal FixedString128Bytes m_groupName;
    internal System.Action<FixedString128Bytes> m_onFoldToggled;

    internal GroupLabel(string text) : base(text, isBold: true)
    {
        // Register callback once - it will use the current m_groupName value
        m_foldout.RegisterValueChangedCallback(evt => m_onFoldToggled?.Invoke(m_groupName));
    }
}

internal class ThreadLabel : FoldableLabel
{
    internal ulong m_threadId;
    internal System.Action<ulong> m_onFoldToggled;

    internal ThreadLabel(string text) : base(text, isBold: false)
    {
        // Register callback once - it will use the current m_threadId value
        m_foldout.RegisterValueChangedCallback(evt => m_onFoldToggled?.Invoke(m_threadId));
    }
}

internal class ThreadLabels : VisualElement
{
    List<ThreadLabel> m_threadLabels;
    List<GroupLabel> m_groupLabels;
    bool m_visible = true;
    System.Action<ulong> m_onThreadFoldToggled;
    System.Action<FixedString128Bytes> m_onGroupFoldToggled;

    internal ThreadLabels()
    {
        m_threadLabels = new List<ThreadLabel>(128);
        m_groupLabels = new List<GroupLabel>(16);

        style.overflow = Overflow.Hidden;
        style.flexGrow = 1.0f;

        generateVisualContent += OnGenerateVisualContent;
    }

    internal void SetThreadFoldCallback(System.Action<ulong> callback)
    {
        m_onThreadFoldToggled = callback;
    }

    internal void SetGroupFoldCallback(System.Action<FixedString128Bytes> callback)
    {
        m_onGroupFoldToggled = callback;
    }

    /// <summary>
    /// Converts internal group names to display names.
    /// </summary>
    static string GetGroupDisplayName(in FixedString128Bytes name)
    {
        // Convert singular to plural for better readability
        if (name == "Job")
            return "Jobs";
        if (name == "Background Job")
            return "Background Jobs";
        return name.ToString();
    }

    void OnThreadFoldButtonClicked(ulong threadId)
    {
        m_onThreadFoldToggled?.Invoke(threadId);
    }

    void OnGroupFoldButtonClicked(FixedString128Bytes groupName)
    {
        m_onGroupFoldToggled?.Invoke(groupName);
    }

    internal void Hide()
    {
        foreach (var label in m_threadLabels)
            label.style.visibility = Visibility.Hidden;

        foreach (var label in m_groupLabels)
            label.style.visibility = Visibility.Hidden;

        m_visible = false;
    }

    internal void Show()
    {
        m_visible = true;
    }

    internal void DrawThreadLines(MeshGenerationContext ctx, float width, float height)
    {
        if (!m_visible)
            return;

        ctx.painter2D.strokeColor = new Color32(60, 60, 60, 255);
        ctx.painter2D.lineWidth = 1.0f;

        foreach (var label in m_threadLabels)
        {
            // Skip hidden labels (e.g., when their group is folded)
            if (label.style.visibility == Visibility.Hidden)
                continue;

            var pos = label.resolvedStyle.translate;

            // Skip labels that are outside view
            if (pos.y < 0.0f || pos.y > height)
                continue;

            ctx.painter2D.BeginPath();
            ctx.painter2D.MoveTo(new Vector2(0.0f, pos.y - 5.0f));
            ctx.painter2D.LineTo(new Vector2(width, pos.y - 5.0f));
            ctx.painter2D.Stroke();
        }
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        DrawThreadLines(ctx, resolvedStyle.width, resolvedStyle.height);
    }

    internal void Update(in NativeHashMap<ulong, ThreadPosition> threadOffsets, in FrameData frameData, in NativeArray<ThreadGroupInfo> threadGroupOrder, float4x4 mat)
    {
        if (!m_visible)
            return;

        float y = resolvedStyle.transformOrigin.y;

        // Build a set of thread IDs that are alone in their group (no need for fold toggle)
        HashSet<ulong> singleThreadGroupIds = new HashSet<ulong>();
        foreach (var group in frameData.threadGroups)
        {
            int threadCount = group.arrayEnd - group.arrayIndex;
            if (threadCount == 1)
            {
                singleThreadGroupIds.Add(frameData.threads[group.arrayIndex].threadId);
            }
        }

        // Ensure we have enough group labels
        for (int i = m_groupLabels.Count; i < threadGroupOrder.Length; ++i)
        {
            var label = new GroupLabel("");
            label.m_onFoldToggled = OnGroupFoldButtonClicked;
            m_groupLabels.Add(label);
            Add(label);
        }

        // Ensure we have enough thread labels
        for (int i = m_threadLabels.Count; i < frameData.threads.Length; ++i)
        {
            var label = new ThreadLabel("");
            label.m_onFoldToggled = OnThreadFoldButtonClicked;
            m_threadLabels.Add(label);
            Add(label);
        }

        int threadLabelIndex = 0;

        // Render thread labels
        foreach (var thread in frameData.threads)
        {
            ThreadPosition threadPos;

            if (threadOffsets.TryGetValue(thread.threadId, out threadPos))
            {
                ThreadLabel label = m_threadLabels[threadLabelIndex];

                // Hide labels for threads in preview mode (folded group compact view)
                if (threadPos.visibility == ThreadVisibility.Preview)
                {
                    label.style.visibility = Visibility.Hidden;
                    threadLabelIndex++;
                    continue;
                }

                if (label.text != thread.name)
                    label.text = thread.name.ToString();

                // Update thread ID (callback will use current value)
                label.m_threadId = thread.threadId;

                // Hide fold toggle for threads that are alone in their group
                label.SetFoldable(!singleThreadGroupIds.Contains(thread.threadId));

                // Update fold state (Foldout.value: true = expanded, false = collapsed)
                label.m_foldout.SetValueWithoutNotify(!threadPos.isFolded);

                float3 posTemp = new float3(0.0f, threadPos.offset, 0.0f);
                float3 pos = transform(mat, posTemp);
                // Indent thread labels under their group (tree view style)
                label.style.translate = new StyleTranslate(new Translate(new Length(20.0f), new Length(pos.y + 5.0f), 0.0f));
                label.style.visibility = Visibility.Visible;

                threadLabelIndex++;
            }
        }

        // Render group labels using the pre-calculated group offset
        int groupLabelIndex = 0;
        foreach (var groupInfo in threadGroupOrder)
        {
            GroupLabel groupLabel = m_groupLabels[groupLabelIndex];

            // Update label text (use display name for UI)
            string displayName = GetGroupDisplayName(groupInfo.name);
            if (groupLabel.text != displayName)
                groupLabel.text = displayName;

            // Update group name (callback will use current value)
            groupLabel.m_groupName = groupInfo.name;

            // Update fold state (Foldout.value: true = expanded, false = collapsed)
            groupLabel.m_foldout.SetValueWithoutNotify(!groupInfo.isFolded);

            // Use the group's stored offset for positioning
            float3 posTemp = new float3(0.0f, groupInfo.offset, 0.0f);
            float3 pos = transform(mat, posTemp);
            groupLabel.transform.position = new Vector3(10.0f, pos.y + 5.0f, 0.0f);
            groupLabel.style.visibility = Visibility.Visible;

            groupLabelIndex++;
        }

        // Hide unused thread labels
        for (int i = threadLabelIndex; i < m_threadLabels.Count; ++i)
        {
            m_threadLabels[i].style.visibility = Visibility.Hidden;
        }

        // Hide unused group labels
        for (int i = groupLabelIndex; i < m_groupLabels.Count; ++i)
        {
            m_groupLabels[i].style.visibility = Visibility.Hidden;
        }

        MarkDirtyRepaint();
    }

    ThreadGroup FindGroup(in FrameData frame, in FixedString128Bytes name)
    {
        foreach (var group in frame.threadGroups)
        {
            if (group.name == name)
                return group;
        }

        throw new System.ArgumentException("Group not found");
    }
}
