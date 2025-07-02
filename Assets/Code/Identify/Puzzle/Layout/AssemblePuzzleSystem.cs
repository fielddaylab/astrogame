using UnityEngine;
using BeauUtil;
using BeauPools;
using FieldDay;
using FieldDay.UI;
using FieldDay.Systems;
using System.Text;
using System.Collections.Generic;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.OpenSubmissionUpdateMask)]
    public class AssemblePuzzleSystem : ComponentSystemBehaviour<PuzzleDisplay> {
        private readonly RingBuffer<PuzzleCell> m_CellWorkList = new RingBuffer<PuzzleCell>(12, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime) {
            // Only assemble the puzzle if new one is queued or TODO reset clicked
            var state = Find.State<PuzzleState>();
            if (state.QueuedPuzzle == null) { return; }

            var pools = Find.State<PuzzlePools>();
            PuzzleUtility.ExtractCols(state.QueuedPuzzle, out List<DataTypeMask> types, out int numCols);

            m_CellWorkList.Clear();

            foreach (var display in m_Components) {
                // generate header labels
                PuzzleHeader[] headers = new PuzzleHeader[numCols];
                for (int c = 0; c < numCols; c++) {
                    var newHeader = pools.Headers.Alloc(display.transform.position);
                    newHeader.Text.SetText(GenerateHeaderText(types[c]));
                    headers[c] = newHeader;
                }

                // TEMPORARY, TODO: streamline this
                using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    for (int i = 0; i < state.QueuedPuzzle.ClueText.Length; i++) {
                        psb.Builder.Append("<sprite name=\"hint-bullet\">");
                        psb.Builder.Append(state.QueuedPuzzle.ClueText[i]);
                        psb.Builder.Append("\n");
                    }
                    display.Clues.Text.SetTextAndActive(psb.Builder);
                }

                // generate and organize puzzle cells
                for (int r = 0; r < state.QueuedPuzzle.Rows.Length; r++) {
                    for (int c = 0; c < numCols; c++) {

                        PuzzleCell newCell = null;
                        if ((types[c] & DataTypeMask.Color) != 0) {
                            newCell = pools.ColorCells.Alloc(display.transform.position);
                        }
                        else {
                            newCell = pools.Cells.Alloc(display.transform.position);
                        }
                        newCell.DataSlot.Type = types[c];
                        if ((state.QueuedPuzzle.Rows[r].ProvidedProperties & types[c]) != 0) {
                            // load provided data directly
                            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(state.QueuedPuzzle.Rows[r].Object);
                            var newPacket = GenerateProvidedPacket(types[c], asset);
                            DataUtility.TrySetData(newCell.DataSlot, newPacket);
                            // provided data is not modifiable
                            newCell.DataSlot.Modifiable = false;
                        }
                        // assign unique id for each slot
                        StringBuilder sb = new StringBuilder();
                        sb.Append('R');
                        sb.Append(r.ToStringLookup());
                        newCell.DataSlot.PuzzleRow = r;
                        sb.Append('C');
                        sb.Append(c.ToStringLookup());
                        newCell.DataSlot.PuzzleCol = c;
                        newCell.DataSlot.SlotId = sb.ToString();

                        m_CellWorkList.PushBack(newCell);
                    }
                }

                PuzzleUtility.LoadCells(display, m_CellWorkList, headers, numCols);
                PuzzleUtility.LayoutCells(display, state, pools, types);
            }

            state.SelectedCells = new bool[state.QueuedPuzzle.Rows.Length, numCols];
            // select first row by default
            for (int i = 0; i < numCols; i++) {
                state.SelectedCells[0, i] = true;
            }
            state.CellsUpdated = true;

            Debug.Log("[AssemblePuzzle] New puzzle cols: " + numCols);

            state.ActivePuzzle = state.QueuedPuzzle;
            state.QueuedPuzzle = null;
            m_CellWorkList.Clear();
        }

        /// <summary>
        /// TODO: merge this with AssetPacketConversionSystem
        /// </summary>
        private DataPacket GenerateProvidedPacket(DataTypeMask type, CelestialAsset asset) {
            if ((type & DataTypeMask.Name) != 0) {
                return DataPacket.Name(asset);
            }
            if ((type & DataTypeMask.Coordinates) != 0) {
                return DataPacket.Coordinates(asset.Coords);
            }
            if ((type & DataTypeMask.ApparentMagnitude) != 0) {
                return DataPacket.ApparentMagnitude(asset.ApparentMagnitude);
            }
            if ((type & DataTypeMask.AbsoluteMagnitude) != 0) {
                return DataPacket.AbsoluteMagnitude(asset.AbsoluteMagnitude);
            }
            if ((type & DataTypeMask.MaterialSpectrum) != 0) {
                return DataPacket.Spectrograph(asset.Spectrograph);
            }
            if ((type & DataTypeMask.Temperature) != 0) {
                return DataPacket.Temperature(asset.Temperature);
            }
            if ((type & DataTypeMask.Distance) != 0) {
                return DataPacket.Distance(asset.Distance);
            }

            return new DataPacket();
        }

        private string GenerateHeaderText(DataTypeMask type) {
            if ((type & DataTypeMask.Name) != 0) {
                return DataTypeLabels.Name;
            }
            if ((type & DataTypeMask.Coordinates) != 0) {
                return DataTypeLabels.Coordinates;
            }
            if ((type & DataTypeMask.Color) != 0) {
                return DataTypeLabels.Color;
            }
            if ((type & DataTypeMask.ApparentMagnitude) != 0) {
                return DataTypeLabels.ApparentMagnitude;
            }
            if ((type & DataTypeMask.AbsoluteMagnitude) != 0) {
                return DataTypeLabels.AbsoluteMagnitude;
            }
            if ((type & DataTypeMask.MaterialSpectrum) != 0) {
                return DataTypeLabels.MaterialSpectrum;
            }
            if ((type & DataTypeMask.Temperature) != 0) {
                return DataTypeLabels.Temperature;
            }
            if ((type & DataTypeMask.Distance) != 0) {
                return DataTypeLabels.Distance;
            }
            if ((type & DataTypeMask.Historical_Coordinates) != 0) {
                return DataTypeLabels.HistoricalCoordinates;
            }
            if ((type & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                return DataTypeLabels.HistoricalApparentMagnitude;
            }
            if ((type & DataTypeMask.BlueMagnitude) != 0) {
                return DataTypeLabels.BlueMagnitude;
            }
            if ((type & DataTypeMask.InfraredMagnitude) != 0) {
                return DataTypeLabels.InfraredMagnitude;
            }
            if ((type & DataTypeMask.Historical_Color) != 0) {
                return DataTypeLabels.HistoricalColor;
            }

            return string.Empty;
        }
    }
}