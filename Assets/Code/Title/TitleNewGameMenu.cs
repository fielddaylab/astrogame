using System.Collections.Generic;
using BeauUtil;
using BeauUtil.UI;
using FieldDay.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace Astro.Title {
    public sealed class TitleNewGameMenu : MonoBehaviour, IScenePreload {
        public PointerListener BeginButton;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            BeginButton.onClick.AddListener(OnClickBegin);
            return null;
        }

        private void OnClickBegin() {
            // TODO: Implement
            ScriptTriggers.LoadDay("Day1");
        }
    }
}