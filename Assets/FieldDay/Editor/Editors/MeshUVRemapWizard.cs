using FieldDay.Rendering;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class MeshUVRemapWizard : ScriptableWizard {
        public Mesh SourceMesh;

        [Header("Offset")]
        [Range(0, 7)] public int Channel = 0;
        public Rect OriginalRange = new Rect(0, 0, 1, 1);
        public Rect NewRange = new Rect(0, 0, 1, 1);

        private void OnWizardUpdate() {
            if (!SourceMesh) {
                isValid = false;
                errorString = "You must specify a source mesh";
            } else if (!SourceMesh.isReadable) {
                isValid = false;
                errorString = "Mesh is not readable";
            } else if (Channel < 0 || Channel > 7 || !SourceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + Channel)) {
                isValid = false;
                errorString = "Mesh does not have uv coordinates at the given channel";
            } else {
                isValid = true;
                errorString = string.Empty;
            }
        }

        private void OnWizardCreate() {
            if (!SourceMesh) {
                return;
            }

            EditorHelpers.ResourceSaveForm SaveForm = new EditorHelpers.ResourceSaveForm() {
                FileExtension = "mesh",
                Header = "Save Modified Mesh",
                LastSaveLocationKey = PlayerSettings.productGUID + "/MeshUVRemapSavePath",
                Message = "Save this mesh"
            };

            Mesh clone = Instantiate(SourceMesh);
            try {
                MeshModUtility.RemapUVs(clone, Channel, OriginalRange, NewRange);
                EditorHelpers.SaveResourceAs(clone, SourceMesh.name + "_uvremap", SaveForm);
            } finally {
                EditorHelpers.DestroyResource(ref clone);
            }
        }

        [MenuItem("Window/Field Day/Mesh UV Remap Wizard")]
        static private void CreateWizard() {
            DisplayWizard<MeshUVRemapWizard>("UV Remap Mesh", "Remap");
        }

        [MenuItem("CONTEXT/MeshFilter/Create UV Remapped Mesh")]
        static private void CreateWizardForMeshFilter(MenuCommand cmd) {
            MeshFilter filter = (MeshFilter) cmd.context;
            MeshUVRemapWizard wizard = DisplayWizard<MeshUVRemapWizard>("UV Remap Mesh", "Remap");
            wizard.SourceMesh = filter.sharedMesh;
            wizard.OnWizardUpdate();
        }

        [MenuItem("CONTEXT/Mesh/Create UV Remapped Mesh")]
        static private void CreateWizardForMesh(MenuCommand cmd) {
            Mesh mesh = (Mesh) cmd.context;
            MeshUVRemapWizard wizard = DisplayWizard<MeshUVRemapWizard>("UV Remap Mesh", "Remap");
            wizard.SourceMesh = mesh;
            wizard.OnWizardUpdate();
        }
    }
}