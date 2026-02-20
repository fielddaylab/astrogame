
using BeauPools;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.HID;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public sealed class PuzzleState : SharedStateComponent, IRegistrationCallbacks {
        public PuzzleAsset QueuedPuzzle;
        public PuzzleAsset ActivePuzzle;

        [NonSerialized] public PuzzleDisplay Display;
        public bool CellsUpdated = false;

        // Temp flag for grouped vs individual cell implementation
        public bool GroupCellsByRow = false;

        // CELL GROUPED BY ROW IMPLEMENTATION
        public int SelectedRow = -1;

        // INDIVIDUAL CELL IMPLEMENTATION
        public bool[,] SelectedCells;
        public DataTypeMask RelevantColFilter;

        // TUTORIAL ISOLATION
        public bool IsIsolated;
        public RingBuffer<StringHash32> IsolatedSlots = new RingBuffer<StringHash32>(2, RingBufferMode.Expand);

        public PuzzleCellLibrary Library;

        [Header("Consts")]
        public Material UnselectedCellMat;
        public Material SelectedCellMat;

        // Tracking stars that have guess trackers on them
        public UIFocus[] PuzzleEntryGuesses = new UIFocus[4] { null, null, null, null };

        public Routine PuzzleCorrectSubmissionRoutine = new Routine();

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartPuzzleMode, PuzzleUtility.ActivatePuzzlePanel);
            Game.Events.Register(GameEvents.StartFinalPuzzle, PuzzleUtility.ActivateFinalPuzzle);
            Game.Events.Register(GameEvents.StopPuzzleMode, PuzzleUtility.DeactivatePuzzlePanel);

            Game.Scenes.QueueOnEnable(() => {
                DayConfigAsset config = DayConfigUtil.GetConfigForState();
                QueuedPuzzle = config.DayPuzzle;
            });
        }

        public void OnDeregister() {
            Game.Events?.Deregister(GameEvents.StartPuzzleMode, PuzzleUtility.DeactivatePuzzlePanel);
            Game.Events?.Deregister(GameEvents.StartFinalPuzzle, PuzzleUtility.ActivateFinalPuzzle);
            Game.Events?.Deregister(GameEvents.StopPuzzleMode, PuzzleUtility.ActivatePuzzlePanel);
        }

        private void Awake() {
            Display = FindFirstObjectByType<PuzzleDisplay>();
        }
    }

    public static partial class PuzzleUtility {
        public static bool TrySetSelectedRow(PuzzleState state, int index)
        {
            if (state.ActivePuzzle == null) { return false; }
            if (index < 0) { index = state.ActivePuzzle.Rows.Length - 1; }

            index = index % state.ActivePuzzle.Rows.Length;
            state.SelectedRow = index;
            state.CellsUpdated = true;

            return true;
        }

        public static bool TrySetSelectedCellInCol(PuzzleState state, int colIndex, int rowDir)
        {
            if (state.ActivePuzzle == null) { return false; }

            int rowIndex = -1;
            for (int r = 0; r < state.SelectedCells.GetLength(0); r++) {
                if (state.SelectedCells[r, colIndex]) {
                    rowIndex = r;
                    break;
                }
            }
            rowIndex += rowDir;

            if (rowIndex < 0) { rowIndex = state.ActivePuzzle.Rows.Length - 1; }

            rowIndex = rowIndex % state.ActivePuzzle.Rows.Length;
            for (int i = 0; i < state.ActivePuzzle.Rows.Length; i++) {
                state.SelectedCells[i, colIndex] = i == rowIndex;
            }
            state.CellsUpdated = true;

            return true;
        }

        public static void DeactivatePuzzlePanel() {
            PuzzleDisplay display = Find.State<PuzzleState>().Display;

            if (display == null) { return; }

            foreach (RectTransform child in display.PuzzleOverrideDisplays.GetComponentsInChildren<RectTransform>()) {
                child.gameObject.SetActive(false);
            }

            // Flip the puzzle panel
            Sfx.PlayDetached("Oneshot.LabButtonC.Click", display.RotateModulePos);
            display.FlipRoutine.Replace(display, display.RotateModulePos.RotateTo(-180f, 0.35f, Axis.Z, Space.Self).Ease(Curve.CubeInOut).ForceOnCancel());

            display.ClueGroup.gameObject.SetActive(false);
            display.CellAnchorPos.gameObject.SetActive(false);
            display.HeaderAnchorPos.gameObject.SetActive(false);
        }

        public static void ActivatePuzzlePanel() {
            PuzzleDisplay display = Find.State<PuzzleState>().Display;

            InputUtility.SetInputEnabled(Find.State<InputState>(), false);
            display.OverrideRoutine.Replace(PuzzleTransitionRoutine(display));
        }

        public static void ActivateFinalPuzzle() {
            PuzzleDisplay display = Find.State<PuzzleState>().Display;
            InputUtility.SetInputEnabled(Find.State<InputState>(), false);
            display.OverrideRoutine.Replace(FinalPuzzleTransitionRoutine(display));
        }

        public static IEnumerator PuzzleTransitionRoutine(PuzzleDisplay display) {
            MonitorUIMgr monitorUI = MonitorUIMgr.Instance;
            yield return monitorUI.ShowElement("OffPanel");
            yield return monitorUI.ShowElement("PuzzleMsg1");
            yield return new WaitForSeconds(0.2f);
            yield return monitorUI.ShowElement("PuzzleMsg2");


            MonitorUIElement off = monitorUI.GetElement("OffPanel");

            yield return off.GetComponent<Graphic>().ColorTo(new Color(1f, 0.745f, 0.24f), 0.2f);

            foreach (RectTransform child in display.PuzzleOverrideDisplays.GetComponentsInChildren<RectTransform>(true)) {
                yield return new WaitForSeconds(0.2f);
                Sfx.PlayDetached("Oneshot.LabButtonC.Click", display.PuzzleOverrideDisplays.transform);
                child.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(2f);

            foreach (RectTransform child in display.PuzzleOverrideDisplays.GetComponentsInChildren<RectTransform>(true)) {
                child.gameObject.SetActive(false);
            }

            off.GetComponent<Graphic>().color = Color.black;
            yield return monitorUI.HideElement("PuzzleMsg2");
            yield return monitorUI.HideElement("PuzzleMsg1");
            yield return monitorUI.HideElement("OffPanel");

            GeneratePreExsistingTrackers();

            // Flip the puzzle panel
            Sfx.PlayDetached("Oneshot.LabButtonC.Click", display.RotateModulePos);
            display.FlipRoutine.Replace(display, display.RotateModulePos.RotateTo(0f, 0.35f, Axis.Z, Space.Self).Ease(Curve.CubeInOut).ForceOnCancel());

            InputUtility.SetInputEnabled(Find.State<InputState>(), true);
            display.CellAnchorPos.gameObject.SetActive(true);
            display.HeaderAnchorPos.gameObject.SetActive(true);
            display.ClueGroup.gameObject.SetActive(true);
        }        

        public static void GeneratePreExsistingTrackers() {
            PuzzleState state = Find.State<PuzzleState>();
            ExtractCols(state.ActivePuzzle, out List<DataTypeMask> types, out int numCols);

            for (int r = 0; r < state.ActivePuzzle.Rows.Length; r++) {
                for (int c = 0; c < numCols; c++) {
                    // check for pre existing data directly
                    if ((state.ActivePuzzle.Rows[r].ProvidedProperties & types[c]) == 0) continue;

                    StringHash32 assetId = state.ActivePuzzle.Rows[r].Object; 
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(assetId);
                    UIFocus focus = FocusableUtility.GetFocusByData(assetId);

                    if (c != 1) continue; // collumn for coordinates, which control trackers 
                    FocusableUtility.UpdateFocusTrackerSprite(focus, FocusState.GuessTrackerSprites[r]);
                    state.PuzzleEntryGuesses[r] = focus;     
                }
            }            
        }

        public static IEnumerator FinalPuzzleTransitionRoutine(PuzzleDisplay display) {
            MonitorUIMgr monitorUI = MonitorUIMgr.Instance;
            yield return monitorUI.ShowElement("OffPanel");
            yield return monitorUI.ShowElement("PuzzleMsg1");
            yield return new WaitForSeconds(0.2f);
            yield return monitorUI.ShowElement("PuzzleMsg2");

            MonitorUIElement off = monitorUI.GetElement("OffPanel");

            yield return off.GetComponent<Graphic>().ColorTo(new Color(1f, 0.745f, 0.24f), 0.2f);

            foreach (RectTransform child in display.PuzzleOverrideDisplays.GetComponentsInChildren<RectTransform>(true)) {
                yield return new WaitForSeconds(0.2f);
                Sfx.PlayDetached("Oneshot.LabButtonC.Click", display.PuzzleOverrideDisplays.transform);
                child.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(2f);

            foreach (RectTransform child in display.PuzzleOverrideDisplays.GetComponentsInChildren<RectTransform>(true)) {
                child.gameObject.SetActive(false);
            }

            off.GetComponent<Graphic>().color = Color.black;
            yield return monitorUI.HideElement("PuzzleMsg2");
            yield return monitorUI.HideElement("PuzzleMsg1");
            // yield return monitorUI.HideElement("OffPanel");

            InputUtility.SetInputEnabled(Find.State<InputState>(), true);
            display.CellAnchorPos.gameObject.SetActive(true);
            display.HeaderAnchorPos.gameObject.SetActive(true);
            display.ClueGroup.gameObject.SetActive(true);
        }        

        public static bool IsSlotIsolated(PuzzleState state, StringHash32 slotId) {
            foreach (var isolated in state.IsolatedSlots) {
                if (isolated.Equals(slotId)) {
                    return true;
                }
            }

            return false;
        }

        [LeafMember("IsolatePuzzleCell")]
        public static void LeafIsolatePuzzleCell(int row, int col)
        {
            PuzzleState state = Find.State<PuzzleState>();

            StringBuilder sb = new StringBuilder();
            sb.Append('R');
            sb.Append(row.ToStringLookup());
            sb.Append('C');
            sb.Append(col.ToStringLookup());
            StringHash32 slotId = sb.ToString();

            state.IsolatedSlots.PushBack(slotId);
            state.IsIsolated = true;
        }

        [LeafMember("AddIsolatedSlot")]
        public static void LeafAddIsolatedSlot(StringHash32 slotId) {
            PuzzleState state = Find.State<PuzzleState>();

            state.IsolatedSlots.PushBack(slotId);
            state.IsIsolated = true;
        }

        [LeafMember("RemoveIsolatedSlot")]
        public static void LeafRemoveIsolatedSlot(StringHash32 slotId)
        {
            PuzzleState state = Find.State<PuzzleState>();

            if (state.IsolatedSlots.Contains(slotId)) {
                state.IsolatedSlots.Remove(slotId);

                if (state.IsolatedSlots.Count == 0) {
                    state.IsIsolated = false;
                }
            }
        }

        [LeafMember("ReleaseIsolatedPuzzle")]
        public static void LeafReleaseIsolatePuzzle()
        {
            PuzzleState state = Find.State<PuzzleState>();
            state.IsIsolated = false;
            state.IsolatedSlots.Clear();
        }
    }
}