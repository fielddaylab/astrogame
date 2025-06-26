
using FieldDay;
using FieldDay.Components;
using UnityEngine.UI;

namespace Astro {
    public class SettingsMenu : BatchedComponent, IRegistrationCallbacks {
        public Toggle DriftToggle;
        public Toggle FullscreenToggle;
        public Slider VolumeSlider;

        public void OnDeregister() {
            DriftToggle.onValueChanged.RemoveAllListeners();
            VolumeSlider.onValueChanged.RemoveAllListeners();
        }

        public void OnRegister() {
            UpdateCameraDrift(DriftToggle.isOn);
            UpdateFullscreen(FullscreenToggle.isOn);
            DriftToggle.onValueChanged.AddListener(UpdateCameraDrift);
            FullscreenToggle.onValueChanged.AddListener(UpdateFullscreen);
            VolumeSlider.onValueChanged.AddListener(UpdateVolume);
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

    }
}