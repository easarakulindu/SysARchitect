using System;
using System.Collections.Generic;
using UnityEngine;
using SysARchitect.Quiz;

namespace SysARchitect.Lessons
{
    public interface IProgressStorage { string Read(); void Write(string json); }
    public sealed class PlayerPrefsProgressStorage : IProgressStorage
    {
        private const string Key = "SysARchitect.LearningProgress.v1";
        public string Read() => PlayerPrefs.GetString(Key, "");
        public void Write(string json) { PlayerPrefs.SetString(Key, json); PlayerPrefs.Save(); }
    }
    [Serializable]
    public sealed class ProgressData
    {
        public int version = 1;
        public List<string> completedLessons = new List<string>();
        public List<QuizAttempt> quizAttempts = new List<QuizAttempt>();
    }
    public sealed class LearningProgress
    {
        private readonly IProgressStorage storage;
        private readonly ProgressData data;
        public int CompletedCount => data.completedLessons.Count;
        public IReadOnlyList<QuizAttempt> QuizAttempts => data.quizAttempts.AsReadOnly();
        public LearningProgress(IProgressStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            try { data = JsonUtility.FromJson<ProgressData>(storage.Read()) ?? new ProgressData(); }
            catch (ArgumentException) { data = new ProgressData(); }
            if (data.version != 1) data = new ProgressData();
            if (data.completedLessons == null) data.completedLessons = new List<string>();
            var valid = new HashSet<string>(); foreach (var lesson in LessonCatalog.All) valid.Add(lesson.Id.ToString());
            var seen = new HashSet<string>();
            data.completedLessons.RemoveAll(id => !valid.Contains(id) || !seen.Add(id));
            if (data.quizAttempts == null) data.quizAttempts = new List<QuizAttempt>();
            data.quizAttempts.RemoveAll(attempt => !Normalize(attempt));
            if (data.quizAttempts.Count > 20) data.quizAttempts.RemoveRange(0, data.quizAttempts.Count - 20);
        }
        public bool IsComplete(LessonId id) => data.completedLessons.Contains(id.ToString());
        public void Complete(LessonId id)
        {
            LessonCatalog.Get(id); if (IsComplete(id)) return;
            data.completedLessons.Add(id.ToString()); storage.Write(JsonUtility.ToJson(data));
        }
        public void RecordQuiz(QuizSession session)
        {
            if (session == null || !session.Complete) throw new InvalidOperationException("Only completed quizzes can be saved.");
            if (session.Recorded) return;
            var attempt = new QuizAttempt { stage = session.Stage.ToString(), completedUtc = DateTime.UtcNow.ToString("O") };
            foreach (var question in session.Questions) attempt.questionIds.Add(question.Id);
            foreach (int answer in session.Answers) attempt.answers.Add(answer);
            if (!Normalize(attempt)) throw new InvalidOperationException("Quiz answers do not match a valid form.");
            data.quizAttempts.Add(attempt); if (data.quizAttempts.Count > 20) data.quizAttempts.RemoveAt(0);
            storage.Write(JsonUtility.ToJson(data)); session.Recorded = true;
        }
        public QuizAttempt LatestQuiz(QuizStage stage)
        {
            for (int i = data.quizAttempts.Count - 1; i >= 0; i--) if (data.quizAttempts[i].stage == stage.ToString()) return data.quizAttempts[i];
            return null;
        }
        private static bool Normalize(QuizAttempt attempt)
        {
            if (attempt == null || !Enum.TryParse(attempt.stage, out QuizStage stage) || (stage != QuizStage.Pre && stage != QuizStage.Post)) return false;
            var form = QuizCatalog.Form(stage);
            if (attempt.questionIds == null || attempt.answers == null || attempt.questionIds.Count != form.Count || attempt.answers.Count != form.Count) return false;
            int correct = 0;
            for (int i = 0; i < form.Count; i++)
            {
                if (attempt.questionIds[i] != form[i].Id || attempt.answers[i] < 0 || attempt.answers[i] >= form[i].Choices.Count) return false;
                if (attempt.answers[i] == form[i].CorrectAnswer) correct++;
            }
            attempt.correct = correct; attempt.total = form.Count; attempt.stage = stage.ToString(); return true;
        }
    }
}
