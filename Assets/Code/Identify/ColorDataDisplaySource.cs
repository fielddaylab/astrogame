using System;
using FieldDay;
using FieldDay.Components;

using UnityEngine;

namespace Astro {
    public class ColorDataDisplaySource : BatchedComponent, IRegistrationCallbacks {
        public DataDisplay Display;
        public MeshRenderer Panel;

        private Action<DataPacket, DataFormattingFlags> m_SetPanelMats;
        private Action m_ClearPanelMats;
        public void OnRegister() {
            m_SetPanelMats = (p, f) => { ColorDataUtility.OnRequest(this, p, f); };
            m_ClearPanelMats = () => { ColorDataUtility.OnClear(this); };

            Display.OnDisplayRequested.Register(m_SetPanelMats);
            Display.OnDisplayCleared.Register(m_ClearPanelMats);
        }

        public void OnDeregister() {
            Display.OnDisplayRequested.Deregister(m_SetPanelMats);
            Display.OnDisplayRequested.Deregister(m_ClearPanelMats);
        }

    }

    public static partial class ColorDataUtility{
        public static void OnRequest(ColorDataDisplaySource display, DataPacket packet, DataFormattingFlags flags) {
            SetPanelMaterials(display.Panel, packet.Value.AssetId, true);
        }

        public static void OnClear(ColorDataDisplaySource display) {
            SetPanelMaterials(display.Panel, default, true);
        }

        // Unused?
        public static void SetIndicatorMaterials(MeshRenderer mesh, bool active) {
            if (mesh == null) { return; }

            Material activeIndicatorMat = Find.State<ColorTextureState>().IndicatorActive;
            Material inactiveIndicatorMat = Find.State<ColorTextureState>().IndicatorInactive;

            Material newMat = active ? activeIndicatorMat : inactiveIndicatorMat;

            var mats = mesh.sharedMaterials;
            mats[0] = newMat;
            mesh.sharedMaterials = mats;
        }
    }
}