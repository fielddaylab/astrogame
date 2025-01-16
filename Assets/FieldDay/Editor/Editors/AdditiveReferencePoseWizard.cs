using System;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class AdditiveReferencePoseWizard : ScriptableWizard {
        public AnimationClip Reference;
        public AnimationClip[] Targets = Array.Empty<AnimationClip>();

        private void OnWizardUpdate() {
            if (!Reference) {
                helpString = "Please provide a source";
                isValid = false;
            } else if (Targets.Length == 0) {
                helpString = "Please provide a target";
                isValid = false;
            } else {
                helpString = string.Empty;
                isValid = true;
            }
        }

        private void OnWizardCreate() {
            if (Reference && Targets.Length > 0) {
                foreach(var obj in Targets) {
                    Undo.RecordObject(obj, "Setting reference pose");
                    AnimationUtility.SetAdditiveReferencePose(obj, Reference, 0);
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        [MenuItem("Window/Field Day/Additive Reference Pose Wizard")]
        static private void CreateWizard() {
            DisplayWizard<AdditiveReferencePoseWizard>("Addditive Reference Poses", "Apply");
        }
    }
}