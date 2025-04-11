

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
    }


    public static partial class ReferenceUtility {
        public static void PopulateContents(RefGuideContents contents, ReferencePageAsset page) {
            if (page == null) {
                ClearContents(contents);
                return;
            }

            contents.Background.Path = page.BackgroundImagePath;
        }

        private static void ClearContents(RefGuideContents contents) {
            contents.Background.Path = null;
        }
    }
}