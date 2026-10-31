using System;
using System.Collections.Generic;

namespace SysARchitect.Quiz
{
    public sealed class QuizSession
    {
        private readonly IReadOnlyList<QuizQuestion> questions;
        private readonly List<int> answers = new List<int>();
        internal bool Recorded;
        public QuizStage Stage { get; }
        public int Index { get; private set; }
        public int Count => questions.Count;
        public int Correct { get; private set; }
        public QuizQuestion Current => questions[Index];
        public IReadOnlyList<QuizQuestion> Questions => questions;
        public IReadOnlyList<int> Answers => answers.AsReadOnly();
        public bool Answered => answers.Count > Index;
        public bool Complete => Index == Count - 1 && Answered;
        public string Feedback { get; private set; }
        public QuizSession(QuizStage stage) { Stage = stage; questions = QuizCatalog.Form(stage); }
        public bool Answer(int choice)
        {
            if (Answered) throw new InvalidOperationException("This question was already answered.");
            if (choice < 0 || choice >= Current.Choices.Count) throw new ArgumentOutOfRangeException(nameof(choice));
            answers.Add(choice); bool correct = choice == Current.CorrectAnswer; if (correct) Correct++;
            Feedback = (correct ? "Correct. " : "Correct answer: " + Current.Choices[Current.CorrectAnswer] + ". ") + Current.Explanation;
            return correct;
        }
        public void Next()
        {
            if (!Answered || Complete) throw new InvalidOperationException("Answer before advancing; completed sessions cannot advance.");
            Index++; Feedback = null;
        }
    }
    [Serializable]
    public sealed class QuizAttempt
    {
        public string stage;
        public int correct, total;
        public string completedUtc;
        public List<string> questionIds = new List<string>();
        public List<int> answers = new List<int>();
    }
}
