using Astro.Audio;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleMusic : SharedStateComponent, IRegistrationCallbacks {
        [AudioEvent] public StringHash32 MusicEvent;
        [AudioEvent] public StringHash32 TensionSfx;

        public float FadeInDuration = 1;
        [NonSerialized] public AudioHandle TensionHandle;

        void IRegistrationCallbacks.OnRegister() {
            AstroGame.Events.Register<ViewNode>(ViewNavUtility.Events.NodeEntered, OnNodeLoaded);
            ScriptUtility.RegisterForSignal("KillMusic", OnKillMusicSignal);
            ScriptUtility.RegisterForSignal("BeginTension", OnBeginTension);
        }

        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.DeregisterAllSignalsForContext(this);
            Game.Events?.DeregisterAllForContext(this);
        }

        private void OnNodeLoaded(ViewNode node) {
            StringHash32 nodeId = node.Id;

            if (nodeId == "Title") {
                MusicUtility.PlayMusic(MusicEvent, FadeInDuration);
            }
        }

        private void OnKillMusicSignal() {
            MusicUtility.StopMusic(0.05f);
            Sfx.Stop(TensionHandle, 0.05f);
            Sfx.Play("Prelude.Panic");
        }

        private void OnBeginTension() {
            TensionHandle = Sfx.Play("Prelude.RisingTension");
        }
    }
}