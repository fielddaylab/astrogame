using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    [PreloadOrder(100)]
    public class SkyGenerationState : SharedStateComponent, IScenePreload {
        public Sprite DefaultStarSprite;
        public Sprite DataSubmittedStarSprite;
        public Sprite DataSubmittedNeutrinoStarSprite;
        public Sprite DefaultPlanetSprite;
        public Sprite DefaultGalaxySprite;
        [NonSerialized] public CelestialObjectVisMask VisMask = CelestialObjectVisMask.Visible;
        [NonSerialized] public bool IsDirty = true;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.QueueOnEnable(() => {
                Game.Scenes.RegisterLoadDependency(Async.Schedule(DoPreload(this), AsyncFlags.MainThreadOnly));
            });
            return null;
        }

        static private IEnumerator DoPreload(SkyGenerationState state) {
            // var layout = Find.GlobalAsset<SkyLayoutAsset>();
            var dome = Find.State<SkyDome>();
            var focusPools = Find.State<FocusPools>();
            var focusState = Find.State<FocusState>();
            var spaceCamera = Find.State<SpaceCameraState>();

            Vector3 camPos = spaceCamera.Camera.RootTransform.position;
            Vector3 camUp = spaceCamera.Camera.RootTransform.up;

            spaceCamera.StarRoot.position = camPos;

            int fociiAllocated = 0;
            foreach (var obj in dome.AboveHorizon) {
                var newFocus = focusPools.Focii.Alloc(spaceCamera.StarRoot);
                // TODO: assign relevant 2D representation
                Sprite sprite = DetermineSprite(state, obj.Resource.Category, obj.Resource, out Vector2 spriteSize);
                FocusableUtility.InitFocusable(focusState, newFocus, obj.transform, obj.Resource, sprite, spriteSize);
                focusState.ActiveFocii.PushBack(newFocus);
                newFocus.IsVisibleInCurrentFilter = (newFocus.TargetData.Visibility & state.VisMask) != 0;

                newFocus.Represent2D.enabled = false;
                newFocus.Clickable.enabled = false;

                UIFocusPackedData packed;
                packed.TargetPos = obj.transform.position;
                packed.TargetVector = Vector3.Normalize(packed.TargetPos - camPos);

                focusState.ActiveFociiPacked[fociiAllocated++] = packed;

                newFocus.Root.SetLocalPositionAndRotation(packed.TargetVector * 20, Quaternion.LookRotation(-packed.TargetVector, camUp));
                yield return null;
            }

            spaceCamera.LookUpdatedThisFrame = true;
        }

        static private Sprite DetermineSprite(SkyGenerationState state, CelestialObjectCategory category, CelestialAsset asset, out Vector2 size) {
            size = new Vector2(0.32f, 0.32f);
            switch (category) {
                case CelestialObjectCategory.Star:
                    bool inNeutrinoEvent = NeutrinoEventUtil.IsAssetInNeutrinoEvent(asset);
                    bool hasDataToDisplay = CelestialDataDisplayUtil.HasIdDataToDisplay(asset);

                    if (inNeutrinoEvent & hasDataToDisplay) {
                        size = new Vector2(0.64f, 0.64f);
                        return state.DataSubmittedNeutrinoStarSprite;
                    } else if (hasDataToDisplay) {
                        size = new Vector2(0.64f, 0.64f);
                        return state.DataSubmittedStarSprite;
                    } else {
                        size = new Vector2(0.32f, 0.32f);
                        return state.DefaultStarSprite;
                    }
                case CelestialObjectCategory.Planet:
                    return state.DefaultPlanetSprite;
                case CelestialObjectCategory.Satellite:
                    return null;
                case CelestialObjectCategory.Constellation:
                    return null;
                case CelestialObjectCategory.Galaxy:
                    return state.DefaultGalaxySprite;
                case CelestialObjectCategory.Comet:
                    return null;
                default:
                    return null;
            }
        }
    }
}