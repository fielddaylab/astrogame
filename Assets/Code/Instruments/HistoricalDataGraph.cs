using BeauUtil;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class HistoricalDataGraph : BatchedComponent, IRegistrationCallbacks {
        [NonSerialized] public HistoricalPatternType CurrentType;
        [NonSerialized] public float Scale;
        public MeshRenderer DisplayTarget;
        public DataDisplay Display;

        public void OnDeregister() {
        }

        public void OnRegister() {
            Display.OnDisplayRequested.Register(
                (packet, flags) => HistoricalDataUtility.OnDisplayRequest(this, packet, flags));
            Display.OnDisplayCleared.Register(
                () => HistoricalDataUtility.OnDisplayClear(this));
        }
    }

    public static class HistoricalDataUtility {

        public static void OnDisplayRequest(HistoricalDataGraph graph, DataPacket packet, DataFormattingFlags flags) {
            SetPattern(graph, packet.HistoricalPatternId);
        }
        
        public static void OnDisplayClear(HistoricalDataGraph graph) {
            ClearPattern(graph);
        }

        public static void ClearPattern(HistoricalDataGraph graph) {
            graph.CurrentType = HistoricalPatternType.None;
            graph.Scale = 1;
            UpdatePatternMaterial(graph);
        }

        public static void SetPattern(HistoricalDataGraph graph, StringHash32 assetId) {
            if (assetId.Equals(StringHash32.Null)) {
                graph.CurrentType = HistoricalPatternType.Constant;
                graph.Scale = 1;
            } else {
                HistoricalPatternAsset asset = Find.NamedAsset<HistoricalPatternAsset>(assetId);
                graph.CurrentType = asset.Type;
                graph.Scale = asset.WaveAmplitude;
            }
            UpdatePatternMaterial(graph);
        }

        public static void UpdatePatternMaterial(HistoricalDataGraph graph) {
            graph.DisplayTarget.material = GetPatternMaterial(graph.CurrentType, Find.State<HistoricalDataState>());
        }

        public static Material GetPatternMaterial (HistoricalPatternType type, HistoricalDataState state) {
            return state.PatternMaterials.Find(pm => (pm.Pattern == type)).Material;
        }
    }
}