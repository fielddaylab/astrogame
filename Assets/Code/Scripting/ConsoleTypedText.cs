using System.Collections;
using System.Text;
using BeauPools;
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

        [LeafMember("PlayConsoleText")]
        public IEnumerator Play(StringHash32 textAsset) {
            ConsoleTextAsset asset = Find.NamedAsset<ConsoleTextAsset>(textAsset); 
            return PlayAsset(asset);
        }

        [LeafMember("PlayConsoleTextAndWait")]
        public IEnumerator LeafPlayAndWait(StringHash32 textAsset) {
            ConsoleTextAsset asset = Find.NamedAsset<ConsoleTextAsset>(textAsset); 
            return PlayAssetAndWait(asset);
        }

        public IEnumerator PlayClassification(ReferenceClassification rc) {
            string text;
            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.Append("> SUBMIT \"");
                psb.Builder.Append(rc.Label).Append("\"");
                text = psb.Builder.ToString();
            }
            return PlayString(text, true, 0.25f);
        }

        public IEnumerator PlayMaterials(SpectrographMaterialMask mask) {
            string text;
            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.Append("> SUBMIT \"");
                psb.Builder.Append(SpectrographUtility.ToSymbolsString(mask)).Append("\"");
                text = psb.Builder.ToString();
            }
            return PlayString(text, true, 0.25f);
        }

        public IEnumerator PlayAsset(ConsoleTextAsset textAsset) {
            yield return m_Routine.Replace(this, AssetRoutine(textAsset));
            yield return m_Routine.Replace(this, HideRoutine());
        }

        public IEnumerator PlayAssetAndWait(ConsoleTextAsset textAsset) {
            yield return m_Routine.Replace(this, AssetRoutine(textAsset));
        }

        public IEnumerator PlayString(string text, bool isKeyboard, float delayAfter) {
            m_Routine.Replace(this, StringLineRoutine(text, isKeyboard, delayAfter));
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
            // Hide();
        }

        private IEnumerator StringLineRoutine(string line, bool isKeyboard, float delayAfter) {
            Text.SetText(string.Empty);
            Text.enabled = true;
            Group.alpha = 1;
            m_TextBuilder.Clear();
            m_TextBuilder.Append(line);
            ScriptActor keyboardActor = ScriptUtility.FindActor("KeyboardSfxLocation");

            Text.SetText(m_TextBuilder);
            Text.maxVisibleCharacters = 0;

            ViewState viewState = Find.State<ViewState>();
            while (viewState.ActiveTransitionRoutine.Exists()) {
                yield return 0.1f;
            }

            int charIdx = 0;
            int charCount = line.Length;

            while (charIdx < charCount) {
                char c = line[charIdx++];
                Text.maxVisibleCharacters++;

                if (charIdx > 2 && isKeyboard) {
                    Sfx.PlayDetached("Oneshot.Keyboard.Type", keyboardActor.transform);
                    // TODO: play keyboard clicky clacky sound
                    yield return 0.04f;
                } else {
                    yield return 0.02f;
                }
            }

            yield return delayAfter;

            Text.maxVisibleCharacters++;

            Hide();
        }
    }
}