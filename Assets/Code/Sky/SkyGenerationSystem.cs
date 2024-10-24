using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyGenerationSystem : SystemBehaviour
    {
        public override void ProcessWork(float deltaTime)
        {
            var layout = Game.Assets.GetGlobal(typeof(SkyLayoutAsset));
        }
    }
}