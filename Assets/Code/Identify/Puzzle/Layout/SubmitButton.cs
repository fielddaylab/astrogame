
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(LabButton))]
    public class SubmitButton : BatchedComponent, IRegistrationCallbacks {
        public SubmitButtonType ButtonType;
        public GameObject Root;
        public MeshRenderer ButtonMesh;
        public TMP_Text ButtonLabel;

        [Header("Button Materials")]
        public Material InactiveButtonMaterial;
        public Material ActiveButtonMaterial;

        [Header("Loading Colliders")]
        public GameObject MonitorLoadingCollider;
        public GameObject PuzzleLoadingCollider;

        private LabButton labButton;

        private Action m_SetButtonToPuzzle;  
        private Action m_SetButtonToId;

        private void Awake() {
            labButton = GetComponent<LabButton>();
        }

        public void OnRegister() {
            m_SetButtonToPuzzle = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitPuzzle);
            m_SetButtonToId = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitIdentification);

            Game.Events.Register(GameEvents.StartPuzzleMode, m_SetButtonToPuzzle);
            Game.Events.Register(GameEvents.StartFinalPuzzle, m_SetButtonToPuzzle);
            Game.Events.Register(GameEvents.StartOpenMode, m_SetButtonToId);
        }

        public void OnDeregister() {
            Game.Events?.Deregister(GameEvents.StartPuzzleMode, m_SetButtonToPuzzle);
            Game.Events?.Deregister(GameEvents.StartFinalPuzzle, m_SetButtonToPuzzle);
            Game.Events?.Deregister(GameEvents.StartOpenMode, m_SetButtonToId);
        }

        public IEnumerator SetButtonActive(bool active) {
            GetComponent<BoxCollider>().enabled = active;

            if (active) {
                ButtonMesh.sharedMaterial = ActiveButtonMaterial;
                yield return labButton.Movable.MoveTo(labButton.OriginalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.BackOut);
            } else {
                ButtonMesh.sharedMaterial = InactiveButtonMaterial;
                labButton.Movable.position = labButton.OriginalDisplacement + labButton.LocalDisplacement;
            }
            
        }
    }

    [Serializable, Flags]
    public enum SubmitButtonType {
        SubmitPuzzle = 1,
        SubmitIdentification = 2
    }
}