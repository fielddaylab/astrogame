using System;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Astro
{
    public sealed class DocumentPrompter : BatchedComponent, IRegistrationCallbacks
    {
        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
        }
    }

    public static partial class DocumentUtility
    {
    }
}