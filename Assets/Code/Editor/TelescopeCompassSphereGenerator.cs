using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astro {
    public sealed class TelescopeCompassSphereGenerator : ScriptableWizard {
        #region Inspector

        [Header("Configuration")]
        public float Radius = 1;
        public int VerticalLineCount = 2;
        public int RingResolution = 32;
        [Range(-1, 1)] public float EquatorDip = -0.2f;
        public float VerticalLineAngleOffset = 15;

        #endregion // Inspector


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Vertex {
            [VertexAttr(VertexAttribute.Position)] public Vector3 Position;
        }

        public void OnWizardCreate() {
            Vector3 top = new Vector3(0, Radius, 0);
            Vector3 horizontal = new Vector3(-Radius, 0, 0);

            int ringRes = Unsafe.AlignUp4(RingResolution);

            MeshData16<Vertex> meshData = new MeshData16<Vertex>(512, MeshTopology.LineStrip, true);

            meshData.Clear();

            // vertical rings
            Vector3 rot = default,
                newPos = default;

            meshData.AddIndex((ushort) meshData.VertexCount);
            meshData.AddVertex(new Vertex() { Position = -top });

            ushort topIdx = (ushort) meshData.VertexCount;
            meshData.AddIndex(topIdx);
            meshData.AddVertex(new Vertex() { Position = top });

            // initial ring (1/4)
            rot.y = 0;
            for (int j = 1; j <= ringRes / 4; j++) {
                rot.z = (j * 360f) / ringRes;
                newPos = Quaternion.Euler(rot) * top;
                meshData.AddIndex((ushort) meshData.VertexCount);
                meshData.AddVertex(new Vertex() { Position = newPos });
            }

            // equator
            rot.z = 0;
            for (int j = 1; j < ringRes; j++) {
                rot.y = -(j * 360f) / ringRes;
                newPos = Quaternion.Euler(rot) * horizontal;
                newPos.y += Mathf.Sin(Mathf.PI * 2 * j / ringRes) * EquatorDip * Radius;
                meshData.AddIndex((ushort) meshData.VertexCount);
                meshData.AddVertex(new Vertex() { Position = newPos });
            }

            rot.y = 0;
            // initial ring (3/4)
            for (int j = ringRes / 4; j < ringRes; j++) {
                rot.z = (j * 360f) / ringRes;
                newPos = Quaternion.Euler(rot) * top;
                meshData.AddIndex((ushort) meshData.VertexCount);
                meshData.AddVertex(new Vertex() { Position = newPos });
            }

            meshData.AddIndex(topIdx);
            // SphereMeshData.AddVertex(new VertexP3C1() { Position = top });

            int angleDelta = 180 / (VerticalLineCount + 1);
            for (int i = 1; i <= VerticalLineCount; i++) {
                rot.y = (i * angleDelta) + VerticalLineAngleOffset;
                for (int j = 1; j < ringRes; j++) {
                    rot.z = (j * 360f) / ringRes;
                    newPos = Quaternion.Euler(rot) * top;
                    newPos.z = Math.Abs(newPos.z);
                    meshData.AddIndex((ushort) meshData.VertexCount);
                    meshData.AddVertex(new Vertex() { Position = newPos });
                }

                meshData.AddIndex(topIdx);
                //SphereMeshData.AddVertex(new VertexP3C1() { Position = top });
            }

            Mesh m = new Mesh();
            meshData.Upload(m, MeshDataUploadFlags.MarkNoLongerReadable);

            SaveResourceAs(m, "CompassSphereMesh");
            DestroyResource(ref m);
        }

        static private bool SaveResourceAs(Mesh mesh, string name) {
            string path = EditorUtility.SaveFilePanelInProject("Save Mesh", name, "mesh", "Save this mesh", "Assets/_Assets/Models/Compass");
            if (!string.IsNullOrEmpty(path)) {
                Mesh clone = Instantiate(mesh);
                clone.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(clone, path);
                //lastDirectory = Path.GetDirectoryName(path);
                return true;
            }

            return false;
        }

        static private void DestroyResource<T>(ref T obj) where T : UnityEngine.Object {
            if (obj != null) {
                DestroyImmediate(obj);
                obj = null;
            }
        }

        [MenuItem("Astro/Compass Sphere Generator")]
        static private void CreateWizard() {
            DisplayWizard<TelescopeCompassSphereGenerator>("Compass Sphere Generator", "Generate");
        }
    }
}