using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.JobsProfiler.Tests
{
    internal class BasicTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }

    internal class ThreadVisibilityTests
    {
        [Test]
        public void TestThreadFullyVisible()
        {
            // Case where the thread is fully within the screen bounds
            Assert.AreEqual(MathUtils.ThreadVisibility.Fully, MathUtils.CalcThreadVisibility(0f, 100f, 10f, 90f));
            Assert.AreEqual(MathUtils.ThreadVisibility.Fully, MathUtils.CalcThreadVisibility(50f, 150f, 75f, 125f));
        }

        [Test]
        public void TestThreadPartiallyVisible()
        {
            // Case where the thread starts before the screen but ends within it
            Assert.AreEqual(MathUtils.ThreadVisibility.Partial, MathUtils.CalcThreadVisibility(0f, 100f, -10f, 50f));

            // Case where the thread starts within the screen but ends after it
            Assert.AreEqual(MathUtils.ThreadVisibility.Partial, MathUtils.CalcThreadVisibility(0f, 100f, 50f, 150f));

            // Case where the thread overlaps the screen on both sides
            Assert.AreEqual(MathUtils.ThreadVisibility.Partial, MathUtils.CalcThreadVisibility(0f, 100f, -10f, 110f));
        }

        [Test]
        public void TestThreadHidden()
        {
            // Case where the thread is completely above the screen
            Assert.AreEqual(MathUtils.ThreadVisibility.Hidden, MathUtils.CalcThreadVisibility(50f, 150f, 0f, 40f));

            // Case where the thread is completely below the screen
            Assert.AreEqual(MathUtils.ThreadVisibility.Hidden, MathUtils.CalcThreadVisibility(50f, 150f, 160f, 200f));

            // Case where the thread ends exactly at the start of the screen (edge case)
            Assert.AreEqual(MathUtils.ThreadVisibility.Hidden, MathUtils.CalcThreadVisibility(50f, 150f, 0f, 49f));

            // Case where the thread starts exactly at the end of the screen (edge case)
            Assert.AreEqual(MathUtils.ThreadVisibility.Hidden, MathUtils.CalcThreadVisibility(50f, 150f, 151f, 200f));
        }
    }
    internal class PrimitiveRendererTest
    {
        [Test]
        public void AddSingleQuad()
        {
            var renderer = new PrimitiveRenderer(1024);

            float2 v0 = new float2(0.0f, 1.0f);
            float2 v1 = new float2(2.0f, 3.0f);

            renderer.DrawQuadColor(v0, v1, new Color32(0, 0, 0, 0));

            Assert.AreEqual(6, renderer.indices.Length);
            Assert.AreEqual(4, renderer.vertices.Length);

            renderer.Dispose();
        }

        [Test]
        public void AddSingleTriangle()
        {
            var renderer = new PrimitiveRenderer(1024);
            var vertex = new Vertex();

            renderer.AddTriangle(vertex, vertex, vertex);

            Assert.AreEqual(3, renderer.indices.Length);
            Assert.AreEqual(3, renderer.vertices.Length);

            renderer.Dispose();
        }


        [Test]
        public void AddMaxTrianglesNoOverflow()
        {
            int count = PrimitiveRenderer.kMaxVertsPerChunk / 3;

            var renderer = new PrimitiveRenderer(1024);
            var vertex = new Vertex();

            for (int i = 0; i < count; i++)
                renderer.AddTriangle(vertex, vertex, vertex);

            Assert.AreEqual(PrimitiveRenderer.kMaxVertsPerChunk, renderer.indices.Length);
            Assert.AreEqual(PrimitiveRenderer.kMaxVertsPerChunk, renderer.vertexIndex);
            Assert.AreEqual(0, renderer.drawRanges.Length);

            renderer.Dispose();
        }

        [Test]
        public void AddOneTriangleOverflow()
        {
            int count = (PrimitiveRenderer.kMaxVertsPerChunk / 3) + 1;

            var renderer = new PrimitiveRenderer(1024);
            var vertex = new Vertex();

            for (int i = 0; i < count; i++)
                renderer.AddTriangle(vertex, vertex, vertex);

            Assert.AreEqual(PrimitiveRenderer.kMaxVertsPerChunk + 3, renderer.indices.Length);
            Assert.AreEqual(3, renderer.vertexIndex, 3);
            Assert.AreEqual(1, renderer.drawRanges.Length, 1);

            var drawRange = renderer.drawRanges[0];
            Assert.AreEqual(0, drawRange.indexStart);
            Assert.AreEqual(0, drawRange.vertexStart);
            Assert.AreEqual(PrimitiveRenderer.kMaxVertsPerChunk, drawRange.indexCount);
            Assert.AreEqual(PrimitiveRenderer.kMaxVertsPerChunk, drawRange.vertexCount);

            renderer.Dispose();
        }

        [Test]
        public void AddQuadsNoOverflow()
        {
            int count = 16384 - 1;

            var renderer = new PrimitiveRenderer(1024);

            float2 p = new float2(0.0f, 0.0f);

            for (int i = 0; i < count; i++)
                renderer.DrawQuadColor(p, p, new Color32(0, 0, 0, 0));

            Assert.AreEqual(count * 6, renderer.indices.Length);
            Assert.AreEqual(count * 4, renderer.vertices.Length);
            Assert.AreEqual(65532, renderer.vertexIndex);

            renderer.Dispose();
        }

        [Test]
        public void AddQuadsOneOverflow()
        {
            int count = 16384;

            var renderer = new PrimitiveRenderer(1024);

            float2 p = new float2(0.0f, 0.0f);

            for (int i = 0; i < count; i++)
                renderer.DrawQuadColor(p, p, new Color32(0, 0, 0, 0));

            Assert.AreEqual(count * 6, renderer.indices.Length);
            Assert.AreEqual(count * 4, renderer.vertices.Length);
            Assert.AreEqual(4, renderer.vertexIndex);
            Assert.AreEqual(1, renderer.drawRanges.Length);

            var drawRange = renderer.drawRanges[0];
            Assert.AreEqual(0, drawRange.indexStart);
            Assert.AreEqual(0, drawRange.vertexStart);
            Assert.AreEqual(16383 * 6, drawRange.indexCount);
            Assert.AreEqual(16383 * 4, drawRange.vertexCount);

            renderer.Dispose();
        }

        [Test]
        public void AddQuadsDoubleOverflow()
        {
            int count = 16384 * 2;

            var renderer = new PrimitiveRenderer(1024);

            float2 p = new float2(0.0f, 0.0f);

            for (int i = 0; i < count; i++)
                renderer.DrawQuadColor(p, p, new Color32(0, 0, 0, 0));

            Assert.AreEqual(count * 6, renderer.indices.Length);
            Assert.AreEqual(count * 4, renderer.vertices.Length);
            Assert.AreEqual(8, renderer.vertexIndex);
            Assert.AreEqual(2, renderer.drawRanges.Length);

            int offset = 16383;

            var drawRange0 = renderer.drawRanges[0];
            Assert.AreEqual(0, drawRange0.indexStart);
            Assert.AreEqual(offset * 6, drawRange0.indexCount);
            Assert.AreEqual(0, drawRange0.vertexStart);
            Assert.AreEqual(offset * 4, drawRange0.vertexCount);

            var drawRange1 = renderer.drawRanges[1];
            Assert.AreEqual((offset * 6), drawRange1.indexStart);
            Assert.AreEqual((offset * 6), drawRange1.indexCount);
            Assert.AreEqual((offset * 4), drawRange1.vertexStart);
            Assert.AreEqual((offset * 4), drawRange1.vertexCount);

            renderer.Dispose();
        }

        [Test]
        public void ValidateMaxVertexCounts()
        {
            int count = 16384 * 2;

            var renderer = new PrimitiveRenderer(1024);

            for (int i = 0; i < count; i++)
            {
                float2 p = new float2(i, i);
                renderer.DrawQuadColor(p, p, new Color32(0, 0, 0, 0));
            }

            var verts = renderer.vertices.AsArray();
            var indices = renderer.indices.AsArray();

            int vertexStart = 0;
            int indexStart = 0;

            int validateVertexIndex = 0;

            foreach (var range in renderer.drawRanges)
            {
                vertexStart = range.vertexStart;
                indexStart = range.indexStart;

                int vertexCount = range.vertexCount;
                int indexCount = range.indexCount;

                var vertArray = new NativeSlice<Vertex>(verts, vertexStart, vertexCount);
                var indArray = new NativeSlice<ushort>(indices, indexStart, indexCount);

                Assert.IsTrue(vertArray.Length <= 65535);
                Assert.IsTrue(indArray.Length <= 65535 * 6);

                // Each chunk should always start with 0 index value
                Assert.AreEqual(0, indArray[0]);

                for (int i = 0; i < vertArray.Length; i += 4)
                {
                    int t0 = (int)vertArray[i + 0].position.x;
                    int t1 = (int)vertArray[i + 3].position.x;

                    Assert.AreEqual(t0, validateVertexIndex);
                    Assert.AreEqual(t1, validateVertexIndex);

                    validateVertexIndex++;
                }

                // when we fall out we want start to to be at the end of the range above meaning start + count
                vertexStart += vertexCount;
                indexStart += indexCount;
            }

            var vertexArray = new NativeSlice<Vertex>(verts, vertexStart, verts.Length - vertexStart);
            var indexArray = new NativeSlice<ushort>(indices, indexStart, indices.Length - indexStart);

            if (indexArray.Length > 0)
                Assert.AreEqual(0, indexArray[0]);

            for (int i = 0; i < vertexArray.Length; i += 4)
            {
                int t0 = (int)vertexArray[i + 0].position.x;
                int t1 = (int)vertexArray[i + 3].position.x;

                Assert.AreEqual(t0, validateVertexIndex);
                Assert.AreEqual(t1, validateVertexIndex);

                validateVertexIndex++;
            }

            Assert.IsTrue(vertexArray.Length <= 65535);
            Assert.IsTrue(vertexArray.Length <= 65535 * 6);

            renderer.Dispose();
        }

        [Test]
        public void RandomizeValidate()
        {
            var rand = new Unity.Mathematics.Random(0xfadebabe);
            float2 t = new float2(0.0f, 0.0f);
            Vertex vertex = new Vertex();

            for (int rt = 0; rt < 32; ++rt)
            {
                int primitiveCount = rand.NextInt(18000, 28000);

                var renderer = new PrimitiveRenderer(primitiveCount * 6);

                for (int i = 0; i < primitiveCount; ++i)
                {
                    if (rand.NextBool())
                    {
                        renderer.AddTriangle(vertex, vertex, vertex);
                        renderer.AddTriangle(vertex, vertex, vertex);
                    }
                    else
                    {
                        renderer.DrawQuadColor(t, t, new Color32(0, 0, 0, 0));
                    }
                }

                var verts = renderer.vertices.AsArray();
                var indices = renderer.indices.AsArray();

                int vertexStart = 0;
                int indexStart = 0;

                foreach (var range in renderer.drawRanges)
                {
                    vertexStart = range.vertexStart;
                    indexStart = range.indexStart;

                    int vertexCount = range.vertexCount;
                    int indexCount = range.indexCount;

                    var vertArray = new NativeSlice<Vertex>(verts, vertexStart, vertexCount);
                    var indArray = new NativeSlice<ushort>(indices, indexStart, indexCount);

                    Assert.IsTrue(vertArray.Length <= 65535);
                    Assert.IsTrue(indArray.Length <= 65535 * 6);

                    // Each chunk should always start with 0 index value
                    Assert.AreEqual(0, indArray[0]);

                    // when we fall out we want start to to be at the end of the range above meaning start + count
                    vertexStart += vertexCount;
                    indexStart += indexCount;
                }

                var vertexArray = new NativeSlice<Vertex>(verts, vertexStart, verts.Length - vertexStart);
                var indexArray = new NativeSlice<ushort>(indices, indexStart, indices.Length - indexStart);

                if (indexArray.Length > 0)
                    Assert.AreEqual(0, indexArray[0]);

                Assert.IsTrue(vertexArray.Length <= 65535);
                Assert.IsTrue(vertexArray.Length <= 65535 * 6);

                renderer.Dispose();
            }
        }
    }

    internal class CacheProfilerDataTests
    {
        [Test]
        public void ReorderEventIndicesBasic()
        {
            var events = new NativeList<ProfilingEvent>(5, Allocator.Temp);
            var threads = new NativeArray<ThreadInfo>(1, Allocator.Temp);
            var scheduledJobs = new NativeList<ScheduledJobInfo>(1, Allocator.Temp);
            var jobFlows = new NativeList<JobFlow>(1, Allocator.Temp);
            var jobEventIndexList = new NativeList<JobHandleEventIndex>(1, Allocator.Temp);

            threads[0] = new ThreadInfo
            {
                name = new FixedString128Bytes("Test"),
                threadId = 0,
                maxDepth = 3,
                eventStart = 0,
                eventEnd = 6,
            };

            // Construct events that looks like this
            //  0 4 5
            //   1 3
            //    2
            events.Add(new ProfilingEvent(0.0f, 0.0f, 0, 0, 0, 0, 0));
            events.Add(new ProfilingEvent(1.0f, 0.0f, 0, 1, 0, 0, 0));
            events.Add(new ProfilingEvent(2.0f, 0.0f, 0, 2, 0, 0, 0));
            events.Add(new ProfilingEvent(3.0f, 0.0f, 0, 1, 0, 0, 0));
            events.Add(new ProfilingEvent(4.0f, 0.0f, 0, 0, 0, 0, 0));
            events.Add(new ProfilingEvent(5.0f, 0.0f, 0, 0, 0, 0, 0));

            CacheFixupJob.OrderLevels(events, threads, scheduledJobs, jobFlows, jobEventIndexList);

            Assert.AreEqual(0, (int)events[0].level);
            Assert.AreEqual(0, (int)events[0].startTime);

            Assert.AreEqual(0, (int)events[1].level);
            Assert.AreEqual(4, (int)events[1].startTime);

            Assert.AreEqual(0, (int)events[2].level);
            Assert.AreEqual(5, (int)events[2].startTime);

            Assert.AreEqual(1, (int)events[3].startTime);
            Assert.AreEqual(1, (int)events[3].level);

            Assert.AreEqual(3, (int)events[4].startTime);
            Assert.AreEqual(1, (int)events[4].level);

            Assert.AreEqual(2, (int)events[5].startTime);
            Assert.AreEqual(2, (int)events[5].level);
        }
    }
}
