using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Components;
using TMPro;
using BeauUtil;
using FieldDay.Rendering;

namespace Astro
{
    public class ColorDataDisplayTarget : BatchedComponent, IRegistrationCallbacks
    {
        public DataDisplay Display;
        public ColorPanel Panel;
        public bool UnlitMaterial;

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
            SetPanelMaterials(display.Panel.PanelMesh, packet.Value.AssetId, display.UnlitMaterial);
        }

        public static void OnClear(ColorDataDisplayTarget display) {
            SetPanelMaterials(display.Panel.PanelMesh, null, false);
        }

        public static void SetPanelMaterials(MeshRenderer mesh, StringHash32 colorId, bool unlit) {
            if (mesh == null) { return; }

            Material newMat;
            if (colorId.IsEmpty) {
                newMat = Find.State<ColorTextureState>().PanelBlank;
            } else {
                var asset = Find.NamedAsset<ReferenceColor>(colorId);
                newMat = unlit ? asset.UnlitTexture : asset.Texture;
            };

            mesh.SetSharedMaterialAtIndex(0, newMat);
        }
    }
}