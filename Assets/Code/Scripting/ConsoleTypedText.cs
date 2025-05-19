using System.Collections;
using System.Text;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using TMPro;
using UnityEngine;

namespace Astro {
    public sealed class ConsoleTypedText : ScriptActorComponent {
        public CanvasGroup Group;
        public TMP_Text Text;

        private Routine m_Routine;
        private StringBuilder m_TextBuilder = new StringBuilder(512);

        public override void OnScriptRegister(ScriptActor actor) {
            base.OnScriptRegister(actor);
            Group.alpha = 0;
            Text.enabled = false;
        }

        [LeafMember("HideConsole")]
        public void Hide() {
            m_Routine.Replace(this, HideRoutine());
        }

        [LeafMember("PlayPuzzleConsoleText")]
        public IEnumerator Play() {
            return PlayAsset(DayConfigUtil.GetConfigForState().DayPuzzlePrelude);
        }

        public IEnumerator PlayAsset(ConsoleTextAsset textAsset) {
            m_Routine.Replace(this, AssetRoutine(textAsset));
            return m_Routine.Wait();
        }

        private IEnumerator HideRoutine() {
            yield return Group.FadeTo(0, 0.3f);
            Text.enabled = false;
        }

        private IEnumerator AssetRoutine(ConsoleTextAsset textAsset) {
            Text.SetText(string.Empty);
            Text.enabled = true;
            Group.alpha = 1;
            m_TextBuilder.Clear();

            ScriptActor keyboardActor = ScriptUtility.FindActor("KeyboardSfxLocation");

            foreach(var line in textAsset.Lines) {
                m_TextBuilder.Append(line.Text).Append('\n');
            }
            m_TextBuilder.TrimEnd(StringUtils.DefaultNewLineChars);

            Text.SetText(m_TextBuilder);
            Text.maxVisibleCharacters = 0;

            ViewState viewState = Find.State<ViewState>();
            while (viewState.ActiveTransitionRoutine.Exists()) {
                yield return 0.1f;
            }

            foreach(var line in textAsset.Lines) {
                int charIdx = 0;
                int charCount = line.Text.Length;
                    
                while(charIdx < charCount) {
                    char c = line.Text[charIdx++];
                    Text.maxVisibleCharacters++;

                    if (charIdx > 2 && line.IsKeyboard) {
                        Sfx.PlayDetached("Oneshot.Keyboard.Type", keyboardActor.transform);
                        // TODO: play keyboard clicky clacky sound
                        yield return 0.04f;
                    } else {
                        yield return 0.02f;
                    }
                }

                yield return line.DelayAfter;
                
                Text.maxVisibleCharacters++;
            }

            Hide();
        }
    }
}