using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using UnityEngine;

namespace Astro {
    public sealed class TelescopeCompassRenderer : MonoBehaviour, IScenePreload {
        #region Inspector

        [Header("Configuration")]
        public float Radius = 1;
        public int RingResolution = 32;
        [Range(-1, 1)] public float EquatorDip = 0.5f;

        [Header("Arcs")]
        public Color32 RAColor;
        public Color32 DeclinationColor;

        [Header("Output")]
        public DynamicMeshFilter ArcOutput;
        public DynamicMeshFilter ArcEdgeOutput;
        public RenderAtlasOutput Screen;

        #endregion // Inspector

        [NonSerialized] public MeshData16<VertexP3C1> ArcMeshData;
        [NonSerialized] public MeshData16<VertexP3C1> ArcEdgeData;

        public void GenerateArcs(float raDegrees, float decDegrees) {
            Vector3 top = new Vector3(0, Radius, 0);
            Vector3 horizontal = new Vector3(-Radius, 0, 0);

            ArcMeshData.Clear();
            ArcEdgeData.Clear();

            int ringRes = Unsafe.AlignUp4(RingResolution);

            if (raDegrees > 180) {
                raDegrees -= 360;
            } else if (raDegrees < -180) {
                raDegrees += 360;
            }

            decDegrees = Mathf.Clamp(decDegrees, -90, 90);

            Vector3 decBase = GenerateRA(raDegrees, new Vector3(-Radius, 0, 0), ringRes);
            GenerateDec(decDegrees, decBase, ringRes);

            ArcOutput.Upload(ArcMeshData, MeshDataUploadFlags.Default);
            ArcEdgeOutput.Upload(ArcEdgeData, MeshDataUploadFlags.Default);

            Screen.MarkDirty();
        }

        private unsafe Vector3 GenerateRA(float raDegrees, Vector3 baseline, int res) {
            ushort centerIdx = (ushort) ArcMeshData.VertexCount;
            VertexP3C1 vert;
            vert.Color = RAColor;

            vert.Position = default;
            ArcMeshData.AddVertex(vert);

            ArcEdgeData.AddVertex(vert);

            vert.Position = baseline;
            ushort arcStartIdx = (ushort) ArcMeshData.VertexCount;
            ArcMeshData.AddVertex(vert);

            Vector3 endVec = HorizontalRingPos(baseline, raDegrees);

            vert.Position = endVec;
            ushort arcEndIdx = (ushort) ArcMeshData.VertexCount;
            ArcMeshData.AddVertex(vert);

            ArcEdgeData.AddVertex(vert);
            ArcEdgeData.AddIndices(0, 1);

            if (Mathf.Approximately(raDegrees, 0)) {
                return endVec;
            }

            ushort* edges = stackalloc ushort[res];
            edges[0] = arcStartIdx;
            edges[res - 1] = arcEndIdx;

            for(int i = 1; i < res - 1; i++) {
                edges[i] = (ushort) ArcMeshData.VertexCount;
                vert.Position = HorizontalRingPos(baseline, raDegrees * (float) i / res);
                ArcMeshData.AddVertex(vert);
            }

            for(int i = 1; i < res; i++) {
                ArcMeshData.AddIndices(centerIdx, edges[i - 1], edges[i]);
            }

            return endVec;
        }

        private Vector3 HorizontalRingPos(Vector3 baseline, float rotDegrees) {
            Vector3 res = Quaternion.Euler(0, -rotDegrees, 0) * baseline;
            res.y += Mathf.Sin(rotDegrees * Mathf.Deg2Rad) * EquatorDip * Radius;
            return res;
        }

        private unsafe void GenerateDec(float decDegrees, Vector3 baseline, int res) {
            ushort centerIdx = (ushort) ArcMeshData.VertexCount;
            VertexP3C1 vert;
            vert.Color = DeclinationColor;

            vert.Position = default;
            ArcMeshData.AddVertex(vert);

            ArcEdgeData.AddVertex(vert);

            vert.Position = baseline;
            ushort arcStartIdx = (ushort) ArcMeshData.VertexCount;
            ArcMeshData.AddVertex(vert);

            Vector3 endVec;
            if (decDegrees >= 0) {
                endVec = Vector3.Slerp(baseline, new Vector3(0, Radius, 0), decDegrees / 90f);
            } else {
                decDegrees = -decDegrees;
                endVec = Vector3.Slerp(baseline, new Vector3(0, -Radius, 0), decDegrees / 90f);
            }

            vert.Position = endVec;
            ushort arcEndIdx = (ushort) ArcMeshData.VertexCount;
            ArcMeshData.AddVertex(vert);

            ArcEdgeData.AddVertex(vert);
            ArcEdgeData.AddIndices(2, 3);

            if (Mathf.Approximately(decDegrees, 0)) {
                return;
            }

            ushort* edges = stackalloc ushort[res];
            edges[0] = arcStartIdx;
            edges[res - 1] = arcEndIdx;

            for (int i = 1; i < res - 1; i++) {
                edges[i] = (ushort) ArcMeshData.VertexCount;
                vert.Position = Vector3.Slerp(baseline, endVec, (float) i / res);
                ArcMeshData.AddVertex(vert);
            }

            for (int i = 1; i < res; i++) {
                ArcMeshData.AddIndices(centerIdx, edges[i - 1], edges[i]);
            }
        }

        private void UpdateArcs(SpaceCameraState cam) {
            GenerateArcs(-cam.HorizLook, -cam.VertLook);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ArcMeshData = new MeshData16<VertexP3C1>(256, MeshTopology.Triangles, false);
            ArcEdgeData = new MeshData16<VertexP3C1>(4, MeshTopology.Lines, false);
            yield return null;

            GenerateArcs(0, 0);

            SpaceCameraState cm = Find.State<SpaceCameraState>();
            cm.OnLookUpdated.Register(UpdateArcs);
            UpdateArcs(cm);
        }
    }
}