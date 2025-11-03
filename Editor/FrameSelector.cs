using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

struct FrameTime
{
    /// Time of this frame
    internal float time;
    /// Index of this frame
    internal int index;
}

internal struct SelectedFrameRange
{
    /// Used for selecting how many frames we have surrounding the active one
    internal const int k_SelectionRange = 3;
    /// Start frame in the selection
    internal int start;
    /// The "active" frame
    internal int active;
    /// end frame
    internal int end;
}

/// <summary>
/// Used to
/// </summary>
struct DisplayRange
{
    // Start of the samples to display
    internal int start;
    // end of the samples to display
    internal int end;
}

struct FrameSelectorSettings
{
    /// area we are drawing into
    internal Rect area;
    /// Selected frame, -1 if no selection
    internal int selecetedFrame;
    /// Width of the bars we render
    internal int barWidth;
    /// Spacing between the bars
    internal int barSpacing;
}

struct MouseData
{
    /// Position of the mouse (in window relative coords)
    internal float2 pos;
    /// Is left mouse button down
    internal int isDown;
}

struct GenerateFrameSelectionMesh : IJob
{
    [ReadOnly]
    internal NativeList<FrameTime> m_frameTimes;

    [ReadOnly]
    internal FrameSelectorSettings m_settings;

    // Range of the frames to display
    [ReadOnly]
    internal DisplayRange m_displayRange;

    /// Current mouse state and position
    [ReadOnly]
    internal MouseData m_mouse;

    [WriteOnly]
    internal NativeArray<Vertex> m_vertices;

    [WriteOnly]
    internal NativeArray<ushort> m_indices;

    [WriteOnly]
    internal NativeArray<int> m_counters;

    /// Keeps track of the selected frame(s)
    internal NativeReference<SelectedFrameRange> m_frameRange;

    void AddQuad(ref int vertexPos, ref int indexOffset, float2 c0, float2 c1, Color32 color)
    {
        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();
        Vertex v3 = new Vertex();

        v0.tint = color;
        v1.tint = color;
        v2.tint = color;
        v3.tint = color;

        v0.position.x = c0.x;
        v0.position.y = c0.y;

        v1.position.x = c1.x;
        v1.position.y = c0.y;

        v2.position.x = c1.x;
        v2.position.y = c1.y;

        v3.position.x = c0.x;
        v3.position.y = c1.y;

        // generate quad
        m_vertices[vertexPos + 0] = v0;
        m_vertices[vertexPos + 1] = v1;
        m_vertices[vertexPos + 2] = v2;
        m_vertices[vertexPos + 3] = v3;

        m_indices[indexOffset + 0] = (ushort)(vertexPos + 0);
        m_indices[indexOffset + 1] = (ushort)(vertexPos + 1);
        m_indices[indexOffset + 2] = (ushort)(vertexPos + 2);

        m_indices[indexOffset + 3] = (ushort)(vertexPos + 2);
        m_indices[indexOffset + 4] = (ushort)(vertexPos + 3);
        m_indices[indexOffset + 5] = (ushort)(vertexPos + 0);

        vertexPos += 4;
        indexOffset += 6;
    }

