using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
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
            UpdatePatternMaterial(graph, Find.State<HistoricalDataState>());
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
            UpdatePatternMaterial(graph, Find.State<HistoricalDataState>());
        }

        public static void UpdatePatternMaterial(HistoricalDataGraph graph, HistoricalDataState state) {
            graph.DisplayTarget.material = GetPatternMaterial(graph.CurrentType, state);
        }

        public static Material GetPatternMaterial (HistoricalPatternType type, HistoricalDataState state) {
            if (state.ShowingParallax) {
                //parallax should always show sine
                return state.PatternMaterials.Find(pm => (pm.Pattern == HistoricalPatternType.SineWave)).Material;
            }
            return state.PatternMaterials.Find(pm => (pm.Pattern == type)).Material;
        }

        public static void ToggleInstrumentMode() {
            HistoricalDataState hds = Find.State<HistoricalDataState>();
            SetParallaxShowing(!hds.ShowingParallax, hds);
        }

        public static void SetParallaxShowing(bool parallaxShowing, HistoricalDataState hds) {
            hds.ShowingParallax = parallaxShowing;
            hds.KnobRoutine.Replace(SlideRoutine(hds));
            DataUtility.SetDisplayHidden(hds.DistanceDisplay, !parallaxShowing);
            UpdatePatternMaterial(hds.InstrumentGraph, hds);
        }

        private static IEnumerator SlideRoutine(HistoricalDataState hds) {
            if (hds.ShowingParallax) {
                yield return hds.ModeKnob.MoveTo(0.4f, 0.4f, Axis.Y, Space.Self).Ease(Curve.CubeIn);
            } else {
                yield return hds.ModeKnob.MoveTo(0f, 0.4f, Axis.Y, Space.Self).Ease(Curve.CubeIn);
            }
            yield return null;
        }
    }
}