using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class SkinnedMeshBoneTransferWizard : ScriptableWizard {
        public SkinnedMeshRenderer Source;
        public SkinnedMeshRenderer Target;

        private void OnWizardUpdate() {
            if (!Source) {
                helpString = "Please provide a source";
                isValid = false;
            } else if (!Target) {
                helpString = "Please provide a target";
                isValid = false;
            } else if (Source == Target) {
                helpString = "Source and target must not be the same object";
                isValid = false;
            } else {
                helpString = string.Empty;
                isValid = true;
            }
        }

        private void OnWizardCreate() {
            if (Source && Target && Source != Target) {
                Undo.RecordObject(Target, "Copying bones");
                Target.bones = Source.bones;
                Target.rootBone = Source.rootBone;
                EditorUtility.SetDirty(Target);
            }
        }

        [MenuItem("Window/Field Day/Skinned Mesh Bone Transfer")]
        static private void CreateWizard() {
            DisplayWizard<SkinnedMeshBoneTransferWizard>("Transfer Bones", "Transfer");
        }
    }
}