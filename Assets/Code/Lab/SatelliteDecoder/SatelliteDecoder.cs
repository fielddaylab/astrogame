using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SatelliteDecoder : ScriptActorComponent
    {
        [SerializeField] private GameObject BlankPanel; 
        [SerializeField] private GameObject ActivatedPanel;

        public void SetDecoderActive(bool active)
        {
            BlankPanel.SetActive(!active);
            ActivatedPanel.SetActive(active);
        }

        [LeafMember("SetDecoderActive")]
        public void LeafSetDecoderActive(bool active)
        {
            SetDecoderActive(active);
        }

        [LeafMember("SwapPanel")]
        public void LeafSwapPanel()
        {
            if (BlankPanel.activeInHierarchy) {
                BlankPanel.SetActive(false);
                ActivatedPanel.SetActive(true);
            }
            else{
                BlankPanel.SetActive(true);
                ActivatedPanel.SetActive(false);
            }
        }
    }
}