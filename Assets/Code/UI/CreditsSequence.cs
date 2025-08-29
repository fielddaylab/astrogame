using Astro.Audio;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.HID;
using FieldDay.Scenes;
using ScriptableBake;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    [PreloadOrder(-10000)]
    public sealed class CreditsSequence : MonoBehaviour, IScenePreload, IBaked {
        public StreamingQuadTexture SkyRenderer;

        [Header("Positions")]
        public float StartY;
        public float FastStartY;
        public float EndY;
        public AnimationCurve MovementCurve;
        public float Duration;

        [Header("Stars")]
        public StreamingQuadTexture StarsRenderer;
        [HideInInspector] public float StarsPositionOffset;
        public float StarsParallax;

        [Header("Camera")]
        public Transform CameraTransform;
        public SceneReference MainScene;

        [Header("-- DEBUG --")]
        public bool DEBUG_UseFast;

        [HideInInspector] public string SkyTexture;
        [HideInInspector] public string StarsTexture;

        [NonSerialized] private bool m_IsFastMode;
        [NonSerialized] private Routine m_MoveRoutine;

        private void Awake() {
            Game.Scenes.QueueOnLoad(OnSceneStart);
        }

        private void OnSceneStart() {
            if (!m_IsFastMode) {
                Routine.Start(this, MainRoutine()).TryManuallyUpdate(0);
            } else {
                Routine.Start(this, FastRoutine()).TryManuallyUpdate(0);
            }
        }

        private IEnumerator MoveRoutine() {
            return Tween.ZeroToOne(SetCameraLerpedY, Duration);
        }

        private IEnumerator MainRoutine() {
            yield return 2;
            m_MoveRoutine = Routine.Start(this, MoveRoutine());

            yield return 15;
            yield return Tween.OneToZero((f) => StarsRenderer.Alpha = f, 5);

            StarsRenderer.gameObject.SetActive(false);
            SkyRenderer.gameObject.SetActive(false);

            yield return m_MoveRoutine;
            yield return 4;

            MusicUtility.StopMusic(2);
            Game.Scenes.LoadMainScene(MainScene);
        }

        private IEnumerator FastRoutine() {
            yield return 1;
            m_MoveRoutine = Routine.Start(this, MoveRoutine());

            // TODO: stuff

            yield return m_MoveRoutine;
            yield return 1;

            MusicUtility.StopMusic(2);
            Game.Scenes.LoadMainScene(MainScene);
        }

        private void SetCameraY(float y) {
            CameraTransform.localPosition = new Vector3(0, y, -10);

            float starsY = StarsPositionOffset + (y - StartY) * StarsParallax;

            StarsRenderer.transform.localPosition = new Vector3(0, starsY, 5);
        }

        private void SetCameraLerpedY(float f) {
            SetCameraY(Mathf.LerpUnclamped(StartY, EndY, MovementCurve.Evaluate(f)));
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            bool useFast = Game.Scenes.GetPreviousMainSceneIndex() <= 1;
#if UNITY_EDITOR
            if (DebugFlags.LaunchedFromThisScene) {
                useFast = DEBUG_UseFast;
            }
#endif // UNITY_EDITOR
            m_IsFastMode = useFast;

            if (!useFast) {
                StarsRenderer.Path = StarsTexture;
                SkyRenderer.Path = SkyTexture;
                StarsRenderer.gameObject.SetActive(true);
                SkyRenderer.gameObject.SetActive(true);
            } else {
                StarsRenderer.Unload();
                SkyRenderer.Unload();

                Duration = Duration * (EndY - FastStartY) / (EndY - StartY);
                StartY = FastStartY;
            }

            SetCameraY(StartY);
            return null;
        }

#if UNITY_EDITOR

        int IBaked.Order => -10;
        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            StarsPositionOffset = StarsRenderer.transform.localPosition.y;
            SkyTexture = SkyRenderer.Path;
            StarsTexture = StarsRenderer.Path;
            StarsRenderer.gameObject.SetActive(false);
            SkyRenderer.gameObject.SetActive(false);
            return true;
        }

#endif // UNITY_EDITOR
    }
}