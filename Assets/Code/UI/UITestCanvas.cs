using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class UITestCanvas : MonoBehaviour
    {
        public Button ObjBtn1;
        public Button ObjBtn2;

        public CelestialAsset[] CelestialAssets; 

        private void Awake()
        {
            ObjBtn1.onClick.AddListener(() => { HandleObjBtnClicked(0); });
            ObjBtn2.onClick.AddListener(() => { HandleObjBtnClicked(1); });
        }

        private void HandleObjBtnClicked(int index)
        {
            var dataState = Find.State<DataPacketDistributionState>();

            DataDistributionUtility.QueueConversion(dataState, CelestialAssets[index]);
        }
    }
}
