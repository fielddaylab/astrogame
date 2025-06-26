using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.Files;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astro {
    public class DomeLightState : SharedStateComponent {
        public Material DefaultMaterial;
        [StreamingImagePath] public string DomeEmissionPath;
        public Light DomeLight;
        public Color BlackLightColor;
        public Color NormalColor;
        [NonSerialized] public bool BlackLightEnabled;
    }

    public static class DomeLightUtility {
        [LeafMember("SetDomeGlow")]
        public static void LeafSetDomeGlow(bool glow) {
            DomeLightState state = Find.State<DomeLightState>();
            state.BlackLightEnabled = glow;

            if (glow) {
                FileLoadRequest texReq = new FileLoadRequest();
                texReq.Location = FileLocation.Streaming;
                texReq.Mode = FileBufferMode.Texture;
                texReq.Path = state.DomeEmissionPath;
                texReq.Flags = 0;
                texReq.Callback = LoadEmissionTexture;
                texReq.CallbackContext = state;
                Game.Files.RequestFile(texReq, FileLoadPriority.High);
            } else {
                Sfx.PlayDetached("Oneshot.LabButtonC.Click", state.transform);
                AssetUtility.ManualUnload(state.DefaultMaterial.GetTexture("_EmissionMap"));
                state.DefaultMaterial.SetKeyword(new LocalKeyword(state.DefaultMaterial.shader, "_EMISSION"), state.BlackLightEnabled);
                state.DomeLight.color = state.NormalColor;
            }
        }

        private static void LoadEmissionTexture(FileLoadRequest request, FileLoadResult result, object context) {

            DomeLightState state = Find.State<DomeLightState>();
            Sfx.PlayDetached("Oneshot.LabButtonC.Click", state.transform);
            state.DefaultMaterial.SetTexture("_EmissionMap", result.ReadTexture());
            state.DefaultMaterial.SetKeyword(new LocalKeyword(state.DefaultMaterial.shader, "_EMISSION"), state.BlackLightEnabled);
            state.DomeLight.color = state.BlackLightEnabled ? state.BlackLightColor : state.NormalColor;
        }
    }
}