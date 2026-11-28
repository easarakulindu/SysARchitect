using UnityEngine;

namespace SysARchitect.Lessons
{
    /// <summary>Bundled English teaching audio. Playback never advances lessons or simulation time.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class LessonNarration : MonoBehaviour
    {
        private AudioSource speaker;
        private bool paused;
        public bool Muted { get; private set; }
        public bool Paused => paused;
        public bool IsPlaying => speaker != null && speaker.isPlaying;
        public string CurrentResource { get; private set; }
        public AudioClip CurrentClip => speaker != null ? speaker.clip : null;
        private void Awake()
        {
            speaker = GetComponent<AudioSource>();
            speaker.playOnAwake = false; speaker.loop = false; speaker.spatialBlend = 0; speaker.volume = .85f;
        }
        public static string ResourceFor(LessonId lesson, int step) => $"LessonNarration/{lesson}-{step + 1}";
        public static string ScriptFor(LessonDefinition lesson, int step)
        {
            var detail = lesson.StepDetails[step];
            string overview = step == 0 ? lesson.Title + ". " + lesson.Explanation + "\n\n" : "";
            return (overview + $"Step {step + 1} of {lesson.Steps.Count}. {detail.Heading}. " +
                lesson.Steps[step] + "\n\n" + detail.Explanation)
                .Replace(" > ", " to ").Replace("requests/s", "requests per second");
        }
        public void Play(LessonId lesson, int step)
        {
            var definition = LessonCatalog.Get(lesson);
            if (step < 0 || step >= definition.Steps.Count) throw new System.ArgumentOutOfRangeException(nameof(step));
            Stop(); CurrentResource = ResourceFor(lesson, step);
            speaker.clip = Resources.Load<AudioClip>(CurrentResource);
            if (speaker.clip == null) { Debug.LogWarning("Missing lesson narration: " + CurrentResource, this); return; }
            if (!Muted) speaker.Play();
        }
        public void Replay() { if (speaker.clip != null && !Muted) { speaker.Stop(); paused = false; speaker.Play(); } }
        public void TogglePause()
        {
            if (speaker.clip == null || Muted) return;
            if (paused) { speaker.UnPause(); paused = false; }
            else if (speaker.isPlaying) { speaker.Pause(); paused = true; }
            else Replay();
        }
        public void ToggleMute()
        {
            Muted = !Muted;
            speaker.Stop(); paused = false;
            if (!Muted && speaker.clip != null) speaker.Play();
        }
        public void Stop()
        {
            if (speaker != null) speaker.Stop();
            paused = false;
        }
        private void OnDisable() => Stop();
        private void OnApplicationPause(bool suspended)
        { if (suspended && IsPlaying) { speaker.Pause(); paused = true; } }
    }
}
