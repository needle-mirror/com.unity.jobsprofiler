using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

internal class ThreadLabel : TextElement
{
    internal ThreadLabel(string text)
    {
        style.position = Position.Absolute;
        usageHints = UsageHints.DynamicTransform;
        style.flexGrow = 1.0f;

        this.text = text;
    }
}

internal class ThreadLabels : VisualElement
{
    List<ThreadLabel> m_threadLabels;
    bool m_visible = true;

    internal ThreadLabels()
    {
        m_threadLabels = new List<ThreadLabel>(128);

        style.overflow = Overflow.Hidden;
        style.flexGrow = 1.0f;

        generateVisualContent += OnGenerateVisualContent;
    }

    internal void Hide()
    {
        foreach (ThreadLabel label in m_threadLabels)
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
            var pos = label.transform.position;

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

    internal void Update(in NativeHashMap<ulong, ThreadPosition> threadOffsets, in FrameData frameData, float4x4 mat)
    {
        if (!m_visible)
            return;

        float y = resolvedStyle.transformOrigin.y;

        // Add new lables if we don't have enough
        for (int i = m_threadLabels.Count; i < frameData.threads.Length; ++i)
        {
            var label = new ThreadLabel("");
            m_threadLabels.Add(label);
            Add(label);
        }

        //float threadOffsetGroup = 0.0f;
        int labelIndex = 0;

        foreach (var thread in frameData.threads)
        {
            ThreadPosition threadPos;

            if (threadOffsets.TryGetValue(thread.threadId, out threadPos))
            {
                if (m_threadLabels[labelIndex].text != thread.name)
                    m_threadLabels[labelIndex].text = thread.name.ToString();

                float3 posTemp = new float3(0.0f, threadPos.offset, 0.0f);
                float3 pos = transform(mat, posTemp);
                m_threadLabels[labelIndex].transform.position = new Vector3(10.0f, pos.y + 5.0f, 0.0f);
                m_threadLabels[labelIndex].style.visibility = Visibility.Visible;

                labelIndex++;
            }
        }


        /*
        foreach (var groupOrder in groups)
        {
            ThreadGroup group = FindGroup(frameData, groupOrder.name);

            float threadOffset = threadOffsetGroup;

            // TODO: Here we can check if the whole group is visible or not
            for (int threadIndex = group.arrayIndex; threadIndex < group.arrayEnd; ++threadIndex)
            {
                ThreadInfo thread = frameData.threads[threadIndex];

                if (m_threadLabels[labelIndex].text != thread.name)
                    m_threadLabels[labelIndex].text = thread.name.ToString();

                float3 posTemp = new float3(0.0f, threadOffset, 0.0f);
                float3 pos = transform(mat, posTemp);
                m_threadLabels[labelIndex].transform.position = new Vector3(10.0f, pos.y + 5.0f, 0.0f);
                m_threadLabels[labelIndex].style.visibility = Visibility.Visible;

                threadOffset += thread.maxDepth;
                labelIndex++;
            }

            if (groupOrder.maxDepth != 0)
                threadOffsetGroup += groupOrder.maxDepth;
            else
                threadOffsetGroup += threadOffset;
        }
        */

        for (int i = labelIndex; i < m_threadLabels.Count; ++i)
        {
            m_threadLabels[i].style.visibility = Visibility.Hidden;
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
