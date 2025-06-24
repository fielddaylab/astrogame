using FieldDay;
using FieldDay.Audio;
using FieldDay.SharedState;
using System;

namespace Astro {
    public class UserSettingsState : SharedStateComponent {
        [NonSerialized] public float MasterVolume;
        [NonSerialized] public bool CameraDriftEnabled = true;
        [NonSerialized] public bool HighQualityMode;
    }

    public static class SettingsUtility {
        public static void SetQualityMode(UserSettingsState state, bool mode) {
            state.HighQualityMode = mode;
        }
        public static void SetCameraDrift(UserSettingsState state, bool drift) {
            state.CameraDriftEnabled = drift;
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