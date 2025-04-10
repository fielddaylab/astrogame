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
        public ColorPanel[] Panels;

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

            for (int i = 0; i < Panels.Length; i++) {
                if (Panels[i].PopulateOnRegister) {
                    ColorDataUtility.SetPanelMaterials(Panels[i].PanelMesh, Panels[i].ColorId, false);
                }
            }
        }
    }

    public static partial class ColorDataUtility
    {
        public static void OnRequest(ColorDataDisplaySource display, DataPacket packet, DataFormattingFlags flags)
        {
            for (int i = 0; i < display.Panels.Length; i++) {
                if (display.Panels[i].ColorId.Equals(packet.Value.AssetId)) {
                    SetIndicatorMaterials(display.Panels[i].IndicatorMesh, true);
                }
                else {
                    SetIndicatorMaterials(display.Panels[i].IndicatorMesh, false);
                }
            }
        }

        public static void OnClear(ColorDataDisplaySource display)
        {
            for (int i = 0; i < display.Panels.Length; i++) {
                SetIndicatorMaterials(display.Panels[i].IndicatorMesh, false);
            }
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