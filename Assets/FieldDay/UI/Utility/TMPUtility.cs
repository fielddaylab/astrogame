using System;
using System.Text;
using TinyIL;
using TMPro;
using UnityEngine;

namespace FieldDay.UI {
    static public class TMPUtility {
        static public bool SetTextAndActive(this TMP_Text tmp, string text) {
            if (string.IsNullOrEmpty(text)) {
                tmp.gameObject.SetActive(false);
                return false;
            }

            tmp.gameObject.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        static public bool SetTextAndActive(this TMP_Text tmp, StringBuilder text) {
            if (text == null || text.Length == 0) {
                tmp.gameObject.SetActive(false);
                return false;
            }

            tmp.gameObject.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        static public bool SetTextAndActive(this TMP_Text tmp, string text, GameObject group) {
            if (string.IsNullOrEmpty(text)) {
                group.SetActive(false);
                return false;
            }

            group.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        static public bool SetTextAndActive(this TMP_Text tmp, StringBuilder text, GameObject group) {
            if (text == null || text.Length == 0) {
                group.SetActive(false);
                return false;
            }

            group.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        static public bool SetTextAndActive(this TMP_Text tmp, string text, Component group) {
            if (string.IsNullOrEmpty(text)) {
                group.gameObject.SetActive(false);
                return false;
            }

            group.gameObject.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        static public bool SetTextAndActive(this TMP_Text tmp, StringBuilder text, Component group) {
            if (text == null || text.Length == 0) {
                group.gameObject.SetActive(false);
                return false;
            }

            group.gameObject.SetActive(true);
            tmp.SetText(text);
            return true;
        }

        [IntrinsicIL("ldarg.0; ldfld [arg tmp]::m_characterCount; ret;")]
        static public int CharacterCount(this TMP_Text tmp) {
            throw new NotImplementedException();
        }
    }
}