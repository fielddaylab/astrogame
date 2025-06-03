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

        public const int SamplesPerChunk = 4; // NOTE: Must be power of 2
        public const int ChunksPerSecond = 8;
        public const int BytesPerChunk = BitsPerSample * SamplesPerChunk / 8;

        public UnsafeSpan<VoxWaveformChunk> Chunks;

        static public float ReadAmplitude(in VoxWaveform waveform, float time, float duration) {
            Assert.True(waveform.Chunks.Length >= 2);
            float sampleIdxF = time * (ChunksPerSecond * SamplesPerChunk);
            int sampleIdxA = Math.Min((int) sampleIdxF, (waveform.Chunks.Length * SamplesPerChunk) - 2);
            int sampleIdxB = sampleIdxA + 1;
            float sampleLerp = Math.Min(1, sampleIdxF - sampleIdxA);

            Log.Msg("sample {0} to {1} ({2}) (time {3}/{4})", sampleIdxA, sampleIdxB, sampleLerp, time, duration);

            return ReadSample(waveform, sampleIdxA) * (1 - sampleLerp)
                + ReadSample(waveform, sampleIdxB) * (sampleLerp);
        }

        static public unsafe float ReadSample(in VoxWaveform waveform, int sampleIndex) {
            int chunkIdx = sampleIndex / SamplesPerChunk;
            int localSampleIdx = sampleIndex & (SamplesPerChunk - 1);
            ref VoxWaveformChunk chunk = ref waveform.Chunks[chunkIdx];

            // 8 bits per sample
            byte data = chunk.Data[sampleIndex];
            return data * SampleToAmplitude;
        }

        static public unsafe VoxWaveformChunk[] GenerateChunks(AudioClip clip) {
            int channelCount = clip.channels;
            int totalClipSamples = clip.samples * channelCount;
            int clipSamplesPerChunk = clip.frequency * channelCount / ChunksPerSecond;

            int clipSamplesPerChunkSample = clipSamplesPerChunk / SamplesPerChunk;
            int totalChunkSamples = 1 + totalClipSamples / clipSamplesPerChunkSample;
            int totalChunkSamplesPadded = Unsafe.AlignUpN(totalChunkSamples, SamplesPerChunk);
            int totalChunks = totalChunkSamplesPadded / SamplesPerChunk;

            //Log.Msg("duration {0}secs, {1} samples ({2} channel(s) at {3}Hz) - {4} amplitude samples", clip.length, totalClipSamples, channelCount, clip.frequency, totalChunkSamples);

            VoxWaveformChunk[] chunks = new VoxWaveformChunk[totalChunks];
            float[] clipSampleBuffer = new float[clipSamplesPerChunkSample];
            int clipSampleInterval = clipSamplesPerChunkSample / channelCount;

            int halfSampleCount = clipSamplesPerChunkSample / 2;
            float* sampleAmpFloats = stackalloc float[totalChunkSamples];

            fixed (float* clipSampleBufferPtr = clipSampleBuffer) {
                for (int i = 0; i < totalChunkSamples - 1; i++) {
                    clip.GetData(clipSampleBuffer, clipSampleInterval * i);

                    float left = CalculateRMS(clipSampleBufferPtr, clipSamplesPerChunkSample);
                    
                    //sampleAmpFloats[i] += left;
                    sampleAmpFloats[i + 1] += left;
                }
            }
            
            fixed(VoxWaveformChunk* chunkBuffer = chunks) {
                byte* sampleBuff = (byte*) chunkBuffer;
                for(int i = 0; i < totalChunkSamples; i++) {
                    float sample = sampleAmpFloats[i];
                    sampleBuff[i] = (byte) (sample * AmplitudeToSample);
                }
            }

            return chunks;
        }

        static private unsafe float CalculateRMS(float* data, int count) {
            Assert.True(count > 0);
            float accum = 0;

            for(int i = 0; i < count; i++) {
                accum += data[i] * data[i];
            }

            accum /= count;
            return (float) Math.Sqrt(accum);
        }
    }
}