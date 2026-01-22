using System;
using Astro;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro
{
    [RequireComponent(typeof(ViewNode))]
    public class ViewDocumentReleaseTrigger : BatchedComponent, IRegistrationCallbacks
    {
        public void OnDeregister()
        {

        }

        public void OnRegister()
        {
            GetComponent<ViewNode>().OnExit.Register(OnExitAction);
        }

        private void OnExitAction(ViewNode node)
        {
            // put away current document if viewing one
            var docState = Find.State<DocumentBoardState>();
            if (docState && docState.SelectedDocument)
            {
                DocumentUtility.DeselectDocument(docState);
            }
            if (docState && docState.DocZoomed)
            {
                DocumentUtility.ToggleZoomDoc(docState.DocZoomed, true, docState);
            }
        }
    }
}
