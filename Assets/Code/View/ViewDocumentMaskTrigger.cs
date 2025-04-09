using System;
using Astro;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro
{
    [RequireComponent(typeof(ViewNode))]
    public class ViewDocumentMaskTrigger : BatchedComponent, IRegistrationCallbacks
    {
        public void OnDeregister()
        {

        }

        public void OnRegister()
        {
            GetComponent<ViewNode>().OnEnter.Register(OnEnterAction);
            GetComponent<ViewNode>().OnExit.Register(OnExitAction);
        }

        private void OnEnterAction(ViewNode node)
        {
            UpdateMaskTriggerUtility.ResumeUpdateMask(AstroGame.DocumentUpdateMask);
        }

        private void OnExitAction(ViewNode node)
        {
            UpdateMaskTriggerUtility.SuspendUpdateMask(AstroGame.DocumentUpdateMask);
        }
    }

    public static partial class UpdateMaskTriggerUtility
    {
        public static void ResumeUpdateMask(int updateMask)
        {
            GameLoop.ResumeUpdates(updateMask);
        }

        public static void SuspendUpdateMask(int updateMask)
        {
            GameLoop.SuspendUpdates(updateMask);
        }
    }
}
