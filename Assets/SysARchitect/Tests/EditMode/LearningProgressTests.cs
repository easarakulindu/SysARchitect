using System.Linq;
using NUnit.Framework;
using SysARchitect.Lessons;

namespace SysARchitect.Tests
{
    public class LearningProgressTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Json = ""; public string Read() => Json; public void Write(string json) => Json = json; }
        [Test]
        public void CompletionRoundTripsLocallyAndDoesNotDuplicateLessons()
        {
            var store = new MemoryStore(); var progress = new LearningProgress(store);
            progress.Complete(LessonId.Docker); progress.Complete(LessonId.Docker);
            var restored = new LearningProgress(store);
            Assert.That(restored.CompletedCount, Is.EqualTo(1)); Assert.That(restored.IsComplete(LessonId.Docker), Is.True);
            Assert.That(restored.IsComplete(LessonId.Database), Is.False);
        }
        [Test]
        public void CorruptedAndUnknownProgressCannotFabricateCompletion()
        {
            var store = new MemoryStore { Json = "broken json" };
            Assert.That(new LearningProgress(store).CompletedCount, Is.Zero);
            store.Json = "{\"version\":1,\"completedLessons\":[\"Docker\",\"Docker\",\"Invented\"]}";
            Assert.That(new LearningProgress(store).CompletedCount, Is.EqualTo(1));
        }
        [Test]
        public void AllFiveModulesHaveObjectivesStepsAlgorithmsAndChallenges()
        {
            Assert.That(LessonCatalog.All.Count, Is.EqualTo(5));
            Assert.That(LessonCatalog.All.Select(l => l.Id).Distinct().Count(), Is.EqualTo(5));
            foreach (var lesson in LessonCatalog.All)
            {
                Assert.That(lesson.Objectives, Is.Not.Empty); Assert.That(lesson.Explanation.Length, Is.GreaterThan(150));
                Assert.That(lesson.Steps.Count, Is.GreaterThanOrEqualTo(4)); Assert.That(lesson.Algorithm.Count, Is.GreaterThanOrEqualTo(5));
                Assert.That(lesson.Challenge, Is.Not.Empty);
            }
        }
    }
}
