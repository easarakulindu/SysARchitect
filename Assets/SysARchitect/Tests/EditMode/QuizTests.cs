using System.Linq;
using NUnit.Framework;
using SysARchitect.Lessons;
using SysARchitect.Quiz;

namespace SysARchitect.Tests
{
    public class QuizTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Json = ""; public string Read() => Json; public void Write(string value) => Json = value; }
        private static QuizSession Completed(QuizStage stage)
        {
            var session = new QuizSession(stage);
            while (!session.Complete) { session.Answer(session.Current.CorrectAnswer); if (!session.Complete) session.Next(); }
            return session;
        }
        [Test]
        public void BankHasTwentyUniqueExplainedQuestionsAndBalancedDisjointForms()
        {
            Assert.That(QuizCatalog.All.Count, Is.EqualTo(20)); Assert.That(QuizCatalog.All.Select(q => q.Id).Distinct().Count(), Is.EqualTo(20));
            var pre = QuizCatalog.Form(QuizStage.Pre); var post = QuizCatalog.Form(QuizStage.Post);
            Assert.That(pre.Count, Is.EqualTo(10)); Assert.That(post.Count, Is.EqualTo(10));
            Assert.That(pre.Select(q => q.Id).Intersect(post.Select(q => q.Id)), Is.Empty);
            foreach (var lesson in LessonCatalog.All)
            {
                Assert.That(pre.Count(q => q.Lesson == lesson.Id), Is.EqualTo(2)); Assert.That(post.Count(q => q.Lesson == lesson.Id), Is.EqualTo(2));
            }
            foreach (var question in QuizCatalog.All)
            {
                Assert.That(question.Choices.Count, Is.EqualTo(4)); Assert.That(question.Explanation.Length, Is.GreaterThan(40));
                Assert.That(question.Choices.Distinct().Count(), Is.EqualTo(4));
            }
        }
        [Test]
        public void SessionRequiresAnswersRejectsDoubleSubmissionAndScoresActualChoices()
        {
            var session = new QuizSession(QuizStage.Pre);
            Assert.Throws<System.InvalidOperationException>(() => session.Next());
            Assert.Throws<System.ArgumentOutOfRangeException>(() => session.Answer(4));
            session.Answer((session.Current.CorrectAnswer + 1) % 4);
            Assert.That(session.Correct, Is.Zero); Assert.That(session.Feedback, Does.Contain("Correct answer"));
            Assert.Throws<System.InvalidOperationException>(() => session.Answer(0));
            session.Next(); while (!session.Complete) { session.Answer(session.Current.CorrectAnswer); if (!session.Complete) session.Next(); }
            Assert.That(session.Correct, Is.EqualTo(9)); Assert.Throws<System.InvalidOperationException>(() => session.Next());
        }
        [Test]
        public void ScoresPersistOnceAndAreRecomputedFromValidatedAnswers()
        {
            var store = new MemoryStore(); var progress = new LearningProgress(store);
            Assert.Throws<System.InvalidOperationException>(() => progress.RecordQuiz(new QuizSession(QuizStage.Pre)));
            var session = Completed(QuizStage.Pre); progress.RecordQuiz(session); progress.RecordQuiz(session);
            Assert.That(progress.QuizAttempts.Count, Is.EqualTo(1));
            store.Json = store.Json.Replace("\"correct\":10", "\"correct\":999");
            var restored = new LearningProgress(store); Assert.That(restored.LatestQuiz(QuizStage.Pre).correct, Is.EqualTo(10));
            Assert.That(restored.LatestQuiz(QuizStage.Post), Is.Null);
        }
        [Test]
        public void EarlierLessonDataIsPreservedAndQuizHistoryIsBounded()
        {
            var store = new MemoryStore { Json = "{\"version\":1,\"completedLessons\":[\"Docker\"]}" };
            var progress = new LearningProgress(store); Assert.That(progress.IsComplete(LessonId.Docker), Is.True);
            for (int i = 0; i < 21; i++) progress.RecordQuiz(Completed(QuizStage.Post));
            var restored = new LearningProgress(store);
            Assert.That(restored.QuizAttempts.Count, Is.EqualTo(20)); Assert.That(restored.IsComplete(LessonId.Docker), Is.True);
            Assert.That(restored.LatestQuiz(QuizStage.Post).total, Is.EqualTo(10));
        }
    }
}
