using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SysARchitect.Lessons;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class LessonNarrationSetup
    {
        [Serializable] private sealed class Script { public string resource; public string text; }
        [Serializable] private sealed class Scripts { public Script[] clips; }
        public static void ExportScripts()
        {
            var entries = LessonCatalog.All.SelectMany(lesson => Enumerable.Range(0, lesson.Steps.Count)
                .Select(step => new Script { resource = LessonNarration.ResourceFor(lesson.Id, step), text = LessonNarration.ScriptFor(lesson, step) })).ToArray();
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "sysarchitect-lesson-narration.json"), JsonUtility.ToJson(new Scripts { clips = entries }, true));
        }
        [MenuItem("SysARchitect/Lessons/Configure English Narration")]
        public static void Configure()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            foreach (var item in previous)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
                    throw new InvalidOperationException("Save open scenes before configuring narration.");
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var ui = UnityEngine.Object.FindAnyObjectByType<LessonUI>();
                var narration = ui.GetComponent<LessonNarration>() ?? ui.gameObject.AddComponent<LessonNarration>();
                narration.GetComponent<AudioSource>().playOnAwake = false;
                Set(ui, "narration", narration);
                var serialized = new SerializedObject(ui);
                if (serialized.FindProperty("replayVoice").objectReferenceValue == null)
                {
                    var panel = ((GameObject)serialized.FindProperty("panel").objectReferenceValue).GetComponent<RectTransform>();
                    Set(ui, "replayVoice", Button(panel, "Replay voice", 12, 400, 232, 48));
                    Set(ui, "pauseVoice", Button(panel, "Pause voice", 252, 400, 232, 48));
                    Set(ui, "muteVoice", Button(panel, "Mute voice", 492, 400, 256, 48));
                    // Desktop has a fixed guide; mobile pins these controls beside the steps.
                    var viewport = panel.Find("Scrollable explanation").GetComponent<RectTransform>();
                    viewport.anchoredPosition = new Vector2(12, -168); viewport.sizeDelta = new Vector2(736, 224);
                }
                EditorSceneManager.SaveScene(scene);
            }
            var audioPaths = AssetDatabase.IsValidFolder("Assets/SysARchitect/Resources/LessonNarration")
                ? AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/SysARchitect/Resources/LessonNarration" }).Select(AssetDatabase.GUIDToAssetPath)
                : Enumerable.Empty<string>();
            foreach (var path in audioPaths)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .65f;
                importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets(); ExportScripts();
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
