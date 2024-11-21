using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using System.Collections;
using UnityEngine;

namespace Astro {
    public sealed class DocumentBoardState : SharedStateComponent, IRegistrationCallbacks {
        [HideInInspector] public bool EnableDocumentInteraction;
        [HideInInspector] public DocumentInteractable SelectedDocument;
        [HideInInspector] public Vector3 LastMousePos;


        [Range(0f, 1f)] public float DocumentHoverDistance;
        [Range(0f, 30f)] public float FollowSpeed;
        [Range(0, 200)]public int FollowRadius;
        public Transform DocumentParent;

        public AssetPack DocumentAssets;

        public Routine DocumentRoutine;

        public void OnDeregister() {
        }

        public void OnRegister() {
        }
    }

    public static partial class DocumentUtility {

        public static void SpawnDocument(DocumentAsset asset, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            DocumentRenderer spawned = GameObject.Instantiate(asset.Prefab, state.DocumentParent);
            spawned.Title.SetText(asset.TitleText);
            spawned.Body.SetText(asset.BodyText);
            spawned.transform.localPosition = Vector3.zero;
        }

        public static void SpawnDocument(StringHash32 id) {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id));
        }

        #region Enable/Disable
        public static void SetDocumentInteractionEnabled(bool enable, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            state.EnableDocumentInteraction = enable;
        }
        public static void EnableDocumentInteraction() {
            SetDocumentInteractionEnabled(true);
        }
        public static void DisableDocumentInteraction() {
            SetDocumentInteractionEnabled(false);
        }

        #endregion // Enable/Disable

        #region Selection
        public static void SelectDocument(DocumentInteractable doc, DocumentBoardState state = null) {
            if (state == null) {
                state = Find.State<DocumentBoardState>();
            }
            if (state.SelectedDocument != doc) {
                state.SelectedDocument = doc;
                state.DocumentRoutine.Replace(ShiftZ(doc.transform, -state.DocumentHoverDistance));
            } else {
                state.SelectedDocument = null;
                state.DocumentRoutine.Replace(ShiftZ(doc.transform, state.DocumentHoverDistance));
            }
        }

        private static IEnumerator ShiftZ(Transform doc, float deltaZ) {
            yield return doc.MoveTo(doc.position.z + deltaZ, 0.1f, Axis.Z).Ease(Curve.Smooth);
            yield return null;
        }

        #endregion //Selection
    }
}