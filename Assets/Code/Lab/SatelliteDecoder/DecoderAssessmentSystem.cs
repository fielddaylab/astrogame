using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 100, AstroGame.InteractUpdateMask)] // after DecoderDialInteractionSystem
    public class DecoderAssessmentSystem : SharedStateSystemBehaviour<SatelliteDecoderState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_State.InputUpdatedThisFrame;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            if (DecoderUtility.AssessSequence(m_State.Dials, m_State.Solution)) {
                Debug.Log("[DecoderAssessmentSystem] Decode successful!");
            }
            else {
                Debug.Log("[DecoderAssessmentSystem] Decode unsuccessful.");
            }
        }
    }

    public static partial class DecoderUtility
    {
        public static bool AssessSequence(SatelliteDecoderDial[] dials, string solution)
        {
            Assert.True(solution.Length == dials.Length);

            char dialChar, solutionChar;
            // check each decoder value in turn to see if it matches the solution
            for (int i = 0; i < solution.Length; i++) {
                dialChar = char.ToUpper(dials[i].Values[dials[i].CurrValIndex]);
                solutionChar = char.ToUpper(solution[i]);

                if (!dialChar.Equals(solutionChar)) {
                    return false;
                }
            }

            return true;
        } 
    }
}