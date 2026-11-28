using NUnit.Framework;
using UnityEngine;
using SysARchitect.Lessons;

namespace SysARchitect.Tests
{
    public class LessonNarrationAssetTests
    {
        [System.Serializable] private sealed class Entry { public string resource; public string text; public string audioSha256; }
        [System.Serializable] private sealed class Manifest { public Entry[] clips; }
        [Test]
        public void EveryStepHasDistinctBundledAudioAndASynchronizedTranscript()
        {
            var text = Resources.Load<TextAsset>("LessonNarration/Transcripts"); Assert.That(text, Is.Not.Null);
            var manifest = JsonUtility.FromJson<Manifest>(text.text); int count = 0;
            var hashes = new System.Collections.Generic.HashSet<string>();
            foreach (var lesson in LessonCatalog.All)
                for (int step = 0; step < lesson.Steps.Count; step++)
                {
                    string path = LessonNarration.ResourceFor(lesson.Id, step);
                    var entry = System.Array.Find(manifest.clips, e => e.resource == path);
                    Assert.That(entry, Is.Not.Null, path);
                    Assert.That(entry.text, Is.EqualTo(LessonNarration.ScriptFor(lesson, step)), "Regenerate audio when teaching text changes");
                    var clip = Resources.Load<AudioClip>(path); Assert.That(clip, Is.Not.Null, path);
                    Assert.That(clip.length, Is.GreaterThan(2)); count++;
                    using (var hash = System.Security.Cryptography.SHA256.Create())
                    {
                        string actual = System.BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(clip)))).Replace("-", "").ToLowerInvariant();
                        Assert.That(actual, Is.EqualTo(entry.audioSha256)); Assert.That(hashes.Add(actual), Is.True, "Steps must have distinct audio");
                    }
                }
            Assert.That(count, Is.EqualTo(20)); Assert.That(manifest.clips.Length, Is.EqualTo(count));
        }
    }
}