    public void Execute()
    {
        if (m_frameTimes.Length == 0)
            return;

        Color32 barColor = Color.blue;
        Color32 outsideColor = Color.white;
        Color32 activeColor = Color.white;

        outsideColor.a = 20;
        activeColor.a = 50;

        int vertexPos = 0;
        int indexOffset = 0;

        float height = m_settings.area.height;
        int barWidth = m_settings.barWidth;
        int barSpacing = m_settings.barSpacing;

        int xStep = barWidth + barSpacing;

        int maxBarCount = ((int)m_settings.area.width / xStep) + 1;

        int startRange = m_displayRange.start;
        int endRange = m_displayRange.end;

        // calculate the ranges we should render
        // TODO: Check limits

        float xpos = 0.0f;
        float2 c0;
        float2 c1;

        float maxTime = 0.0f;

        for (int i = startRange; i < endRange; ++i)
            maxTime = math.max(maxTime, m_frameTimes[i].time);

        float timeScale = 1.0f / maxTime;

        for (int i = startRange; i < endRange; ++i)
        {
            // TODO: Validate range
            float h = height - (m_frameTimes[i].time * timeScale * height);

            c0 = new float2(xpos, h);
            c1 = new float2(xpos + m_settings.barWidth, height);

            AddQuad(ref vertexPos, ref indexOffset, c0, c1, barColor);

            xpos += xStep;
        }

        int selectStart = m_frameRange.Value.start;
        int selectActive = m_frameRange.Value.active;
        int selectEnd = m_frameRange.Value.end;

        // If mouse is down we need to update the selection
        if (m_mouse.isDown == 1)
        {
            selectActive = startRange + (int)(m_mouse.pos.x / (float)xStep);
            selectEnd = math.min(selectActive + SelectedFrameRange.k_SelectionRange + 1, endRange);
            selectStart = math.max(selectActive - SelectedFrameRange.k_SelectionRange, startRange);
        }

        m_frameRange.Value = new SelectedFrameRange
        {
            start = selectStart,
            active = selectActive,
            end = selectEnd,
        };

        selectStart -= startRange;
        selectActive -= startRange;

        // This may draw outside of the window, but we let the system handle the clipping as it's very few polys anyway
        // draw left side of selection

        c0 = new float2(xStep * (selectStart + 0), 0.0f);
        c1 = new float2(xStep * (selectStart + SelectedFrameRange.k_SelectionRange), height);
        AddQuad(ref vertexPos, ref indexOffset, c0, c1, outsideColor);

        // Draw active

        float startX = xStep * selectActive;
        float endX = (xStep * selectActive) + barWidth;

        c0 = new float2(startX, 0.0f);
        c1 = new float2(endX, height);
        AddQuad(ref vertexPos, ref indexOffset, c0, c1, activeColor);

        c0 = new float2(endX, 0.0f);
        c1 = new float2(endX + barSpacing, height);
        AddQuad(ref vertexPos, ref indexOffset, c0, c1, outsideColor);

        // Draw right side selection

        c0 = new float2(xStep * (selectActive + 1), 0.0f);
        c1 = new float2(xStep * (selectActive + 1 + SelectedFrameRange.k_SelectionRange), height);
        AddQuad(ref vertexPos, ref indexOffset, c0, c1, outsideColor);

        m_counters[0] = vertexPos / 4;
    }
}

internal class FrameSelector : VisualElement
{
    NativeList<FrameTime> m_frameTimes;
    FrameSelectorSettings m_settings;
    NativeReference<SelectedFrameRange> m_selecetedFrameRange;
    MouseData m_mouseData;
    DisplayRange m_displayRange;
    Scroller m_frameScroller;

    /// Get the active frame selection
    internal SelectedFrameRange Selection { get { return m_selecetedFrameRange.Value; } }
    internal int FrameCount { get { return m_frameTimes.Length; } }

    internal FrameSelector(Scroller scroller)
    {
        style.flexGrow = 1.0f;
        generateVisualContent += OnGenerateVisualContent;
        m_frameTimes = new NativeList<FrameTime>(1024, Allocator.Persistent);
        m_selecetedFrameRange = new NativeReference<SelectedFrameRange>(Allocator.Persistent);
        m_mouseData = new MouseData();
        m_frameScroller = scroller;

        m_settings.barWidth = 6;
        m_settings.barSpacing = 1;

        m_selecetedFrameRange.Value = new SelectedFrameRange
        {
            start = 0,
            active = SelectedFrameRange.k_SelectionRange,
            end = SelectedFrameRange.k_SelectionRange * 2,
        };

        RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);

