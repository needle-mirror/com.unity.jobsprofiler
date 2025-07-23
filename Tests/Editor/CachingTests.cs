using NUnit.Framework;
using System.IO;
using Unity.Profiling;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using Unity.Jobs;

namespace Unity.JobsProfiler.Tests
{
    struct TestJob : IJob
    {
        public void Execute()
        {
        }
    }
    struct TestJob2 : IJob
    {
        public void Execute()
        {
        }
    }

    internal class TestWithClosedProfilerWindow
    {
        [SetUp]
        public void EnsureProfilerWindowClosed()
        {
            // Make sure that ProfilerWindow doesn't affect tests
            // As when its open, it changes profiler state on domain reload
            // By applying settings in Awake/OnEnable
            if (EditorWindow.HasOpenInstances<ProfilerWindow>())
            {
                var window = EditorWindow.GetWindow<ProfilerWindow>();
                window.Close();
            }
        }
    }

    internal class CachingTests : TestWithClosedProfilerWindow
    {
        static string s_DataFilePath => Path.Combine(Application.temporaryCachePath, "profilerdata");
        static readonly string k_TestMarkerName = "TestMarker";
        static ProfilerMarker s_TestMarker = new ProfilerMarker(k_TestMarkerName);

        RawFrameDataView m_RawFrameDataView;
        [SerializeField]
        bool m_OldProfilerEnabled;

        [OneTimeSetUp]
        public void GenerateProfilerData()
        {
            m_OldProfilerEnabled = Profiler.enabled;

            Profiler.enabled = false;
            Profiler.logFile = s_DataFilePath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;

            using (s_TestMarker.Auto())
            {
                // Schedule two jobs where one depends on the other
                var testJob = new TestJob();
                var testJobHandle = testJob.Schedule();
                var testJob2 = new TestJob2();
                var testJob2Handle = testJob2.Schedule(testJobHandle);
                testJob2Handle.Complete();
            }

            Profiler.enabled = false;
            Profiler.logFile = "";

            var loaded = ProfilerDriver.LoadProfile(s_DataFilePath + ".raw", false);
            Assert.IsTrue(loaded);
            Assert.AreNotEqual(-1, ProfilerDriver.lastFrameIndex);
        }

        // Use the existing profiler data for all tests to avoid redundant captures
        [OneTimeSetUp]
        public void SetUpProfilerDataOnce()
        {
            GenerateProfilerData();
        }

