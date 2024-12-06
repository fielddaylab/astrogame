

using FieldDay;
using FieldDay.SharedState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public class RefGuideRenderState : SharedStateComponent {
        public Camera RefRenderCam;

        [Header("UI")]
        public TMP_Text LeftTitle;
        public TMP_Text[] LeftRows;
        public TMP_Text RightTitle;
        public TMP_Text[] RightRows;
        public Image BackgroundImage;

        [HideInInspector] public bool RenderNeedsRefresh;
    }


    public static partial class ReferenceUtility {
        public static void PopulateReferenceCanvas(ReferencePageAsset page, RefGuideRenderState rgrs = null) {
            if (rgrs == null) {
                rgrs = Find.State<RefGuideRenderState>();
            }

            if (page == null) {
                ClearReferenceCanvas(rgrs);
                rgrs.RenderNeedsRefresh = true;
                return;
            }

            for (int i = 0; i < rgrs.LeftRows.Length; i++) {
                if (i < page.EntriesLeft.Length) {
                    rgrs.LeftRows[i].SetText(page.EntriesLeft[i].DisplayText);
                }
                if (i < page.EntriesRight.Length) {
                    rgrs.RightRows[i].SetText(page.EntriesRight[i].DisplayText);
                }
            }

            if (page.BackgroundSprite != null) {
                rgrs.BackgroundImage.enabled = true;
                rgrs.BackgroundImage.sprite = page.BackgroundSprite;
            } else {
                rgrs.BackgroundImage.enabled = false;
            }

            rgrs.RenderNeedsRefresh = true;
        }

        private static void ClearReferenceCanvas(RefGuideRenderState rgrs) {
            for (int i = 0; i < rgrs.LeftRows.Length; i++) {
                rgrs.LeftRows[i].SetText("[null page]");
                rgrs.RightRows[i].SetText("[null page]");
            }
            rgrs.BackgroundImage.enabled = false;
        }
    }
}