using UnityEngine;
using FieldDay;
using FieldDay.SharedState;
using System;
using BeauUtil;
using System.Collections.Generic;
using Leaf.Runtime;

namespace Astro {
    public class NeutrinoHighlightState : SharedStateComponent, IRegistrationCallbacks {
        public Sprite HighlightVisibleSprite;
        public Sprite HighlightNotVisibleSprite;
        public float HighlightNotVisibleAlpha = 0.4f;

        [NonSerialized] public bool OpenModeStarted;
        [NonSerialized] public bool OpenModeEnded;

        [NonSerialized] public List<CelestialAsset> SubmissionObjects = new List<CelestialAsset>();

        [NonSerialized] public RingBuffer<Transform> ActiveHighlights = new RingBuffer<Transform>(8, RingBufferMode.Expand);

        #region Registration

        private Action setOpenModeStarted;
        private Action updateNeutrinoHighlights;
        private Action setOpenModeEnded;

        public void OnRegister() {
            setOpenModeStarted = () => {
                OpenModeStarted = true;

                DayConfigAsset config = DayConfigUtil.GetConfigForState();
                SubmissionObjects.Clear();
                SubmissionObjects.AddRange(config.NeutrinoEvent.RelevantObjects);
            };
            setOpenModeEnded = () => { OpenModeEnded = true; };
            updateNeutrinoHighlights = () => { OpenModeStarted = true; };

            Game.Events.Register(GameEvents.UpdateOpenIdSubmission, updateNeutrinoHighlights);
            Game.Events.Register(GameEvents.StartOpenMode, setOpenModeStarted);
            Game.Events.Register(GameEvents.StopOpenMode, setOpenModeEnded);
        }

        public void OnDeregister() {
            SubmissionObjects.Clear();
            Game.Events.Deregister(GameEvents.StartOpenMode, setOpenModeStarted);
            Game.Events.Deregister(GameEvents.UpdateOpenIdSubmission, updateNeutrinoHighlights);
            Game.Events.Deregister(GameEvents.StopOpenMode, setOpenModeEnded);
        }

        #endregion // Registration
    }

    public static class NeutrinoEventUtil {
        public static bool IsAssetInNeutrinoEvent(CelestialAsset asset, NeutrinoHighlightState state = null) {
            if (state == null)
            {
                state = Find.State<NeutrinoHighlightState>();
            }

            if (state.SubmissionObjects.Contains(asset)) return true;
            return false;
        }

        public static bool IsIdInNeutrinoEvent(StringHash32 assetId, NeutrinoHighlightState state = null) {
            if (state == null)
            {
                state = Find.State<NeutrinoHighlightState>();
            }

            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(assetId);

            if (state.SubmissionObjects.Contains(asset)) return true;
            return false;
        }

        [LeafMember("AddAssetToSubmissionGroup")]
        public static void LeafAddAssetToSubmissionGroup(StringHash32 assetId) {
            NeutrinoHighlightState state = Find.State<NeutrinoHighlightState>();

            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(assetId);
            if (asset == null) {
                Debug.LogWarning("[LeafAddAssetToSubmissionGroup] could not add asset to submission group: " + assetId.ToDebugString());
                return;
            }

            state.SubmissionObjects.Add(Find.NamedAsset<CelestialAsset>(assetId));
        }

        [LeafMember("RemoveAssetToSubmissionGroup")]
        public static void LeafRemoveAssetToSubmissionGroup(StringHash32 assetId) {
            NeutrinoHighlightState state = Find.State<NeutrinoHighlightState>();

            CelestialAsset asset = state.SubmissionObjects.Find(asset => asset.AssetId == assetId);
            if (asset == null) {
                Debug.LogWarning("[LeafRemoveAssetToSubmissionGroup] could not remove asset from submission group: " + assetId);
                return;
            }

            state.SubmissionObjects.Remove(asset);

        }
    }
}
