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
    public class ParallaxGraphAnimation : BatchedComponent, IRegistrationCallbacks {
        [NonSerialized] public float Scale;

        public MeshRenderer DisplayTarget;
        public DataSlot DataTarget;

        [NonSerialized] public MaterialPropertyBlock MaterialProperties;

        public void OnDeregister() {
        }

        public void OnRegister() {
            //GraphDisplay.OnDisplayRequested.Register(
            //    (packet, flags) => ParallaxDataUtility.OnDisplayRequest(this, packet, flags));
            //GraphDisplay.OnDisplayCleared.Register(
            //    () => ParallaxDataUtility.OnDisplayClear(this));

            MaterialProperties = new MaterialPropertyBlock();
        }
    }

    public static class ParallaxDataUtility {

        public static void OnDisplayRequest(ParallaxGraphAnimation graph, DataPacket packet, DataFormattingFlags flags) {
        }
        
        public static void OnDisplayClear(ParallaxGraphAnimation graph) {
            ClearPattern(graph);
        }

        public static void ClearPattern(ParallaxGraphAnimation graph) {
            
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