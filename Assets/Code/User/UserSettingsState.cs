using Astro.Save;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Data;
using FieldDay.Rendering;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class UserSettingsState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject 
    {
        [NonSerialized] public string PlayerCode = null;
        [NonSerialized] public float MasterVolume;
        [NonSerialized] public bool CameraDriftEnabled = true;
        [NonSerialized] public bool HighQualityMode;
        [NonSerialized] public bool FullscreenEnabled;

        public void OnDeregister()
        {
            AstroGame.SaveBuffer.DeregisterHandler("UserSettingsState");
        }

        public void OnRegister()
        {
            PlayerCode = PlayerPrefs.GetString("LatestPlayerCode", null);
            AstroGame.SaveBuffer.RegisterHandler("UserSettingsState", this);
        }

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch)
        {
            float volume = reader.Read<float>();
            SettingsUtility.SetMasterVolume(this, volume);

            bool cameraDrift = reader.Read<bool>();
            SettingsUtility.SetCameraDrift(this, cameraDrift);

            bool highQuality = reader.Read<bool>();
            SettingsUtility.SetQualityMode(this, highQuality);

            bool fullscreen = reader.Read<bool>();
            SettingsUtility.SetFullscreen(this, fullscreen);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch)
        {
            writer.Write((float)MasterVolume);
            writer.Write((bool)CameraDriftEnabled);
            writer.Write((bool)HighQualityMode);
            writer.Write((bool)FullscreenEnabled);
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