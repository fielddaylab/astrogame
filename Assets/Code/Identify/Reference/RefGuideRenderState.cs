

using System;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public class RefGuideRenderState : SharedStateComponent {
        public Camera RefRenderCam;

        public FlexPage Page;

        [Header("UI")]
        public TMP_Text LeftTitle;
        public TMP_Text[] LeftRows;
        public TMP_Text RightTitle;
        public TMP_Text[] RightRows;
        public Image BackgroundImage;


        [Header("Prefabs")]
        public GameObject TextCell;
        public GameObject ImageCell;
        public GameObject FlexRow;


        [NonSerialized] public bool RenderNeedsRefresh;
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

            rgrs.LeftTitle.SetText(page.TitleLeft);
            rgrs.RightTitle.SetText(page.TitleRight);
            for (int i = 0; i < rgrs.LeftRows.Length; i++) {
                if (i < page.EntriesLeft.Length) {
                    rgrs.LeftRows[i].SetText(page.EntriesLeft[i].Label);
                } else {
                    rgrs.LeftRows[i].SetText("");
                }
                if (i < page.EntriesRight.Length) {
                    rgrs.RightRows[i].SetText(page.EntriesRight[i].Label);
                } else {
                    rgrs.RightRows[i].SetText("");
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

        public static void PopulateFlexibleReferenceCanvas(FlexReferencePageAsset asset, RefGuideRenderState rgrs) {
            if (asset.Layout == PageLayout.None) {
                return;
            }

            FlexPage page = rgrs.Page;
            switch (asset.Layout) {
                case PageLayout.ImageOnly: 
                    {
                        page.Table.gameObject.SetActive(false);
                        page.BackgroundImage.gameObject.SetActive(true);
                        page.BackgroundImage.sprite = asset.Background;
                        page.Body.SetTextAndActive("");
                        page.Title.SetTextAndActive("");
                        break;
                    }
                case PageLayout.TitleBody: 
                    {
                        page.Table.gameObject.SetActive(false);
                        page.BackgroundImage.gameObject.SetActive(false);
                        page.Title.SetTextAndActive(asset.TextData.TitleText);
                        page.Body.SetTextAndActive(asset.TextData.BodyText);
                        break;
                    }
                case PageLayout.Table: {
                        page.Table.gameObject.SetActive(true);
                        page.BackgroundImage.gameObject.SetActive(false);
                        page.Title.SetTextAndActive("");
                        page.Body.SetTextAndActive("");
                        PopulateFlexTable(asset.TableData, page);
                        break;
                    }
            }


        }

        public static void PopulateFlexTable(TableData data, FlexPage page) {

        }
    }
}