using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public class PauseMenuState : SharedStateComponent, IRegistrationCallbacks {
        #region Inspector

        [Header("Button")]
        public Button Button;
        public Image ButtonImage;
        public Sprite PauseSprite;
        public Sprite ResumeSprite;
        [Header("Fader")]
        public FadeGroup Fader;
        //public Color FadeColor;
        public float TransitionTime;

        [NonSerialized] public int CurrentUpdateMask;
        [NonSerialized] public bool GamePaused;
        [NonSerialized] public Routine ButtonRoutine;

        public void OnDeregister() {
        }

        public void OnRegister() {
            Button.onClick.AddListener(() => PauseUtility.StartTogglePause(this));
        }
        #endregion // Inspector
    }

    public static class PauseUtility {
        public static void SetPauseButtonActive(PauseMenuState state, bool active) {
            state.Button.interactable = active;
            state.Button.gameObject.SetActive(active);
        }

        public static void StartTogglePause(PauseMenuState state) {
            if (state.GamePaused) {
                TogglePaused(state);
                state.Fader.Hide();
                state.ButtonRoutine.Replace(SlideButtonOut(state));
            } else {
                state.Fader.Show();
                state.ButtonRoutine.Replace(SlideButtonIn(state))
                    .OnComplete(()=>TogglePaused(state));
            }
        }

        public static void TogglePaused(PauseMenuState state) {
            SetPaused(state, !state.GamePaused);
        }

        private static void SetPaused(PauseMenuState state, bool paused) {
            if (Find.State<PauseMenuState>().GamePaused == paused) {
                return;
            }
            state.GamePaused = paused;

            Routine.Settings.Paused = paused;
            //GameLoop.TimeScale = state.GamePaused ? 0f : 1f;
            Time.timeScale = paused ? 0 : 1;
            AudioListener.pause = paused;
            Sfx.SetBusPaused(AudioBus.Master, paused);

            InputState input = Find.State<InputState>();
            if (paused) {
                state.CurrentUpdateMask = GameLoop.UpdateMask;
                GameLoop.SuspendUpdates(Bits.All32);
                //PauseCutscenes();
                InputUtility.SetClickableMaskCustom(input, LayerMasks.UI_Mask);
                Game.Events.Dispatch(GameEvents.GamePaused);
            } else {
                InputUtility.SetClickableMaskDefault(input);
                GameLoop.ResumeUpdates(state.CurrentUpdateMask);
                //ResumeCutscenes();
                Game.Events.Dispatch(GameEvents.GameResumed);
            }
        }

        private static void PauseCutscenes() {
            foreach (CutscenePlayer player in Find.Components<CutscenePlayer>()) {
                if (player.isActiveAndEnabled) {
                    player.Director.Pause();
                }
            }
        }

        private static void ResumeCutscenes() {
            foreach (CutscenePlayer player in Find.Components<CutscenePlayer>()) {
                if (player.isActiveAndEnabled) {
                    player.Director.Resume();
                }
            }
        }

        private static IEnumerator SlideButtonIn(PauseMenuState state) {
            state.Button.interactable = false;
            yield return state.Button.transform.ScaleTo(1.5f, state.TransitionTime, Axis.XY).Ease(Curve.CubeIn);
            state.ButtonImage.sprite = state.ResumeSprite;
            state.Button.interactable = true;
        }

        private static IEnumerator SlideButtonOut(PauseMenuState state) {
            state.Button.interactable = false;
            yield return state.Button.transform.ScaleTo(1.0f, state.TransitionTime, Axis.XY).Ease(Curve.CubeIn);
            state.ButtonImage.sprite = state.PauseSprite;
            state.Button.interactable = true;
        }
    }
}
