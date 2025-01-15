using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Components;
using TMPro;
using BeauUtil;

namespace Astro
{
    public class ColorDataDisplayTarget : BatchedComponent, IRegistrationCallbacks
    {
        public DataDisplay Display;
        public ColorPanel Panel;

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
        public static void OnRequest(ColorDataDisplayTarget display, DataPacket packet, DataFormattingFlags flags)
        {
            SetPanelMaterials(display.Panel.PanelMesh, packet.Value.AssetId);
        }

        public static void OnClear(ColorDataDisplayTarget display)
        {
            SetPanelMaterials(display.Panel.PanelMesh, null);
        }

        public static void SetPanelMaterials(MeshRenderer mesh, StringHash32 colorId)
        {
            if (mesh == null) { return; }

            Material newMat = colorId.IsEmpty ? newMat = Find.State<ColorTextureState>().PanelBlank : Find.NamedAsset<ReferenceColor>(colorId).Texture;

            var mats = mesh.sharedMaterials;
            mats[0] = newMat;
            mesh.sharedMaterials = mats;
        }
    }
}