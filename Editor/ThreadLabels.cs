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
    internal TextElement m_textElement;
    internal Button m_foldButton;

    protected FoldableLabel(string text, bool isBold = false)
    {
        style.position = Position.Absolute;
        usageHints = UsageHints.DynamicTransform;
        style.flexGrow = 1.0f;
        style.flexDirection = FlexDirection.Row;

        // Fold button (arrow)
        m_foldButton = new Button();
        m_foldButton.text = "▼"; // Expanded by default
        m_foldButton.style.width = 16;
        m_foldButton.style.height = 16;
        m_foldButton.style.fontSize = 10;
        m_foldButton.style.paddingLeft = 0;
        m_foldButton.style.paddingRight = 0;
        m_foldButton.style.paddingTop = 0;
        m_foldButton.style.paddingBottom = 0;
        m_foldButton.style.marginLeft = 0;
        m_foldButton.style.marginRight = 2;
        Add(m_foldButton);

        // Text label
        m_textElement = new TextElement();
        m_textElement.text = text;
        m_textElement.style.flexGrow = 1;
        if (isBold)
            m_textElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        Add(m_textElement);
    }

    internal string text
    {
        get => m_textElement.text;
        set => m_textElement.text = value;
    }
}

internal class GroupLabel : FoldableLabel
{
    internal FixedString128Bytes m_groupName;

    internal GroupLabel(string text) : base(text, isBold: true)
    {
    }
}

internal class ThreadLabel : FoldableLabel
{
    internal ulong m_threadId;

    internal ThreadLabel(string text) : base(text, isBold: false)
    {
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

        // Ensure we have enough group labels
        for (int i = m_groupLabels.Count; i < threadGroupOrder.Length; ++i)
        {
            var label = new GroupLabel("");
            m_groupLabels.Add(label);
            Add(label);
        }

        // Ensure we have enough thread labels
        for (int i = m_threadLabels.Count; i < frameData.threads.Length; ++i)
        {
            var label = new ThreadLabel("");
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

                if (label.text != thread.name)
                    label.text = thread.name.ToString();

                // Store thread ID for fold button callback
                if (label.m_threadId != thread.threadId)
                {
                    label.m_threadId = thread.threadId;
                    // Clear all existing callbacks and add new one with captured threadId
                    ulong capturedThreadId = thread.threadId;
                    label.m_foldButton.clickable = new Clickable(() => OnThreadFoldButtonClicked(capturedThreadId));
                }

                // Update fold button arrow based on fold state
                label.m_foldButton.text = threadPos.isFolded ? "►" : "▼";

                float3 posTemp = new float3(0.0f, threadPos.offset, 0.0f);
                float3 pos = transform(mat, posTemp);
                label.style.translate = new StyleTranslate(new Translate(new Length(10.0f), new Length(pos.y + 5.0f), 0.0f));
                label.style.visibility = Visibility.Visible;

                threadLabelIndex++;
            }
        }

        // Render group labels using the pre-calculated group offset
        int groupLabelIndex = 0;
        foreach (var groupInfo in threadGroupOrder)
        {
            GroupLabel groupLabel = m_groupLabels[groupLabelIndex];

            // Update label text
            if (groupLabel.text != groupInfo.name)
                groupLabel.text = groupInfo.name.ToString();

            // Store group name for fold button callback
            if (groupLabel.m_groupName != groupInfo.name)
            {
                groupLabel.m_groupName = groupInfo.name;
                // Clear all existing callbacks and add new one with captured groupName
                FixedString128Bytes capturedGroupName = groupInfo.name;
                groupLabel.m_foldButton.clickable = new Clickable(() => OnGroupFoldButtonClicked(capturedGroupName));
            }

            // Update fold button arrow based on fold state
            groupLabel.m_foldButton.text = groupInfo.isFolded ? "►" : "▼";

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
