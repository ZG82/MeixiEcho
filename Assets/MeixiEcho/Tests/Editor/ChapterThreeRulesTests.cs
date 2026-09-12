#if UNITY_EDITOR
using NUnit.Framework;

namespace MeixiEcho.Editor.Tests
{
    public sealed class ChapterThreeRulesTests
    {
        [TestCase(60f, 0)]
        [TestCase(59.99f, 1)]
        [TestCase(35f, 1)]
        [TestCase(34.99f, 2)]
        [TestCase(0f, 2)]
        public void SanityDifficulty_UsesExactStoryThresholds(float sanity, int expected)
        {
            Assert.That(ChapterThreeRules.Difficulty(sanity), Is.EqualTo(expected));
        }

        [Test]
        public void Schedule_HasSevenAndNineDayTotalsWithoutBitCollisions()
        {
            Assert.That(ChapterThreeRules.TotalAssignedTasks(7), Is.EqualTo(18));
            Assert.That(ChapterThreeRules.TotalAssignedTasks(9), Is.EqualTo(23));
            int seen = 0;
            for (int day = 1; day <= 9; day++)
            {
                ChapterThreeTaskKind[] tasks = ChapterThreeRules.TasksForDay(day);
                for (int slot = 0; slot < tasks.Length; slot++)
                {
                    int bit = 1 << ChapterThreeRules.TaskBitIndex(day, slot);
                    Assert.That(seen & bit, Is.Zero);
                    seen |= bit;
                }
            }
            Assert.That(ChapterThreeRules.CountCompletedTasks(seen, 9), Is.EqualTo(23));
        }

        [Test]
        public void ScoreTiers_RespectSoulAndModelBoundaries()
        {
            Assert.That(ChapterThreeRules.EvaluateTier(ChapterThreeRules.SoulScoreThreshold - .01f), Is.EqualTo(ChapterThreeEndingTier.Ordinary));
            Assert.That(ChapterThreeRules.EvaluateTier(ChapterThreeRules.SoulScoreThreshold), Is.EqualTo(ChapterThreeEndingTier.Excellent));
            Assert.That(ChapterThreeRules.EvaluateTier(ChapterThreeRules.ModelScoreThreshold), Is.EqualTo(ChapterThreeEndingTier.Model));
        }

        [Test]
        public void EvidenceMasks_RequireEveryUniqueClue()
        {
            int truthAll = (1 << ChapterThreeRules.TruthClueCount) - 1;
            int housingAll = (1 << ChapterThreeRules.HousingClueCount) - 1;
            Assert.That(ChapterThreeRules.HasAllTruthClues(truthAll), Is.True);
            Assert.That(ChapterThreeRules.HasAllTruthClues(truthAll & ~(1 << 3)), Is.False);
            Assert.That(ChapterThreeRules.HasAllHousingClues(housingAll), Is.True);
            Assert.That(ChapterThreeRules.HasAllHousingClues(housingAll & ~(1 << 4)), Is.False);
        }

        [Test]
        public void AverageScore_CountsMissingTasksAsZero()
        {
            float sevenDayPerfect = ChapterThreeRules.TotalAssignedTasks(7) * 100f;
            Assert.That(ChapterThreeRules.AverageScore(sevenDayPerfect, 7), Is.EqualTo(100f));
            Assert.That(ChapterThreeRules.AverageScore(sevenDayPerfect - 100f, 7), Is.LessThan(100f));
        }

        [Test]
        public void OptionalExchangeDays_DoNotChangeSevenDayAssessment()
        {
            Assert.That(ChapterThreeRules.CountsTowardAssessment(7), Is.True);
            Assert.That(ChapterThreeRules.CountsTowardAssessment(8), Is.False);
            Assert.That(ChapterThreeRules.CountsTowardAssessment(9), Is.False);
        }

        [TestCase(0f, 0f)]
        [TestCase(4.5f, 50f)]
        [TestCase(9f, 100f)]
        [TestCase(12f, 100f)]
        public void SleepRecovery_IsProportionalAndCapsAtNineHours(float hours, float expected)
        {
            Assert.That(ChapterThreeRules.SleepRecovery(hours), Is.EqualTo(expected).Within(.01f));
        }

        [Test]
        public void SleepRecovery_AddsToCurrentSanityAndCapsAtOneHundred()
        {
            Assert.That(ChapterThreeRules.SanityAfterSleep(40f, 4.5f), Is.EqualTo(90f).Within(.01f));
            Assert.That(ChapterThreeRules.SanityAfterSleep(35f, 9f), Is.EqualTo(100f).Within(.01f));
        }

        [Test]
        public void SanityDrain_IsContinuousByDayAndAcceleratesAtNight()
        {
            Assert.That(ChapterThreeRules.SanityDrainPerSecond(12f, true),
                Is.EqualTo(ChapterThreeRules.DaySanityDrainPerSecond));
            Assert.That(ChapterThreeRules.SanityDrainPerSecond(22f, true),
                Is.GreaterThan(ChapterThreeRules.DaySanityDrainPerSecond));
            Assert.That(ChapterThreeRules.SanityDrainPerSecond(22f, false),
                Is.GreaterThan(ChapterThreeRules.SanityDrainPerSecond(22f, true)));
        }
    }
}
#endif
