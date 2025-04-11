using FieldDay.Rendering;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class MeshTransformWizard : ScriptableWizard {
        public Mesh SourceMesh;

        [Header("Offset")]
        public Vector3 Translate = Vector3.zero;
        public Vector3 Scale = Vector3.one;
        public Quaternion Rotate = Quaternion.identity;

        private void OnWizardUpdate() {
            if (!SourceMesh) {
                isValid = false;
                errorString = "You must specify a source mesh";
            } else if (!SourceMesh.isReadable) {
                isValid = false;
                errorString = "Mesh is not readable";
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
                LastSaveLocationKey = PlayerSettings.productGUID + "/MeshTransformSavePath",
                Message = "Save this mesh"
            };

            Mesh clone = Instantiate(SourceMesh);
            try {
                MeshModUtility.TransformPositions(clone, Matrix4x4.TRS(Translate, Rotate, Scale));
                EditorHelpers.SaveResourceAs(clone, SourceMesh.name + "_trs", SaveForm);
            } finally {
                EditorHelpers.DestroyResource(ref clone);
            }
        }

        [MenuItem("Window/Field Day/Mesh Transform Wizard")]
        static private void CreateWizard() {
            DisplayWizard<MeshTransformWizard>("Transform Mesh", "Transform");
        }

        [MenuItem("CONTEXT/MeshFilter/Create Scaled Mesh")]
        static private void CreateWizardForMeshFilter(MenuCommand cmd) {
            MeshFilter filter = (MeshFilter) cmd.context;
            MeshTransformWizard wizard = DisplayWizard<MeshTransformWizard>("Transform Mesh", "Transform");
            wizard.SourceMesh = filter.sharedMesh;
            wizard.Scale = filter.transform.localScale;
            wizard.OnWizardUpdate();
        }

        [MenuItem("CONTEXT/Mesh/Create Scaled Mesh")]
        static private void CreateWizardForMesh(MenuCommand cmd) {
            Mesh mesh = (Mesh) cmd.context;
            MeshTransformWizard wizard = DisplayWizard<MeshTransformWizard>("Transform Mesh", "Transform");
            wizard.SourceMesh = mesh;
            wizard.OnWizardUpdate();
        }
    }
}