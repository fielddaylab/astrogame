
using TMPro;
using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using FieldDay.UI;
using FieldDay.UI.Animation;
using FieldDay.Audio;
using FieldDay.SharedState;
using UnityEngine.Events;

namespace Astro {
    public class PauseMenuState : SharedStateComponent, IRegistrationCallbacks {
        #region Inspector

        public TMP_Text PlayerCodeDisplay;

        [Header("Button")]
        public Button Button;
        public Image ButtonImage;
        public Sprite PauseSprite;
        public Sprite ResumeSprite;
        [Header("Fader")]
        public FadeGroup Fader;
        //public Color FadeColor;
        public float TransitionTime;

        #endregion // Inspector

        [NonSerialized] public int CurrentUpdateMask;
        [NonSerialized] public int CurrentEventMask;
        [NonSerialized] public bool GamePaused;
        [NonSerialized] public Routine ButtonRoutine;

        private UnityAction m_StartTogglePause;

        public void OnRegister() {
            m_StartTogglePause = () => { PauseUtility.StartTogglePause(this); };
            Button.onClick.AddListener(m_StartTogglePause);
            PlayerCodeDisplay.SetTextAndActive(PlayerPrefs.GetString("LatestPlayerCode", null));
        }

        public void OnDeregister() {
            Button.onClick.RemoveListener(m_StartTogglePause);
        }
    }

    public static class PauseUtility {
        public static void SetPauseButtonActive(bool active, PauseMenuState state = null) {
            if (state == null) state = Find.State<PauseMenuState>();
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
                GameLoop.ResumeUpdates(AstroGame.PauseUpdateMask);
                //PauseCutscenes();
                state.CurrentEventMask = input.Raycaster.eventMask;
                InputUtility.SetClickableMaskCustom(input, LayerMasks.UI_Mask);
                Game.Events.Dispatch(GameEvents.GamePaused);
            } else {
                InputUtility.SetClickableMaskDefault(input);
                InputUtility.SetClickableMaskCustom(input, state.CurrentEventMask);
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
