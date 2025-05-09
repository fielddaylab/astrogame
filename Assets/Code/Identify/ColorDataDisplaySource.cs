using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Components;
using TMPro;

namespace Astro
{
    public class ColorDataDisplaySource : BatchedComponent, IRegistrationCallbacks
    {
        public DataDisplay Display;
        public MeshRenderer Panel;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Display.OnDisplayRequested.Register(
                (p, f) => { ColorDataUtility.OnRequest(this, p, f); }
                );
            Display.OnDisplayCleared.Register(
                () => { ColorDataUtility.OnClear(this); }
                );
        }
    }

    public static partial class ColorDataUtility
    {
        public static void OnRequest(ColorDataDisplaySource display, DataPacket packet, DataFormattingFlags flags)
        {
            ColorDataUtility.SetPanelMaterials(display.Panel, packet.Value.AssetId, false);
        }

        public static void OnClear(ColorDataDisplaySource display)
        {
            ColorDataUtility.SetPanelMaterials(display.Panel, default, false);
        }

        public static void SetIndicatorMaterials(MeshRenderer mesh, bool active)
        {
            if (mesh == null) { return; }

            Material newMat = active ? newMat = Find.State<ColorTextureState>().IndicatorActive : Find.State<ColorTextureState>().IndicatorInactive;

            var mats = mesh.sharedMaterials;
            mats[0] = newMat;
            mesh.sharedMaterials = mats;
        }
    }
}