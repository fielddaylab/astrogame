
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;
using FieldDay.Scenes;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Astro {
    public class SettingsMenu : BatchedComponent, IRegistrationCallbacks {
        public Toggle DriftToggle;
        public Toggle FullscreenToggle;
        public Slider VolumeSlider;
        public Slider MusicSlider;
        public Slider SFXSlider;
        public Slider VoiceSlider;

        public void OnDeregister() {
            DriftToggle.onValueChanged.RemoveAllListeners();
            FullscreenToggle.onValueChanged.RemoveAllListeners();

            VolumeSlider.onValueChanged.RemoveAllListeners();
            MusicSlider.onValueChanged.RemoveAllListeners();
            SFXSlider.onValueChanged.RemoveAllListeners();
            VoiceSlider.onValueChanged.RemoveAllListeners();

            Game.Rendering.OnFullscreenChanged.Deregister(OnFullscreenUpdated);
        }

        public void OnRegister() {
            UserSettingsState state = Find.State<UserSettingsState>();

            Game.Rendering.OnFullscreenChanged.Register(OnFullscreenUpdated);

            DriftToggle.SetIsOnWithoutNotify(state.CameraDriftEnabled);
            FullscreenToggle.SetIsOnWithoutNotify(ScreenUtility.GetFullscreen());
            UpdateCameraDrift(DriftToggle.isOn);
            UpdateFullscreen(FullscreenToggle.isOn);
            DriftToggle.onValueChanged.AddListener(UpdateCameraDrift);
            FullscreenToggle.onValueChanged.AddListener(UpdateFullscreen);

            VolumeSlider.onValueChanged.AddListener(UpdateVolume);
            MusicSlider.onValueChanged.AddListener((float vol) => UpdateBusVolume(SettingsUtility.MUSIC_BUS_ID, vol));
            SFXSlider.onValueChanged.AddListener((float vol) => UpdateBusVolume(SettingsUtility.SFX_BUS_ID, vol));
            VoiceSlider.onValueChanged.AddListener((float vol) => UpdateBusVolume(SettingsUtility.VO_BUS_ID, vol));
            InitializeVolumeSettings();
        }

        private void InitializeVolumeSettings() {
            UserSettingsState settings = Find.State<UserSettingsState>();
            VolumeSlider.value = settings.MasterVolume;
            MusicSlider.value = settings.MusicVolume;
            SFXSlider.value = settings.SFXVolume;
            VoiceSlider.value = settings.VoiceVolume;
         }

        private void UpdateCameraDrift(bool toggle) {
            UserSettingsState settings = Find.State<UserSettingsState>();
            SettingsUtility.SetCameraDrift(settings, toggle);
        }

        private void UpdateFullscreen(bool toggle) {
            SettingsUtility.SetFullscreen(Find.State<UserSettingsState>(), toggle);
        }

        private void UpdateVolume(float volume) {
            SettingsUtility.SetMasterVolume(Find.State<UserSettingsState>(), volume);
        }

        private void UpdateBusVolume(StringHash32 bus, float volume) {
            SettingsUtility.SetAudioBusVolume(Find.State<UserSettingsState>(), bus, volume);
        }

        private void OnFullscreenUpdated(bool fullscreen) {
            FullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }
    }
}