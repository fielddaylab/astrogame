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
    public sealed class PuzzleGridAtlasGenerator : ScriptableWizard {
        #region Inspector

        [Header("Data")]
        public RenderAtlas Atlas;
        public ushort RegionWidth = 222;
        public ushort RegionWidthSmall = 222;
        public ushort RegionHeight = 100;

        [Header("Counts")]
        public int NumXSmall;
        public int NumSmall;
        public int NumMedium;
        public int NumLarge;

        #endregion // Inspector

        public void OnWizardCreate() {
            List<RenderAtlas.RegionData> regions = new List<RenderAtlas.RegionData>();

            Generate(0, NumXSmall, RegionWidthSmall, RegionHeight, regions);
            Generate(1, NumSmall, RegionWidthSmall, RegionHeight, regions);
            Generate(2, NumMedium, RegionWidth, RegionHeight, regions);
            Generate(3, NumLarge, RegionWidth, RegionHeight, regions);

            Atlas.OverwriteRegions(regions.ToArray());
        }

        static private void Generate(int sizeNum, int count, ushort width, ushort height, List<RenderAtlas.RegionData> output) {
            for (int i = 0; i < count; i++) {
                output.Add(new RenderAtlas.RegionData() {
                    Width = width,
                    Height = height,
                    Id = new SerializedHash32(sizeNum.ToStringLookup() + "C" + (i + 1).ToStringLookup())
                });
            }
        }

        [MenuItem("Astro/Puzzle Grid RenderAtlas Generator")]
        static private void CreateWizard() {
            DisplayWizard<PuzzleGridAtlasGenerator>("Puzzle Grid RenderAtlas Generator", "Apply");
        }
    }
}