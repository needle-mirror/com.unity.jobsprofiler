using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
    Label m_frameLabel;
    long m_currentFrame = 0;

    internal void SelectFrame(long frame)
    {
        // + 1 here to match the frame selectors way of counting
        m_frameLabel.text = "Frame " + (frame + 1);
        m_currentFrame = frame;
        m_timelineView.SetCurrentFrame((int)frame);
    }
    void OnGeometryChangedEvent(GeometryChangedEvent e)
    {
        if (m_timelineView != null)
            m_timelineView.Update();
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

        TextField filter = tree.Query<TextField>("filter").First();

        VisualElement mv = tree.Query<VisualElement>("main_view").First();
        mv.style.flexGrow = 1;
        tree.style.flexGrow = 1;

        m_filter = new Filter(m_frameCache, filter);

        m_timelineView = new TimelineBarView(mv, tree, m_frameCache, m_filter);
        m_timelineView.style.display = DisplayStyle.Flex;
        m_timelineView.SetCurrentFrame((int)m_currentFrame);
        m_timelineView.m_stats.Show();

        Add(tree);

        m_frameLabel = this.Query<Label>("main_frame").First();

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