        /*
        // temp data for testing
        var rand = new System.Random();
        for (int i = 0; i < 1024; ++i)
            AddFrame((float)rand.NextDouble() * 0.1f, i);
        */
    }

    ~FrameSelector()
    {
        m_selecetedFrameRange.Dispose();
        m_frameTimes.Dispose();
    }

    /// Called from the outside when we want to clear all the frames
    internal void ClearFrames()
    {
        m_frameTimes.Clear();

        m_selecetedFrameRange.Value = new SelectedFrameRange
        {
            start = 0,
            active = SelectedFrameRange.k_SelectionRange,
            end = SelectedFrameRange.k_SelectionRange * 2,
        };
    }

    internal void AddFrame(float time, int index)
    {
        m_frameTimes.Add(new FrameTime { time = time, index = index });
    }

    internal void Update(bool isRecording)
    {
        int maxScreenItems = calculateMaxAreaItems();

        if (maxScreenItems > m_frameTimes.Length)
        {
            m_frameScroller.SetEnabled(false);
            m_displayRange.start = 0;
            m_displayRange.end = m_frameTimes.Length;
        }
        else
        {
            m_frameScroller.SetEnabled(true);
            m_frameScroller.slider.lowValue = 0;
            m_frameScroller.slider.highValue = m_frameTimes.Length - maxScreenItems;

            if (isRecording)
            {
                int end = m_frameTimes.Length;
                m_displayRange.start = math.max(0, end - maxScreenItems);
                m_displayRange.end = end;
                m_frameScroller.slider.value = end;

                // live update range also
                m_selecetedFrameRange.Value = new SelectedFrameRange
                {
                    start = math.max(0, end - SelectedFrameRange.k_SelectionRange * 2),
                    active = math.max(0, end - SelectedFrameRange.k_SelectionRange),
                    end = end,
                };
            }
            else
            {
                m_displayRange.start = (int)m_frameScroller.slider.value;
                m_displayRange.end = m_displayRange.start + maxScreenItems;
            }
        }
    }

    int calculateMaxAreaItems()
    {
        int areaWidth = math.max(1, (int)resolvedStyle.width);
        return (areaWidth / (m_settings.barWidth + m_settings.barSpacing)) + 1;
    }

    private void OnPointerDownEvent(PointerDownEvent evt)
    {
        m_mouseData.pos.x = evt.originalMousePosition.x;
        m_mouseData.pos.y = evt.originalMousePosition.y;
        m_mouseData.isDown = evt.button == 0 ? 1 : 0;
    }
    private void OnPointerUpEvent(PointerUpEvent evt)
    {
        m_mouseData.pos.x = evt.originalMousePosition.x;
        m_mouseData.pos.y = evt.originalMousePosition.y;
        m_mouseData.isDown = 0;
    }

    private void OnPointerMoveEvent(PointerMoveEvent evt)
    {
        m_mouseData.pos.x = evt.originalMousePosition.x;
        m_mouseData.pos.y = evt.originalMousePosition.y;
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        m_settings.area.x = resolvedStyle.translate.x;
        m_settings.area.y = resolvedStyle.translate.y;
        m_settings.area.width = resolvedStyle.width;
        //m_settings.area.height = resolvedStyle.height;
        m_settings.area.height = 190;

        // Max number of items that we can draw and the selection area
        int maxScreenItems = calculateMaxAreaItems() + (SelectedFrameRange.k_SelectionRange * 2) + 1;

        var vertices = new NativeArray<Vertex>(maxScreenItems * 4, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var indices = new NativeArray<ushort>(maxScreenItems * 6, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var counters = new NativeArray<int>(1, Allocator.TempJob);

        var job = new GenerateFrameSelectionMesh
        {
            m_frameTimes = m_frameTimes,
            m_settings = m_settings,
            m_vertices = vertices,
            m_indices = indices,
            m_counters = counters,
            m_mouse = m_mouseData,
            m_frameRange = m_selecetedFrameRange,
            m_displayRange = m_displayRange,
        };

        job.Schedule().Complete();

        int vertexCount = counters[0];

        if (vertexCount != 0)
        {
            var vertexArray = new NativeSlice<Vertex>(vertices, 0, vertexCount * 4);
            var indexArray = new NativeSlice<ushort>(indices, 0, vertexCount * 6);

            MeshWriteData mwd = mgc.Allocate(vertexArray.Length, indexArray.Length, null);

            mwd.SetAllVertices(vertexArray);
            mwd.SetAllIndices(indexArray);
        }

        counters.Dispose();
        indices.Dispose();
        vertices.Dispose();
    }
}
