using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using Leaf.Runtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public sealed class ChapterTransition : ScriptActorComponent {
        public Canvas Canvas;
        public Graphic Background;
        public TMP_Text Text;

        [Header("Tuning")]
        public float TextFadeInTime = 0.6f;
        public float TextDuration = 1;
        public float TextFadeOutTime = 0.6f;
        public float BackgroundFadeOutTime = 0.6f;
        public float FinalFadeOutTime = 0.6f;

        private bool m_PausedRaycasts;

        public override void OnScriptDeregister(ScriptActor actor) {
            base.OnScriptDeregister(actor);
            if (m_PausedRaycasts) {
                Game.Input.ResumeRaycasts();
            }
        }

        [LeafMember("PrepareTransition")]
        public void Prepare(bool hideBackground = false) {
            Canvas.enabled = true;
            Background.SetAlpha(hideBackground ? 0 : 1);
            Text.alpha = 0;
            Sfx.SetMixState("ChapterTransition", 1, 0);
            Game.Input.PauseRaycasts();
            m_PausedRaycasts = true;
        }

        [LeafMember("DisplayChapterTitles")]
        public IEnumerator TextCycle() {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            for(int i = 0; i < config.ChapterTitles.Length; i++) {
                Text.SetText(config.ChapterTitles[i]);
                Text.alpha = 0;
                yield return Text.FadeTo(1, TextFadeInTime);
                yield return TextDuration;
                if (i < config.ChapterTitles.Length - 1) {
                    yield return Text.FadeTo(0, TextFadeOutTime);
                }
            }
        }

        [LeafMember("DisplayIntroTitles")]
        public IEnumerator IntroTextCycle() {
            DayConfigAsset config = Find.NamedAsset<DayConfigAsset>("Day0(Prelude)");
            for(int i = 0; i < config.ChapterTitles.Length; i++) {
                Text.SetText(config.ChapterTitles[i]);
                Text.alpha = 0;
                yield return Text.FadeTo(1, TextFadeInTime);
                yield return TextDuration;
                if (i < config.ChapterTitles.Length - 1) {
                    yield return Text.FadeTo(0, TextFadeOutTime);
                }
            }
        }

        [LeafMember("FadeOutBackground")]
        public void FadeOutBackground() {
            Routine.Start(this, Background.FadeTo(0, BackgroundFadeOutTime));
            Sfx.SetMixState("ChapterTransition", 0, BackgroundFadeOutTime);
        }

        [LeafMember("FinishTransition")]
        public void Finish(float delay = 0) {
            if (m_PausedRaycasts) Game.Input.ResumeRaycasts();
            m_PausedRaycasts = false;
            Routine.Start(this, Text.FadeTo(0, FinalFadeOutTime).OnComplete(() => {
                Canvas.enabled = false;
            }).DelayBy(delay));
        }
    }
}