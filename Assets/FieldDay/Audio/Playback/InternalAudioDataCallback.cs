using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    [RequireComponent(typeof(AudioSource))]
    internal class InternalAudioDataCallback : MonoBehaviour {
        [Flags]
        internal enum CallbackTypes : byte {
            OutputData = 0x01,
            SpectrumData = 0x02
        }

        internal AudioHandle Handle;
        internal object Context;

        internal CallbackTypes Callbacks;
        internal AudioOutputPCMCallback OutputCallback;
        internal AudioOutputPCMCallback SpectrumCallback;
    }

    public delegate void AudioOutputPCMCallback(AudioHandle handle, UnsafeSpan<byte> data, int channel, object context);
}