        [Test]
        public void FrameCache_Populate_Data()
        {
            FrameData frameData;
            FrameCache frameCache = new FrameCache();
            frameCache.CacheRange(ProfilerDriver.lastFrameIndex, ProfilerDriver.lastFrameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.IsTrue(frameCache.GetFrame(ProfilerDriver.lastFrameIndex, out frameData));
        }

        [Test]
        public void FrameCache_CanCacheAndRetrieveFrame()
        {
            var frameCache = new FrameCache();
            int frameIndex = ProfilerDriver.lastFrameIndex;

            frameCache.CacheRange(frameIndex, frameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.IsTrue(frameCache.GetFrame(frameIndex, out var frameData), "Should retrieve cached frame data.");
            Assert.Greater(frameCache.GetNumberOfFrames(), 0, "FrameCache should report at least one cached frame.");
            Assert.IsTrue(frameCache.Frames.ContainsKey(frameIndex), "Frames property should contain the cached frame.");
        }

        [Test]
        public void FrameCache_StringAndEventLookup_AreConsistent()
        {
            var frameCache = new FrameCache();
            int frameIndex = ProfilerDriver.lastFrameIndex;

            frameCache.CacheRange(frameIndex, frameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.IsTrue(frameCache.GetFrame(frameIndex, out var frameData), "Should retrieve cached frame data.");

            int foundMarkerId = -1;
            foreach (var kvp in frameData.stringIndex)
            {
                foundMarkerId = kvp.Key;
                break;
            }
            Assume.That(foundMarkerId != -1, "No markerId found in frame data.");

            string markerString = frameCache.GetStringForFrame(frameIndex, foundMarkerId);
            Assert.IsNotNull(markerString, "GetStringForFrame should return a string for a valid markerId.");
            Assert.AreNotEqual("Unknown", markerString, "GetStringForFrame should not return 'Unknown' for a valid markerId.");

            string foundString = frameCache.FindString(foundMarkerId);
            Assert.AreEqual(markerString, foundString, "FindString should match GetStringForFrame for the same markerId.");

            if (frameData.events.Length > 0)
            {
                string eventString = frameCache.GetEventStringForFrame(frameIndex, 0);
                Assert.IsNotNull(eventString, "GetEventStringForFrame should return a string for a valid event.");
                Assert.AreNotEqual("Unknown", eventString, "GetEventStringForFrame should not return 'Unknown' for a valid event.");
            }
        }

        [Test]
        public void FrameCache_JobHandleAndEventRelationship_AreValid()
        {
            var frameCache = new FrameCache();
            int frameIndex = ProfilerDriver.lastFrameIndex;

            frameCache.CacheRange(frameIndex, frameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.IsTrue(frameCache.GetFrame(frameIndex, out var frameData), "Should retrieve cached frame data.");

            if (frameData.jobEventIndexList.Length > 0)
            {
                var jobEvent = frameData.jobEventIndexList[0];
                var handle = frameCache.GetJobHandleForFrameEvent(frameIndex, jobEvent.eventIndex);
                Assert.IsTrue(handle.IsValid(), "GetJobHandleForFrameEvent should return a valid handle for a valid event.");

                float time = frameCache.GetTimeForEvent(frameIndex, jobEvent.eventIndex);
                Assert.GreaterOrEqual(time, 0.0f, "GetTimeForEvent should return a non-negative time for a valid event.");
            }
        }

        [Test]
        public void FrameCache_EdgeCases_UncachedAndInvalid()
        {
            var frameCache = new FrameCache();
            int frameIndex = ProfilerDriver.lastFrameIndex;
            int uncachedFrame = frameIndex + 1000;

            Assert.IsFalse(frameCache.GetFrame(uncachedFrame, out var _), "GetFrame should return false for uncached frame.");
            Assert.AreEqual("Unknown", frameCache.GetStringForFrame(uncachedFrame, 12345), "GetStringForFrame should return 'Unknown' for uncached frame.");
            Assert.AreEqual("Unknown", frameCache.GetEventStringForFrame(uncachedFrame, 0), "GetEventStringForFrame should return 'Unknown' for uncached frame.");
            Assert.AreEqual(0.0f, frameCache.GetTimeForEvent(uncachedFrame, 0), "GetTimeForEvent should return 0.0f for uncached frame.");

            frameCache.CacheRange(frameIndex, frameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.AreEqual("Unknown", frameCache.GetStringForFrame(frameIndex, -999), "GetStringForFrame should return 'Unknown' for invalid markerId.");
            Assert.AreEqual("Unknown", frameCache.FindString(-999), "FindString should return 'Unknown' for invalid markerId.");
        }

        [Test]
        public void FrameCache_CacheRange_HandlesEmptyAndInvalidRanges()
        {
            var frameCache = new FrameCache();

            frameCache.CacheRange(5, 5, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);
            Assert.AreEqual(0, frameCache.GetNumberOfFrames(), "No frames should be cached for an empty range.");

            // Invalid range (end < start)
            Assert.DoesNotThrow(() => frameCache.CacheRange(10, 5, false), "CacheRange should not throw on invalid range.");
        }

        [Test]
        public void FrameCache_FrameData_PublicData_IsValid()
        {
            var frameCache = new FrameCache();
            int frameIndex = ProfilerDriver.lastFrameIndex;

            frameCache.CacheRange(frameIndex, frameIndex + 1, false);
            frameCache.Update();
            frameCache.UpdateInflightJobs(FrameCache.WaitOnJobs.Yes);

            Assert.IsTrue(frameCache.GetFrame(frameIndex, out var frameData), "Should retrieve cached frame data.");

            // Validate FrameInfo
            Assert.IsTrue(frameData.info.IsCreated && frameData.info.Length > 0, "FrameInfo should be present.");
            var info = frameData.info[0];
            Assert.AreEqual(frameIndex, info.frameIndex, "FrameInfo.frameIndex should match requested frame.");
            Assert.GreaterOrEqual(info.frameTime, 0.0f, "FrameInfo.frameTime should be non-negative.");

            // Validate events
            Assert.IsTrue(frameData.events.IsCreated, "Events should be created.");
            Assert.Greater(frameData.events.Length, 0, "There should be at least one ProfilingEvent.");
            foreach (var evt in frameData.events)
            {
                Assert.GreaterOrEqual(evt.startTime, 0.0f, "ProfilingEvent.startTime should be non-negative.");
                Assert.GreaterOrEqual(evt.time, 0.0f, "ProfilingEvent.time should be non-negative.");
                Assert.GreaterOrEqual(evt.markerId, 0, "ProfilingEvent.markerId should be non-negative.");
                Assert.GreaterOrEqual(evt.threadIndex, 0, "ProfilingEvent.threadIndex should be non-negative.");
            }

            // Validate threadGroups
            Assert.IsTrue(frameData.threadGroups.IsCreated, "ThreadGroups should be created.");
            Assert.Greater(frameData.threadGroups.Length, 0, "There should be at least one ThreadGroup.");
            foreach (var tg in frameData.threadGroups)
            {
                Assert.GreaterOrEqual(tg.arrayIndex, 0, "ThreadGroup.arrayIndex should be non-negative.");
                Assert.GreaterOrEqual(tg.arrayEnd, tg.arrayIndex, "ThreadGroup.arrayEnd should be >= arrayIndex.");
            }

            // Validate threads
            Assert.IsTrue(frameData.threads.IsCreated, "Threads should be created.");
            Assert.Greater(frameData.threads.Length, 0, "There should be at least one ThreadInfo.");
            foreach (var ti in frameData.threads)
            {
                Assert.IsFalse(string.IsNullOrEmpty(ti.name.ToString()), "ThreadInfo.name should not be empty.");
                Assert.GreaterOrEqual(ti.maxDepth, 0, "ThreadInfo.maxDepth should be non-negative.");
                Assert.GreaterOrEqual(ti.eventStart, 0, "ThreadInfo.eventStart should be non-negative.");
                Assert.GreaterOrEqual(ti.eventEnd, ti.eventStart, "ThreadInfo.eventEnd should be >= eventStart.");
            }

            // Validate catColors
            Assert.IsTrue(frameData.catColors.IsCreated, "catColors should be created.");
            Assert.Greater(frameData.catColors.Length, 0, "catColors should have at least one entry.");

            // Validate stringIndex, strings128, strings512
            Assert.IsTrue(frameData.stringIndex.IsCreated, "stringIndex should be created.");
            Assert.Greater(frameData.stringIndex.Count, 0, "stringIndex should have at least one entry.");
            Assert.IsTrue(frameData.strings128.IsCreated, "strings128 should be created.");
            Assert.Greater(frameData.strings128.Length, 0, "strings128 should have at least one entry.");

            // strings512 may be empty, but should be created
            Assert.IsTrue(frameData.strings512.IsCreated, "strings512 should be created.");

            // Validate jobEventIndexList, handleIndexLookup, eventHandleLookup, scheduledJobs, dependencyTable, jobFlows, threadsLookup
            Assert.IsTrue(frameData.jobEventIndexList.IsCreated, "jobEventIndexList should be created.");
            Assert.IsTrue(frameData.handleIndexLookup.IsCreated, "handleIndexLookup should be created.");
            Assert.IsTrue(frameData.eventHandleLookup.IsCreated, "eventHandleLookup should be created.");
            Assert.IsTrue(frameData.scheduledJobs.IsCreated, "scheduledJobs should be created.");
            Assert.IsTrue(frameData.dependencyTable.IsCreated, "dependencyTable should be created.");
            Assert.IsTrue(frameData.jobFlows.IsCreated, "jobFlows should be created.");
            Assert.IsTrue(frameData.threadsLookup.IsCreated, "threadsLookup should be created.");

            // Spot check: at least one scheduled job, job flow, or job event index should exist for a valid jobs frame
            Assert.GreaterOrEqual(frameData.scheduledJobs.Length + frameData.jobFlows.Length + frameData.jobEventIndexList.Length, 1,
                "There should be at least one scheduled job, job flow, or job event index.");

            // Spot check: dependencyTable may be empty, but should be created
            Assert.GreaterOrEqual(frameData.dependencyTable.Length, 0, "dependencyTable should be created (may be empty).");
        }
    }
}

