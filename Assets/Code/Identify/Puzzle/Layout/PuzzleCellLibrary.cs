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
        [Serializable]
        private struct CellData
        {
            public DataTypeMask Mask;
            public Mesh Mesh;
            public Mesh OutlineMesh;
            public Vector2 Dims;
        }

        public struct AssembledCellData
        {
            public Mesh Mesh;
            public Mesh OutlineMesh;
            public Vector2 Dims;
        }

        [Space]
        [SerializeField] private CellData[] m_Cells = new CellData[16];

        private Dictionary<DataTypeMask, AssembledCellData> m_CellDict;
        private Vector2 m_DefaultDims = new Vector2(1, 1);

        static public readonly ActionEvent OnUpdated = new ActionEvent(64);

        public bool Lookup(DataTypeMask mask, out AssembledCellData assembledCellData)
        {
            if (m_CellDict == null) {
                Construct(m_Cells);
            }

            if (!m_CellDict.ContainsKey(mask)) {
                assembledCellData = new AssembledCellData();
                return false; 
            }

            assembledCellData = m_CellDict[mask];

            return true;
        }

        private void Construct(CellData[] cells)
        {
            if (m_CellDict != null) { m_CellDict.Clear(); }
            else { m_CellDict = new Dictionary<DataTypeMask, AssembledCellData>(); }

            foreach (var cell in cells)
            {
                if (m_CellDict.ContainsKey(cell.Mask))
                {
                    Debug.LogError("[PuzzleCellLibrary] Multiple cells registed for mask " + cell.Mask);
                }
                else
                {
                    AssembledCellData assembledData = new AssembledCellData();
                    assembledData.Mesh = cell.Mesh;
                    assembledData.OutlineMesh = cell.OutlineMesh;
                    assembledData.Dims = cell.Dims * m_DefaultDims;
                    m_CellDict.Add(cell.Mask, assembledData);
                }
            }
            Debug.Log("[PuzzleCellLibrary] constructed library with " + m_CellDict.Count + " entries");
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

            Construct(m_Cells);
        }

#endif // UNITY_EDITOR
    }
}