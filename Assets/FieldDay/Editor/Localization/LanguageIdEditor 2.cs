using BeauUtil.Editor;
using FieldDay.Localization;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CustomPropertyDrawer(typeof(LanguageId)), CanEditMultipleObjects]
    public sealed class LanguageIdEditor : PropertyDrawer {
        static private NamedItemList<ushort> s_LanguageIds;

        static private void InitializeLanguageIdList() {
            if (s_LanguageIds != null) {
                return;
            }

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            s_LanguageIds = new NamedItemList<ushort>(cultures.Length);

            foreach(var culture in cultures) {
                string twoLetterISOName = culture.TwoLetterISOLanguageName;
                if (twoLetterISOName.Length > 2 || culture.ThreeLetterISOLanguageName.ToLowerInvariant() == "ivl") {
                    continue;
                }

                string name = culture.DisplayName;
                name = string.Format("{0}/{1} [{2}]", name[0], name, twoLetterISOName);
                s_LanguageIds.Add(CalculateValue(twoLetterISOName), name);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            InitializeLanguageIdList();

            label = EditorGUI.BeginProperty(position, label, property);

            property.NextVisible(true);

            ushort val = property.hasMultipleDifferentValues ? ushort.MaxValue : (ushort)property.intValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            ushort newVal = (ushort)ListGUI.Popup(position, label, val, s_LanguageIds);
            if (EditorGUI.EndChangeCheck()) {
                property.intValue = newVal;
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.EndProperty();
        }

        static private ushort CalculateValue(string twoLetterCode) {
            return new LanguageId(twoLetterCode).Value;
        }
    }
}