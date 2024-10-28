using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    public class AssemblePuzzleSystem : ComponentSystemBehaviour<PuzzleDisplay>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Only assemble the puzzle if new one is queued or TODO reset clicked
            var state = Find.State<PuzzleState>();
            if (state.QueuedPuzzle == null) { return; }

            foreach (var display in m_Components)
            {

            }

            state.ActivePuzzle = state.QueuedPuzzle;
            state.QueuedPuzzle = null;
        }
    }
}