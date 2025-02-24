using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEditor;
using UnityEngine;

namespace Astro
{
    [CreateAssetMenu(menuName = "AstroGame/Puzzle Cell Library")]
    public sealed class PuzzleCellLibrary : ScriptableObject
    {
        public enum BundleType : byte
        {
            XSmall = 0,
            Small = 1,
            Medium = 2,
            Large = 3
        }

        [Serializable]
        public struct CellMeshBundle
        {
            public BundleType Type;
            public Mesh Mesh;
            public Mesh OutlineMesh;
            public Vector2 Dims; // physical mesh dims
            public Vector2 RenderDims; // scaling on renderer
            public float CamSize;
        }

        [Serializable]
        private struct CellData
        {
            public DataTypeMask Mask;
            public BundleType BundleID;
        }

        public struct AssembledCellData
        {
            public CellMeshBundle Bundle;
        }

        [Space]
        [SerializeField] private CellData[] m_Cells = new CellData[16];
        [SerializeField] private CellMeshBundle[] m_Bundles = new CellMeshBundle[4];

        private Dictionary<DataTypeMask, AssembledCellData> m_CellDict;
        private Dictionary<BundleType, CellMeshBundle> m_BundleDict;
        private Vector2 m_DefaultDims = new Vector2(1, 1);

        static public readonly ActionEvent OnUpdated = new ActionEvent(64);

        public bool Lookup(DataTypeMask mask, out AssembledCellData assembledCellData)
        {
            if (m_CellDict == null) {
                Construct(m_Cells, m_Bundles);
            }

            if (!m_CellDict.ContainsKey(mask)) {
                assembledCellData = new AssembledCellData();
                return false; 
            }

            assembledCellData = m_CellDict[mask];

            return true;
        }

        private void Construct(CellData[] cells, CellMeshBundle[] bundles)
        {
            // construct bundle dict
            if (m_BundleDict != null) { m_BundleDict.Clear(); }
            else { m_BundleDict = new Dictionary<BundleType, CellMeshBundle>(); }

            foreach (var bundle in bundles)
            {
                if (m_BundleDict.ContainsKey(bundle.Type))
                {
                    Debug.LogError("[PuzzleCellLibrary] Multiple bundles registed for id " + bundle.Type);
                }
                else
                {
                    m_BundleDict.Add(bundle.Type, bundle);
                }
            }
            Debug.Log("[PuzzleCellLibrary] constructed bundle library with " + m_BundleDict.Count + " entries");

            // construct cell dict
            if (m_CellDict != null) { m_CellDict.Clear(); }
            else { m_CellDict = new Dictionary<DataTypeMask, AssembledCellData>(); }

            foreach (var cell in cells)
            {
                if (m_CellDict.ContainsKey(cell.Mask))
                {
                    Debug.LogError("[PuzzleCellLibrary] Multiple cells registed for mask " + cell.Mask);
                }
                else if (!m_BundleDict.ContainsKey(cell.BundleID))
                {
                    Debug.LogError("[PuzzleCellLibrary] No bundle exists for id " + cell.BundleID);
                }
                else
                {
                    AssembledCellData assembledData = new AssembledCellData();
                    assembledData.Bundle = m_BundleDict[cell.BundleID];
                    m_CellDict.Add(cell.Mask, assembledData);
                }
            }
            Debug.Log("[PuzzleCellLibrary] constructed cell library with " + m_CellDict.Count + " entries");
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(PuzzleCellLibrary))]
        private class Inspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                PuzzleCellLibrary lib = target as PuzzleCellLibrary;

                if (GUILayout.Button("Rebuild"))
                {
                    lib.Build();
                }

                if (GUILayout.Button("Refresh"))
                {
                    OnUpdated.Invoke();
                }
            }
        }

        [ContextMenu("Refresh")]
        private void Build()
        {
            Undo.RecordObject(this, "rebuilding puzzle cell data");
            EditorUtility.SetDirty(this);

            Construct(m_Cells, m_Bundles);
        }

#endif // UNITY_EDITOR
    }
}