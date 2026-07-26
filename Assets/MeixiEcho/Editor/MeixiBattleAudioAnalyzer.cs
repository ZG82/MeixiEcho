using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MeixiEcho.Editor
{
    /// <summary>
    /// Offline onset/tempo analysis for the authored chapter-one battle music.
    /// The generated JSON is deterministic and ships with the build, so runtime
    /// combat never performs expensive FFT work or guesses timing from a fixed BPM.
    /// </summary>
    public static class MeixiBattleAudioAnalyzer
    {
        private const string AudioPath = "Assets/MeixiEcho/Resources/Audio/ChapterOneBattle.mp3";
        private const string OutputPath = "Assets/MeixiEcho/Resources/Audio/ChapterOneBattleBeatMap.json";
        private const int FftSize = 2048;
        private const int HopSize = 512;

        [Serializable]
        private sealed class BeatMapFile
        {
            public string source;
            public float duration;
            public float bpm;
            public float beatOffset;
            public float analysisHopSeconds;
            public float[] beats;
            public float[] strengths;
            public int[] strongBeatIndices;
            public string analysis;
        }

        [MenuItem("Meixi Echo/Audio/Analyze Chapter One Battle BGM")]
        public static void Analyze()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (clip == null) throw new FileNotFoundException("Battle AudioClip not found", AudioPath);
            if (!clip.LoadAudioData()) throw new InvalidOperationException("Unity could not decode " + AudioPath);

            int channels = Math.Max(1, clip.channels);
            float[] interleaved = new float[clip.samples * channels];
            if (!clip.GetData(interleaved, 0))
                throw new InvalidOperationException("AudioClip.GetData failed. Import the clip as Decompress On Load.");

            float[] mono = new float[clip.samples];
            for (int frame = 0, source = 0; frame < mono.Length; frame++)
            {
                float sum = 0f;
                for (int channel = 0; channel < channels; channel++) sum += interleaved[source++];
                mono[frame] = sum / channels;
            }

            float hopSeconds = HopSize / (float)clip.frequency;
            float[] onset = BuildOnsetEnvelope(mono, clip.frequency);
            NormalizeEnvelope(onset);
            float bpm = EstimateTempo(onset, hopSeconds);
            float secondsPerBeat = 60f / bpm;
            float offset = EstimateBeatOffset(onset, hopSeconds, secondsPerBeat);
            BuildBeatGrid(onset, hopSeconds, clip.length, secondsPerBeat, offset,
                out float[] beats, out float[] strengths, out int[] strongBeatIndices);

            var map = new BeatMapFile
            {
                source = AudioPath,
                duration = clip.length,
                bpm = bpm,
                beatOffset = offset,
                analysisHopSeconds = hopSeconds,
                beats = beats,
                strengths = strengths,
                strongBeatIndices = strongBeatIndices,
                analysis = "2048-sample Hann FFT; positive spectral flux (45-9000 Hz); autocorrelation tempo; local-onset snapped beat grid"
            };

            string absolute = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? string.Empty);
            File.WriteAllText(absolute, JsonUtility.ToJson(map, true));
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[MeixiBeatAnalysis] source={clip.name} duration={clip.length:F3}s rate={clip.frequency}Hz " +
                      $"channels={channels} bpm={bpm:F3} offset={offset:F3}s beats={beats.Length} strong={strongBeatIndices.Length} output={OutputPath}");
        }

        private static float[] BuildOnsetEnvelope(float[] mono, int sampleRate)
        {
            int frameCount = Math.Max(1, 1 + (mono.Length - FftSize) / HopSize);
            float[] envelope = new float[frameCount];
            float[] real = new float[FftSize];
            float[] imaginary = new float[FftSize];
            float[] previousMagnitude = new float[FftSize / 2];
            int firstBin = Math.Max(1, Mathf.FloorToInt(45f * FftSize / sampleRate));
            int lastBin = Math.Min(FftSize / 2 - 1, Mathf.CeilToInt(9000f * FftSize / sampleRate));

            for (int frame = 0; frame < frameCount; frame++)
            {
                int start = frame * HopSize;
                for (int i = 0; i < FftSize; i++)
                {
                    float window = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (FftSize - 1));
                    real[i] = mono[start + i] * window;
                    imaginary[i] = 0f;
                }
                Fft(real, imaginary);

                float flux = 0f;
                float lowFlux = 0f;
                for (int bin = firstBin; bin <= lastBin; bin++)
                {
                    float magnitude = Mathf.Sqrt(real[bin] * real[bin] + imaginary[bin] * imaginary[bin]);
                    float delta = magnitude - previousMagnitude[bin];
                    if (delta > 0f)
                    {
                        float frequency = bin * sampleRate / (float)FftSize;
                        float weight = frequency < 220f ? 1.55f : frequency < 2400f ? 1.0f : 0.72f;
                        flux += delta * weight;
                        if (frequency < 260f) lowFlux += delta;
                    }
                    previousMagnitude[bin] = magnitude;
                }
                envelope[frame] = Mathf.Log(1f + flux * 5f) + Mathf.Log(1f + lowFlux * 8f) * 0.34f;
            }

            // Subtract a moving local floor. This preserves drum attacks while
            // suppressing sustained vocals, pads, and overall loudness changes.
            float[] cleaned = new float[envelope.Length];
            const int radius = 16;
            for (int i = 0; i < envelope.Length; i++)
            {
                int from = Math.Max(0, i - radius);
                int to = Math.Min(envelope.Length - 1, i + radius);
                float mean = 0f;
                for (int j = from; j <= to; j++) mean += envelope[j];
                mean /= to - from + 1;
                cleaned[i] = Mathf.Max(0f, envelope[i] - mean * 0.88f);
            }
            return cleaned;
        }

        private static void NormalizeEnvelope(float[] values)
        {
            float max = 0f;
            for (int i = 0; i < values.Length; i++) max = Mathf.Max(max, values[i]);
            if (max <= 0.000001f) return;
            for (int i = 0; i < values.Length; i++) values[i] /= max;
        }

        private static float EstimateTempo(float[] onset, float hopSeconds)
        {
            const float minBpm = 72f;
            const float maxBpm = 176f;
            int minLag = Mathf.Max(1, Mathf.RoundToInt(60f / maxBpm / hopSeconds));
            int maxLag = Mathf.Min(onset.Length - 1, Mathf.RoundToInt(60f / minBpm / hopSeconds));
            float bestScore = float.MinValue;
            int bestLag = minLag;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                double correlation = 0d;
                double energy = 0d;
                for (int i = lag; i < onset.Length; i++)
                {
                    correlation += onset[i] * onset[i - lag];
                    energy += onset[i] * onset[i];
                }
                float bpm = 60f / (lag * hopSeconds);
                // Musical prior is intentionally broad; it only resolves common
                // half/double-tempo ambiguity and cannot override the waveform.
                float prior = Mathf.Exp(-Mathf.Pow((bpm - 112f) / 56f, 2f)) * 0.08f + 0.92f;
                float score = (float)(correlation / Math.Max(0.000001d, energy)) * prior;
                int doubleLag = lag * 2;
                if (doubleLag <= maxLag)
                {
                    double harmonic = 0d;
                    for (int i = doubleLag; i < onset.Length; i++) harmonic += onset[i] * onset[i - doubleLag];
                    score += (float)(harmonic / Math.Max(0.000001d, energy)) * 0.18f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLag = lag;
                }
            }
            return 60f / (bestLag * hopSeconds);
        }

        private static float EstimateBeatOffset(float[] onset, float hopSeconds, float secondsPerBeat)
        {
            int periodFrames = Math.Max(2, Mathf.RoundToInt(secondsPerBeat / hopSeconds));
            float bestScore = float.MinValue;
            int bestOffset = 0;
            for (int offset = 0; offset < periodFrames; offset++)
            {
                float score = 0f;
                int count = 0;
                for (int frame = offset; frame < onset.Length; frame += periodFrames)
                {
                    float local = onset[frame];
                    if (frame > 0) local = Mathf.Max(local, onset[frame - 1] * 0.88f);
                    if (frame + 1 < onset.Length) local = Mathf.Max(local, onset[frame + 1] * 0.88f);
                    score += local;
                    count++;
                }
                score /= Math.Max(1, count);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestOffset = offset;
                }
            }
            return bestOffset * hopSeconds;
        }

        private static void BuildBeatGrid(float[] onset, float hopSeconds, float duration, float secondsPerBeat,
            float offset, out float[] beats, out float[] strengths, out int[] strongBeatIndices)
        {
            var beatList = new List<float>();
            var strengthList = new List<float>();
            int beatNumber = 0;
            for (float predicted = offset; predicted < duration - 0.03f; predicted += secondsPerBeat, beatNumber++)
            {
                int predictedFrame = Mathf.RoundToInt(predicted / hopSeconds);
                int search = Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(0.105f, secondsPerBeat * 0.20f) / hopSeconds));
                int from = Math.Max(0, predictedFrame - search);
                int to = Math.Min(onset.Length - 1, predictedFrame + search);
                int peak = Mathf.Clamp(predictedFrame, 0, onset.Length - 1);
                for (int frame = from; frame <= to; frame++)
                    if (onset[frame] > onset[peak]) peak = frame;

                float snapWeight = Mathf.Clamp01((onset[peak] - 0.06f) / 0.30f) * 0.82f;
                float snapped = Mathf.Lerp(predicted, peak * hopSeconds, snapWeight);
                if (beatList.Count > 0 && snapped <= beatList[beatList.Count - 1] + secondsPerBeat * 0.55f)
                    snapped = beatList[beatList.Count - 1] + secondsPerBeat;
                beatList.Add(snapped);
                strengthList.Add(onset[peak]);
            }

            var strong = new List<int>();
            for (int i = 0; i < beatList.Count; i++)
            {
                float value = strengthList[i];
                float previous = i > 0 ? strengthList[i - 1] : 0f;
                float next = i + 1 < strengthList.Count ? strengthList[i + 1] : 0f;
                if ((i % 4 == 0 && value >= 0.07f) || (value >= 0.20f && value >= previous && value >= next)) strong.Add(i);
            }
            beats = beatList.ToArray();
            strengths = strengthList.ToArray();
            strongBeatIndices = strong.ToArray();
        }

        private static void Fft(float[] real, float[] imaginary)
        {
            int n = real.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i >= j) continue;
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
            for (int length = 2; length <= n; length <<= 1)
            {
                float angle = -2f * Mathf.PI / length;
                float stepReal = Mathf.Cos(angle);
                float stepImaginary = Mathf.Sin(angle);
                for (int start = 0; start < n; start += length)
                {
                    float wr = 1f;
                    float wi = 0f;
                    int half = length >> 1;
                    for (int j = 0; j < half; j++)
                    {
                        int even = start + j;
                        int odd = even + half;
                        float tr = wr * real[odd] - wi * imaginary[odd];
                        float ti = wr * imaginary[odd] + wi * real[odd];
                        real[odd] = real[even] - tr;
                        imaginary[odd] = imaginary[even] - ti;
                        real[even] += tr;
                        imaginary[even] += ti;
                        float nextWr = wr * stepReal - wi * stepImaginary;
                        wi = wr * stepImaginary + wi * stepReal;
                        wr = nextWr;
                    }
                }
            }
        }
    }
}
