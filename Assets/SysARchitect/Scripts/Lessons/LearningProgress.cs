using System;
using System.Collections.Generic;
using UnityEngine;

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
    }
    public sealed class LearningProgress
    {
        private readonly IProgressStorage storage;
        private readonly ProgressData data;
        public int CompletedCount => data.completedLessons.Count;
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
        }
        public bool IsComplete(LessonId id) => data.completedLessons.Contains(id.ToString());
        public void Complete(LessonId id)
        {
            LessonCatalog.Get(id); if (IsComplete(id)) return;
            data.completedLessons.Add(id.ToString()); storage.Write(JsonUtility.ToJson(data));
        }
    }
}
