
using FieldDay;
using FieldDay.Components;
using UnityEngine.UI;

namespace Astro {
    public class SettingsMenu : BatchedComponent, IRegistrationCallbacks {
        public Button ToggleCameraDriftButton;
        public Image ToggleCameraDriftCheck;
        public Slider VolumeSlider;

        //private bool CameraDriftEnabled;

        public void OnDeregister() {
            ToggleCameraDriftButton.onClick.RemoveAllListeners();
            VolumeSlider.onValueChanged.RemoveAllListeners();
        }

        public void OnRegister() {
            //CameraDriftEnabled = Find.State<UserSettingsState>().CameraDriftEnabled;
            ToggleCameraDriftCheck.gameObject.SetActive(Find.State<UserSettingsState>().CameraDriftEnabled);
            ToggleCameraDriftButton.onClick.AddListener(UpdateCameraDrift);
            VolumeSlider.onValueChanged.AddListener(UpdateVolume);
        }

        private void UpdateCameraDrift() {
            UserSettingsState settings = Find.State<UserSettingsState>();
            SettingsUtility.SetCameraDrift(settings, !settings.CameraDriftEnabled);
            ToggleCameraDriftCheck.gameObject.SetActive(settings.CameraDriftEnabled);
        }

        private void UpdateVolume(float volume) {
            SettingsUtility.SetMasterVolume(Find.State<UserSettingsState>(), volume);
        }

    }
}