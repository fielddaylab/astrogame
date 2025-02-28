using Astro;
using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SysUpdate(GameLoopPhase.Update, 100)] // After DocumentInteractionSystem
public class DocumentPuzzleInteractionSystem : SharedStateSystemBehaviour<DocumentBoardState, DocumentPuzzleState, ViewState>
{
    public override void ProcessWork(float deltaTime)
    {
        // TODO:
        // Create new type of document -- QuestionDocument

        if (!m_StateB.PuzzleActive) { return; }

        // Remove all current selections

        // Assign correct answer ID

        // On select, Trigger leaf VO

        // On Complete, unlock focus on board

    }
}