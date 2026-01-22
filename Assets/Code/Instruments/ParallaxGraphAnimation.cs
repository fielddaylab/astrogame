using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class ParallaxGraphAnimation : BatchedComponent, IRegistrationCallbacks {
        [NonSerialized] public float Scale;

        [Header("Inspector")]
        public DataSlot LinkedDistance;
        public GameObject AnimRoot;
        public SpriteRenderer EarthSprite;
        public SpriteRenderer StarImageSprite;
        public SpriteRenderer RealStarSprite;
        public Transform Ruler;
        public LineRenderer Line;

        [Header("Orbit Settings")]
        public bool OrbitInX;
        public float OrbitRadius;
        public Timer OrbitTimer;
        public float ParallaxMultiplier;

        [NonSerialized] public float ParallaxFactor;
        [NonSerialized] public Vector3 EarthInitPos;
        [NonSerialized] public Vector3 StarInitPos;
        [NonSerialized] public DataDisplay DistanceDisplay;

        [NonSerialized] private Action<DataPacket, DataFormattingFlags> m_HandleDisplayRequest;
        [NonSerialized] private Action m_HandleDisplayCleared;

        public void OnDeregister() {
            DistanceDisplay.OnDisplayRequested.Deregister(m_HandleDisplayRequest);
            DistanceDisplay.OnDisplayCleared.Deregister(m_HandleDisplayCleared);
        }

        public void OnRegister() {
            EarthInitPos = EarthSprite.transform.localPosition;
            StarInitPos = StarImageSprite.transform.localPosition;
            Line.SetPosition(0, EarthSprite.transform.position);
            Line.SetPosition(1, StarImageSprite.transform.position);

            m_HandleDisplayRequest = (packet, flags) => ParallaxDataUtility.OnDisplayRequest(this, packet, flags);
            m_HandleDisplayCleared = () => ParallaxDataUtility.OnDisplayClear(this);

            DistanceDisplay = LinkedDistance.Displays[0];
            DistanceDisplay.OnDisplayRequested.Register(m_HandleDisplayRequest);
            DistanceDisplay.OnDisplayCleared.Register(m_HandleDisplayCleared);
        }
    }

    public static class ParallaxDataUtility {
        public static readonly Vector3 LINE_Z_OFFSET = new Vector3(0, 0, 0.4f);

        public static void OnDisplayRequest(ParallaxGraphAnimation anim, DataPacket packet, DataFormattingFlags flags) {
            if ((packet.Type & DataTypeMask.Distance) != 0) {
                SetParallaxFactor((float)packet.Value.Distance, anim);
            } else {
                ResetAnimation(anim);
            }
        }
        public static void SetParallaxFactor(float distance, ParallaxGraphAnimation anim) {
            ResetAnimation(anim);
            if (distance <= 0) {
                return;
            }
            anim.ParallaxFactor = -anim.ParallaxMultiplier / distance;
            anim.Ruler.localScale = anim.OrbitInX ?
                new Vector3(2, -2 * anim.ParallaxFactor * anim.OrbitRadius, 1) :
                new Vector3(-2 * anim.ParallaxFactor * anim.OrbitRadius, 2, 1);
            // middle school algebra don't fail me now
            if (anim.OrbitInX) {
                float d = (anim.StarInitPos.y * distance) / (distance + anim.ParallaxMultiplier);
                anim.RealStarSprite.transform.localPosition = new Vector3(0, d, 0);
            } else {
                float d = (anim.StarInitPos.x * distance) / (distance + anim.ParallaxMultiplier);
                anim.RealStarSprite.transform.localPosition = new Vector3(d, 0, 0);
            }
            anim.Ruler.gameObject.SetActive(true);
            anim.Line.gameObject.SetActive(true);
            anim.RealStarSprite.gameObject.SetActive(true);
        }

        public static void ResetAnimation(ParallaxGraphAnimation anim) {
            anim.StarImageSprite.transform.localPosition = anim.StarInitPos;
            anim.EarthSprite.transform.localPosition = anim.EarthInitPos;
            UpdateSpline(anim);
            anim.Ruler.gameObject.SetActive(false);
            anim.Line.gameObject.SetActive(false);
            anim.RealStarSprite.gameObject.SetActive(false);
        }

        public static void UpdateSpline(ParallaxGraphAnimation anim) {
            anim.Line.SetPosition(0, anim.EarthSprite.transform.localPosition + LINE_Z_OFFSET);
            anim.Line.SetPosition(1, anim.StarImageSprite.transform.localPosition + LINE_Z_OFFSET);
        }

        public static void OnDisplayClear(ParallaxGraphAnimation anim) {
            ResetAnimation(anim);
        }

        public static void ToggleInstrumentMode() {
            HistoricalDataState hds = Find.State<HistoricalDataState>();
            SetInstrumentMode(!hds.SendingAbsMag, hds, false, true);
            AstroGame.Events.Dispatch(GameEvents.ToggleMagMode);
        }

        public static void SetInstrumentMode(bool absMag, HistoricalDataState hds, bool force, bool playSfx) {
            if (!force && absMag == hds.SendingAbsMag) return;

            hds.SendingAbsMag = absMag;
            hds.KnobRoutine.Replace(hds, SlideRoutine(hds));
            hds.KnobRoutine.OnStop(() => { SwapParallaxToggleDisplay(hds, absMag); } );
            hds.KnobRoutine.OnComplete(() => { SwapParallaxToggleDisplay(hds, absMag); } );
            PhotometerUtility.TogglePhotometerMode(absMag, hds.ConnectedPhotometer);
            if (playSfx) {
                Sfx.PlayDetached("Oneshot.LabButtonC.Click", hds.ModeSwitch);
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

        private static void SwapParallaxToggleDisplay(HistoricalDataState hds, bool active) {
            hds.InstrumentMesh.SetSharedMaterialAtIndex(2, active ? hds.BottomPanelEnabledMaterial : hds.BottomPanelDisabledMaterial);
        }
    }
}