using System;
using System.IO;
using UnityEngine;

namespace MeixiEcho
{
    [Serializable]
    public sealed class ChapterOneBattleBeatMapData
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

    /// <summary>
    /// Converts the offline-analyzed song timeline to DSP time. Every combat cue,
    /// input window and HUD pulse consults this object instead of a synthetic metronome.
    /// </summary>
    public sealed class ChapterOneBattleTimeline
    {
        private readonly ChapterOneBattleBeatMapData data;

        public bool IsValid => data != null && data.beats != null && data.beats.Length >= 8 && data.duration > 1f;
        public float Bpm => IsValid ? data.bpm : 96f;
        public float Duration => IsValid ? data.duration : 0f;
        public int BeatCount => IsValid ? data.beats.Length : 0;

        private ChapterOneBattleTimeline(ChapterOneBattleBeatMapData source)
        {
            data = source;
        }

        public static ChapterOneBattleTimeline Load()
        {
            return Load("Audio/ChapterOneBattleBeatMap");
        }

        public static ChapterOneBattleTimeline Load(string resourcePath)
        {
            TextAsset json = Resources.Load<TextAsset>(resourcePath);
            if (json == null)
            {
                Debug.LogWarning("[MeixiBeatMap] Beat map is missing: Resources/" + resourcePath + ".json");
                return new ChapterOneBattleTimeline(null);
            }
            try
            {
                var parsed = JsonUtility.FromJson<ChapterOneBattleBeatMapData>(json.text);
                var timeline = new ChapterOneBattleTimeline(parsed);
                if (!timeline.IsValid)
                    Debug.LogError("[MeixiBeatMap] Beat map JSON is invalid or contains too few beats.");
                else
                    Debug.Log($"[MeixiBeatMap] Loaded analyzed BGM grid bpm={timeline.Bpm:F3} beats={timeline.BeatCount} duration={timeline.Duration:F3}s");
                return timeline;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return new ChapterOneBattleTimeline(null);
            }
        }

        public static ChapterOneBattleTimeline LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return new ChapterOneBattleTimeline(null);
            try
            {
                ChapterOneBattleBeatMapData parsed = JsonUtility.FromJson<ChapterOneBattleBeatMapData>(File.ReadAllText(filePath));
                return new ChapterOneBattleTimeline(parsed);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[MeixiBeatMap] External beat map could not be loaded: " + exception.Message);
                return new ChapterOneBattleTimeline(null);
            }
        }

        public double GetNextBeatDsp(double afterDsp, double songStartDsp, int minimumBeatSpacing, bool preferStrong)
        {
            if (!IsValid) return afterDsp + 0.625d;
            double afterSong = Math.Max(0d, afterDsp - songStartDsp);
            long cycle = (long)Math.Floor(afterSong / data.duration);
            float local = (float)(afterSong - cycle * data.duration);
            int index = FindFirstBeatAfter(local);
            if (index >= data.beats.Length)
            {
                cycle++;
                index = 0;
            }

            int stride = Math.Max(1, minimumBeatSpacing);
            if (preferStrong)
            {
                int strongIndex = FindFirstStrongBeatAtOrAfter(index);
                if (strongIndex >= 0) index = strongIndex;
            }
            // Preserve phase-specific breathing room even when the strong-beat lookup
            // points at adjacent analyzed onsets.
            if (index + stride - 1 >= data.beats.Length && local > data.beats[index]) cycle++;
            return songStartDsp + cycle * data.duration + data.beats[index];
        }

        public double GetFollowingBeatDsp(double anchorDsp, double songStartDsp, int offset)
        {
            if (!IsValid) return anchorDsp + Math.Max(0, offset) * 0.625d;
            double song = Math.Max(0d, anchorDsp - songStartDsp);
            long cycle = (long)Math.Floor(song / data.duration);
            float local = (float)(song - cycle * data.duration);
            int index = FindNearestBeatIndex(local) + Math.Max(0, offset);
            while (index >= data.beats.Length)
            {
                index -= data.beats.Length;
                cycle++;
            }
            return songStartDsp + cycle * data.duration + data.beats[index];
        }

        public double DistanceToNearestBeat(double dspTime, double songStartDsp)
        {
            if (!IsValid) return double.MaxValue;
            double song = dspTime - songStartDsp;
            if (song < 0d) return -song + data.beats[0];
            double cycle = Math.Floor(song / data.duration);
            float local = (float)(song - cycle * data.duration);
            int index = FindNearestBeatIndex(local);
            double distance = Math.Abs(local - data.beats[index]);
            if (index == 0) distance = Math.Min(distance, Math.Abs(local - (data.beats[data.beats.Length - 1] - data.duration)));
            if (index == data.beats.Length - 1) distance = Math.Min(distance, Math.Abs(local - (data.duration + data.beats[0])));
            return distance;
        }

        public float GetPulse(double dspTime, double songStartDsp, float falloffSeconds = 0.18f)
        {
            double distance = DistanceToNearestBeat(dspTime, songStartDsp);
            return Mathf.Clamp01(1f - (float)distance / Mathf.Max(0.02f, falloffSeconds));
        }

        private int FindFirstBeatAfter(float localTime)
        {
            int low = 0;
            int high = data.beats.Length;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (data.beats[middle] <= localTime + 0.0001f) low = middle + 1;
                else high = middle;
            }
            return low;
        }

        private int FindNearestBeatIndex(float localTime)
        {
            int next = FindFirstBeatAfter(localTime);
            if (next <= 0) return 0;
            if (next >= data.beats.Length) return data.beats.Length - 1;
            return localTime - data.beats[next - 1] <= data.beats[next] - localTime ? next - 1 : next;
        }

        private int FindFirstStrongBeatAtOrAfter(int beatIndex)
        {
            if (data.strongBeatIndices == null || data.strongBeatIndices.Length == 0) return -1;
            for (int i = 0; i < data.strongBeatIndices.Length; i++)
            {
                int candidate = data.strongBeatIndices[i];
                if (candidate >= beatIndex && candidate < data.beats.Length) return candidate;
            }
            return -1;
        }
    }
}
