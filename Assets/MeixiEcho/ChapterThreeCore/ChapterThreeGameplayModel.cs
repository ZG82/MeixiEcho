using System;
using UnityEngine;

namespace MeixiEcho
{
    public enum ChapterThreeTaskKind
    {
        Carry,
        Clean,
        Type,
        OneStroke
    }

    public enum ChapterThreeEndingTier
    {
        Ordinary,
        Excellent,
        Model
    }

    public static class ChapterThreeRules
    {
        // One in-game day lasts thirteen real-time minutes.
        public const float RealSecondsPerGameDay = 780f;
        public const float GameMinutesPerRealSecond = 1440f / RealSecondsPerGameDay;
        public const float SoulScoreThreshold = 70f;
        public const float ModelScoreThreshold = 90f;
        public const float FullSanitySleepHours = 9f;
        // Make daytime choices matter during the ten-minute game day.
        public const float DaySanityDrainPerSecond = .14f;
        public const float LitNightSanityDrainPerSecond = .16f;
        public const float DarkNightSanityDrainPerSecond = .28f;
        public const int TruthClueCount = 8;
        public const int HousingClueCount = 5;

        private static readonly ChapterThreeTaskKind[][] Schedule =
        {
            Array.Empty<ChapterThreeTaskKind>(),
            new[] { ChapterThreeTaskKind.Carry, ChapterThreeTaskKind.Clean, ChapterThreeTaskKind.Type },
            new[] { ChapterThreeTaskKind.OneStroke, ChapterThreeTaskKind.Type },
            new[] { ChapterThreeTaskKind.Clean, ChapterThreeTaskKind.Carry, ChapterThreeTaskKind.OneStroke },
            new[] { ChapterThreeTaskKind.Type, ChapterThreeTaskKind.Clean },
            new[] { ChapterThreeTaskKind.Carry, ChapterThreeTaskKind.OneStroke, ChapterThreeTaskKind.Type },
            new[] { ChapterThreeTaskKind.Clean, ChapterThreeTaskKind.Type, ChapterThreeTaskKind.Carry },
            new[] { ChapterThreeTaskKind.OneStroke, ChapterThreeTaskKind.Clean },
            new[] { ChapterThreeTaskKind.Carry, ChapterThreeTaskKind.Type },
            new[] { ChapterThreeTaskKind.Clean, ChapterThreeTaskKind.OneStroke, ChapterThreeTaskKind.Type }
        };

        public static int Difficulty(float sanity)
        {
            if (sanity < 35f) return 2;
            return sanity < 60f ? 1 : 0;
        }

        public static float SleepRecovery(float hours)
        {
            return Mathf.Clamp(hours, 0f, FullSanitySleepHours) * (100f / FullSanitySleepHours);
        }

        public static float SanityAfterSleep(float currentSanity, float hours)
        {
            return Mathf.Clamp(currentSanity + SleepRecovery(hours), 0f, 100f);
        }

        public static float SanityDrainPerSecond(float gameHour, bool lightsOn)
        {
            float hour = Mathf.Repeat(gameHour, 24f);
            bool night = hour >= 20f || hour < 5f;
            if (!night) return DaySanityDrainPerSecond;
            return lightsOn ? LitNightSanityDrainPerSecond : DarkNightSanityDrainPerSecond;
        }

        public static ChapterThreeTaskKind[] TasksForDay(int day)
        {
            return Schedule[Mathf.Clamp(day, 1, 9)];
        }

        public static int TaskBitIndex(int day, int slot)
        {
            int index = 0;
            for (int current = 1; current < Mathf.Clamp(day, 1, 9); current++) index += Schedule[current].Length;
            return index + Mathf.Clamp(slot, 0, Schedule[Mathf.Clamp(day, 1, 9)].Length - 1);
        }

        public static int TotalAssignedTasks(int finalDay)
        {
            int total = 0;
            for (int day = 1; day <= Mathf.Clamp(finalDay, 1, 9); day++) total += Schedule[day].Length;
            return total;
        }

        public static int CountCompletedTasks(int taskMask, int throughDay)
        {
            int count = 0;
            int max = TotalAssignedTasks(throughDay);
            for (int i = 0; i < max; i++) if ((taskMask & (1 << i)) != 0) count++;
            return count;
        }

        public static bool IsTaskCompleted(int taskMask, int day, int slot)
        {
            return (taskMask & (1 << TaskBitIndex(day, slot))) != 0;
        }

        public static bool IsDayComplete(int taskMask, int day)
        {
            ChapterThreeTaskKind[] tasks = TasksForDay(day);
            for (int slot = 0; slot < tasks.Length; slot++)
                if (!IsTaskCompleted(taskMask, day, slot)) return false;
            return true;
        }

        public static float AverageScore(float scoreTotal, int finalDay)
        {
            return Mathf.Clamp(scoreTotal / Mathf.Max(1, TotalAssignedTasks(finalDay)), 0f, 100f);
        }

        public static bool CountsTowardAssessment(int day) => day >= 1 && day <= 7;

        public static ChapterThreeEndingTier EvaluateTier(float averageScore)
        {
            if (averageScore < SoulScoreThreshold) return ChapterThreeEndingTier.Ordinary;
            return averageScore >= ModelScoreThreshold ? ChapterThreeEndingTier.Model : ChapterThreeEndingTier.Excellent;
        }

        public static bool HasAllTruthClues(int mask) => (mask & ((1 << TruthClueCount) - 1)) == (1 << TruthClueCount) - 1;
        public static bool HasAllHousingClues(int mask) => (mask & ((1 << HousingClueCount) - 1)) == (1 << HousingClueCount) - 1;

        public static string TaskName(ChapterThreeTaskKind kind) => kind switch
        {
            ChapterThreeTaskKind.Carry => "搬运行李",
            ChapterThreeTaskKind.Clean => "整理客房",
            ChapterThreeTaskKind.Type => "录入实践材料",
            _ => "一笔画声纹"
        };
    }
}
