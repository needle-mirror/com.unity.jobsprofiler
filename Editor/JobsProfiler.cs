using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Networking.PlayerConnection;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;
using System.Threading;
using UnityEditorInternal;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Text;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;
using System.Globalization;
using NUnit.Framework.Constraints;
using UnityEditor.Compilation;

[assembly: InternalsVisibleTo("Unity.JobsProfiler.Editor.Tests")]

internal struct Settings
{
    internal static readonly float[] TickModulos = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 30000, 60000 };
    internal const string TickFormatMilliseconds = "{0}ms";
    internal const string TickFormatSeconds = "{0}s";
    internal const int TickLabelSeparation = 60;
    internal const int InitialLabelCount = 32;
}

internal class JobsProfiler : VisualElement
{
    TimelineBarView m_timelineView = null;
    FrameCache m_frameCache;
    Filter m_filter;
    ToolbarSearchField m_searchField;
    VisualElement m_timeline;
    VisualElement m_filterSpacer;
    long m_currentFrame = 0;

    internal void SelectFrame(long frame)
    {
        m_currentFrame = frame;
        m_timelineView.SetCurrentFrame((int)frame);
    }
    void OnGeometryChangedEvent(GeometryChangedEvent e)
    {
        if (m_timelineView != null)
            m_timelineView.Update();

        UpdateSearchBarPosition();
    }

    void UpdateSearchBarPosition()
    {
        // Position searchbar right edge to align with timeline right edge
        if (m_timeline != null && m_searchField != null && m_filterSpacer != null)
        {
            var timelineRect = m_timeline.worldBound;
            var parentRect = m_filterSpacer.parent.worldBound;
            var searchWidth = m_searchField.resolvedStyle.width;

            if (timelineRect.width > 0 && searchWidth > 0 && parentRect.width > 0)
            {
                // Convert timeline right edge to parent-relative coordinates
                float timelineRightRelative = timelineRect.xMax - parentRect.xMin;
                float spacerWidth = timelineRightRelative - searchWidth;

                if (spacerWidth > 0)
                    m_filterSpacer.style.width = spacerWidth;
            }
        }
    }

    internal void Update()
    {
        int firstFrame = ProfilerDriver.firstFrameIndex;
        int lastFrame = ProfilerDriver.lastFrameIndex;

        if (firstFrame == -1 || lastFrame == -1)
        {
            m_timelineView.ClearData();
            m_frameCache.ClearCache();
        }

        m_timelineView.Update();
        m_frameCache.Update();
        UpdateSearchBarPosition();
    }

    internal void ClearCaches()
    {
        m_timelineView.ClearData();
        m_frameCache.ClearCache();
    }

    void ClearedProfile()
    {
        ClearCaches();
    }
    internal void Create()
    {
        if (CompilationPipeline.codeOptimization == CodeOptimization.Debug ||
            !BurstCompiler.Options.EnableBurstCompilation)
        {
            if (EditorUtility.DisplayDialog("Script optimizations are disabled",
                "Currently scripting optimizations or Burst has been disabled and this will cause the Jobs Profiler to run much slower. " +
                "Do you wish to enable them?\n" +
                "Notice that after enabling them it may take a while before it's in effect depending on the size of your project. ",
                "Enable", "No",
                DialogOptOutDecisionType.ForThisSession, "JobsProfilerOptimizationsEnabled"))
            {
                BurstCompiler.Options.EnableBurstCompilation = true;
                CompilationPipeline.codeOptimization = CodeOptimization.Release;
            }
        }

        m_frameCache = new FrameCache();
        var path = AssetDatabase.GUIDToAssetPath("1677c8f5454ea9d4ab631bea655dcb48");
        var mainVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        var tree = mainVisualTree.Instantiate();

        m_searchField = tree.Query<ToolbarSearchField>("filter").First();
        m_filterSpacer = tree.Query<VisualElement>("filter_spacer").First();
        m_filterSpacer.style.flexGrow = 0;

        VisualElement mv = tree.Query<VisualElement>("main_view").First();
        mv.style.flexGrow = 1;
        tree.style.flexGrow = 1;
        tree.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1.0f);

        m_filter = new Filter(m_frameCache, m_searchField);

        m_timelineView = new TimelineBarView(mv, tree, m_frameCache, m_filter);

        // Get reference to timeline element for dynamic searchbar sizing
        m_timeline = m_timelineView.Query<VisualElement>("timeline").First();
        m_timelineView.style.display = DisplayStyle.Flex;
        m_timelineView.SetCurrentFrame((int)m_currentFrame);
        m_timelineView.m_stats.Show();

        Add(tree);

        EditorApplication.update += Update;
        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);

        ProfilerDriver.profileCleared += ClearedProfile;
    }
}

internal class JobsProfilerViewController : ProfilerModuleViewController
{
    internal JobsProfilerViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }
    private JobsProfiler m_jobsProfiler;

    void LoadedProfile()
    {
        m_jobsProfiler.ClearCaches();
        m_jobsProfiler.SelectFrame((int)ProfilerWindow.selectedFrameIndex);
    }

    protected override VisualElement CreateView()
    {
        int selectedFrame = (int)ProfilerWindow.selectedFrameIndex;

        m_jobsProfiler = new JobsProfiler();
        m_jobsProfiler.Create();

        ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
        m_jobsProfiler.style.flexShrink = 1;
        m_jobsProfiler.style.flexGrow = 1;
        m_jobsProfiler.SelectFrame(selectedFrame);

        ProfilerDriver.profileLoaded += LoadedProfile;

        return m_jobsProfiler;
    }

    void OnSelectedFrameIndexChanged(long selectedFrameIndex)
    {
        m_jobsProfiler.SelectFrame(selectedFrameIndex);
    }
}

[Serializable]
[ProfilerModuleMetadata("Jobs Profiler")]
internal class JobsProfilerModule : ProfilerModule
{
    static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
    {
        new ProfilerCounterDescriptor("Rendering", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Scripts", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Physics", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Animation", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("GarbageCollector", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("VSync", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Global Illumination", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("UI", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Others", ProfilerCategory.Scripts),
        //new ProfilerCounterDescriptor("Main Thread", ProfilerCategory.Internal)
    };

    public JobsProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea) { }
    public override ProfilerModuleViewController CreateDetailsViewController()
    {
        return new JobsProfilerViewController(ProfilerWindow);
    }
}
