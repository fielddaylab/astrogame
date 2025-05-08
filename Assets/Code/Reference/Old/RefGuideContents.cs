

using System;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astro.Reference {
    public class RefGuideContents : MonoBehaviour {
        public StreamingQuadTexture Background;
        public Transform RealtimeRoot;

        [NonSerialized] public Transform RealtimeChildEnabled;
    }


    public static partial class ReferenceUtility {
        public static void PopulateContents(RefGuideContents contents, ReferencePageAsset page) {
            if (page == null) {
                ClearContents(contents);
                return;
            }

            contents.Background.Path = page.BackgroundImagePath;
            if (contents.RealtimeChildEnabled) {
                contents.RealtimeChildEnabled.gameObject.SetActive(false);
                contents.RealtimeChildEnabled = null;
            }

            if (string.IsNullOrEmpty(page.BackgroundImagePath)) {
                contents.Background.Unload();

                Transform realtime = contents.RealtimeRoot.Find(page.name);
                if (realtime) {
                    contents.RealtimeChildEnabled = realtime;
                    contents.RealtimeChildEnabled.gameObject.SetActive(true);
                }
            }
        }

        private static void ClearContents(RefGuideContents contents) {
            contents.Background.Path = null;

            if (contents.RealtimeChildEnabled) {
                contents.RealtimeChildEnabled.gameObject.SetActive(false);
                contents.RealtimeChildEnabled = null;
            }
        }
    }
}