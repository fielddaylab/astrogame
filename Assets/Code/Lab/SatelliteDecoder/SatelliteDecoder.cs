using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.UI;
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

        public void SetDecoderActive(bool active) {
            BlankPanel.SetActive(!active);
            ActivatedPanel.SetActive(active);
            if (active) {
                DisplayDecoderClues();
            } else {
                Find.State<PuzzleState>().Display.Clues.Text.SetTextAndActive("");
            }
        }

        private void DisplayDecoderClues() {
            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                foreach (string clue in Find.GlobalAsset<FinalPuzzleCluesAsset>().Clues) {
                    psb.Builder.Append("<sprite name=\"hint-bullet\">");
                    psb.Builder.Append(clue);
                    psb.Builder.Append("\n");
                    Find.State<PuzzleState>().Display.Clues.Text.SetTextAndActive(psb.Builder);
                }
            }
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