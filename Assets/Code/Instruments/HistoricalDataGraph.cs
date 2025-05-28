using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
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
        public DataDisplay GraphDisplay;

        [NonSerialized] public MaterialPropertyBlock MaterialProperties;

        public void OnDeregister() {
        }

        public void OnRegister() {
            GraphDisplay.OnDisplayRequested.Register(
                (packet, flags) => HistoricalDataUtility.OnDisplayRequest(this, packet, flags));
            GraphDisplay.OnDisplayCleared.Register(
                () => HistoricalDataUtility.OnDisplayClear(this));

            MaterialProperties = new MaterialPropertyBlock();
        }
    }

    public static class HistoricalDataUtility {

        public static void OnDisplayRequest(HistoricalDataGraph graph, DataPacket packet, DataFormattingFlags flags) {
            if (packet.IsValid) {
                SetPattern(graph, packet.HistoricalPatternId);
            } else {
                ClearPattern(graph);
            }
        }
        
        public static void OnDisplayClear(HistoricalDataGraph graph) {
            ClearPattern(graph);
        }

        public static void ClearPattern(HistoricalDataGraph graph) {
            graph.CurrentType = HistoricalPatternType.None;
            graph.Scale = 1;
            graph.DisplayTarget.enabled = false;
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

            graph.DisplayTarget.enabled = true;
            UpdatePatternMaterial(graph, Find.State<HistoricalDataState>());
            UpdateScale(graph);
        }

        public static void UpdatePatternMaterial(HistoricalDataGraph graph, HistoricalDataState state) {
            graph.DisplayTarget.sharedMaterial = GetPatternMaterial(graph.CurrentType, state);
        }

        public static Material GetPatternMaterial (HistoricalPatternType type, HistoricalDataState state) {
            foreach(var pattern in state.PatternMaterials) {
                if (pattern.Pattern == type) {
                    return pattern.Material;
                }
            }
            return null;
        }

        public static void SetParallaxScale(HistoricalDataGraph graph, DataPacket packet, HistoricalDataState hds) {
            if (!packet.IsValid) {
                ClearPattern(graph);
                return;
            }
            if (packet.Value.Distance < 0.01) {
                Log.Error("[HistoricalDataGraph] Attempted to parallax scale distance of 0!");
                ClearPattern(graph);
                return;
            } else {
                graph.Scale = 48f / (float)packet.Value.Distance;
            }
            graph.CurrentType = HistoricalPatternType.Parallax;
            graph.DisplayTarget.sharedMaterial = GetPatternMaterial(graph.CurrentType, hds);
            UpdateScale(graph);
        }

        public static void UpdateScale(HistoricalDataGraph graph) {
            // TODO: update material property
             //graph.DisplayTarget.transform.SetScale(graph.Scale, Axis.Y);
        }

        public static void ToggleInstrumentMode() {
            HistoricalDataState hds = Find.State<HistoricalDataState>();
            SetInstrumentMode(!hds.SendingAbsMag, hds, false, true);
        }

        public static void SetInstrumentMode(bool absMag, HistoricalDataState hds, bool force, bool playSfx) {
            if (force || absMag != hds.SendingAbsMag) {
                hds.SendingAbsMag = absMag;
                hds.KnobRoutine.Replace(hds, SlideRoutine(hds));
                PhotometerUtility.TogglePhotometerMode(absMag, hds.ConnectedPhotometer);
                if (playSfx) {
                    Sfx.PlayDetached("Oneshot.LabButtonC.Click", hds.ModeSwitch);
                }
            }
        }

        private static IEnumerator SlideRoutine(HistoricalDataState hds) {
            if (hds.SendingAbsMag) {
                yield return hds.ModeSwitch.RotateQuaternionTo(hds.ModeRotOn, 0.2f, Space.Self).Ease(Curve.BackOut);
            } else {
                yield return hds.ModeSwitch.RotateQuaternionTo(hds.ModeRotDefault, 0.2f, Space.Self).Ease(Curve.BackOut);
            }
            yield return null;
        }
    }
}