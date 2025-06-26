using FieldDay;
using FieldDay.Audio;
using FieldDay.Rendering;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class UserSettingsState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public string PlayerCode = null;
        [NonSerialized] public float MasterVolume;
        [NonSerialized] public bool CameraDriftEnabled = true;
        [NonSerialized] public bool HighQualityMode;
        [NonSerialized] public bool FullscreenEnabled;

        public void OnDeregister()
        {

        }

        public void OnRegister()
        {
            PlayerCode = PlayerPrefs.GetString("LatestPlayerCode", null);
        }
    }

    public static class SettingsUtility {
        public static void SetQualityMode(UserSettingsState state, bool mode) {
            state.HighQualityMode = mode;
        }
        public static void SetCameraDrift(UserSettingsState state, bool drift) {
            state.CameraDriftEnabled = drift;
        }

        public static void SetFullscreen(UserSettingsState state, bool fullscreen) {
            state.FullscreenEnabled = fullscreen;
            ScreenUtility.SetFullscreen(fullscreen);
        }

        public static void SetMasterVolume(UserSettingsState state, float set) {
            if (set < 0 || set > 1.0f) {
                throw new ArgumentOutOfRangeException("[SettingsUtility] Set volume "+set+" invalid! Must be 0 to 1");
            }
            state.MasterVolume = set;
            Sfx.SetBusVolume(AudioBus.Master, set);
        }
    }
}