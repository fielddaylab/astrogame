using BeauUtil;
using BeauUtil.Debugger;
using System;
using UnityEngine;

namespace Astro.Audio {
    public unsafe struct VoxWaveformChunk {
        public fixed byte Data[VoxWaveform.BytesPerChunk];
    }

    public struct VoxWaveform {
        public const int BitsPerSample = 8; // NOTE: keep this synced up with ReadSample and GenerateChunks
        public const int AmplitudeToSample = (1 << BitsPerSample) - 1;
        public const float SampleToAmplitude = 1.0f / AmplitudeToSample;

        public const float AmplitudePowerScale = 0.25f;

        public const int SamplesPerChunk = 4; // NOTE: Must be power of 2
        public const int ChunksPerSecond = 2;
        public const int BytesPerChunk = BitsPerSample * SamplesPerChunk / 8;

        public UnsafeSpan<VoxWaveformChunk> Chunks;

        static public float ReadAmplitude(in VoxWaveform waveform, float time) {
            Assert.True(waveform.Chunks.Length >= 2);
            float sampleIdxF = time * (ChunksPerSecond * SamplesPerChunk);
            int sampleIdxA = Math.Min((int) sampleIdxF, (waveform.Chunks.Length * SamplesPerChunk) - 2);
            int sampleIdxB = sampleIdxA + 1;
            float sampleLerp = Math.Min(1, sampleIdxF - sampleIdxA);

            return ReadSample(waveform, sampleIdxA) * (1 - sampleLerp)
                + ReadSample(waveform, sampleIdxB) * (sampleLerp);
        }

        static public unsafe float ReadSample(in VoxWaveform waveform, int sampleIdx) {
            int chunkIdx = sampleIdx / SamplesPerChunk;
            int localSampleIdx = sampleIdx & (SamplesPerChunk - 1);
            ref VoxWaveformChunk chunk = ref waveform.Chunks[chunkIdx];

            // 8 bits per sample
            byte data = chunk.Data[sampleIdx];
            return data * SampleToAmplitude;
        }

        static public unsafe VoxWaveformChunk[] GenerateChunks(AudioClip clip) {
            int channelCount = clip.channels;
            int totalClipSamples = clip.samples * channelCount;
            int clipSamplesPerChunk = clip.frequency * channelCount / ChunksPerSecond;

            int clipSamplesPerChunkSample = clipSamplesPerChunk / SamplesPerChunk;
            int totalChunkSamples = totalClipSamples / clipSamplesPerChunkSample;
            int totalChunkSamplesPadded = Unsafe.AlignUpN(totalChunkSamples, SamplesPerChunk);
            int totalChunks = totalChunkSamplesPadded / SamplesPerChunk;

            //Log.Msg("duration {0}secs, {1} samples ({2} channel(s) at {3}Hz) - {4} amplitude samples", clip.length, totalClipSamples, channelCount, clip.frequency, totalChunkSamples);

            VoxWaveformChunk[] chunks = new VoxWaveformChunk[totalChunks];
            float[] clipSampleBuffer = new float[clipSamplesPerChunkSample];
            int clipSampleInterval = clipSamplesPerChunkSample / channelCount;

            for (int i = 0; i < totalChunkSamples; i++) {
                int chunkIdx = i / SamplesPerChunk;
                int sampleIdx = i & (SamplesPerChunk - 1);

                clip.GetData(clipSampleBuffer, clipSampleInterval * i);
                float amp = CalcAvgAmp(clipSampleBuffer);

                byte data = (byte)(amp * AmplitudeToSample);
                chunks[chunkIdx].Data[sampleIdx] = data;
            }

            for (int i = totalChunkSamples; i < totalChunkSamplesPadded; i++) {
                int chunkIdx = i / SamplesPerChunk;
                int sampleIdx = i & (SamplesPerChunk - 1);
                chunks[chunkIdx].Data[sampleIdx] = 0;
            }

            return chunks;
        }

        static private float CalcAvgAmp(float[] data) {
            // Scaling during accumulation vs after produces different results
            // Since it's only an approximation for purposes of amplification,
            // the inaccuracy of scaling after accumulation is acceptable.

            Assert.True(data.Length > 0);
            float accum = 0;
            for(int i = 0; i < data.Length; i++) {
                accum += Mathf.Abs(data[i]);
            }
            accum /= data.Length;
            return (float) Math.Pow(accum, AmplitudePowerScale);
        }
    }
}