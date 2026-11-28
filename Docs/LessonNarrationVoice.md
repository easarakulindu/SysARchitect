# English teaching voice

SysARchitect bundles synthetic English narration of its existing five lessons and
20 instruction steps. The first step includes the lesson overview; later steps
read their own heading, instruction and explanation. These are generated recordings,
not a live speaker, live AI tutor or runtime speech service.

Generation uses the standalone [Piper 2023.11.14-2 release](https://github.com/rhasspy/piper/releases/tag/2023.11.14-2)
and `en_US-ljspeech-high`. Its [published model card](https://huggingface.co/rhasspy/piper-voices/blob/main/en/en_US/ljspeech/high/MODEL_CARD)
identifies the [LJ Speech dataset](https://keithito.com/LJ-Speech-Dataset/) as public
domain and describes training from scratch. The engine and model are build-time
tools in `/tmp`, and are not included in the Unity project or application. The
application ships only mono Vorbis recordings, their transcripts and provenance.
The generated manifest records the downloaded model's SHA-256 and every clip's
SHA-256, duration and exact source text.

To regenerate after editing educational content:

1. Run `SysARchitect.Editor.LessonNarrationSetup.ExportScripts` in Unity. This writes
   `sysarchitect-lesson-narration.json` in the OS temporary directory.
2. Obtain the linked standalone Piper release, model and matching `.onnx.json` config.
3. Run `python3 Tools/GenerateLessonNarration.py --piper /path/to/piper --model /path/to/en_US-ljspeech-high.onnx --scripts /tmp/sysarchitect-lesson-narration.json --output Assets/SysARchitect/Resources/LessonNarration`.
4. Run **SysARchitect > Lessons > Configure English Narration** in Unity to import
   compressed mono clips and retain both scenes' existing control references.
5. Run narration asset/UI tests. Preserve existing audio metadata when replacing
   recordings. Text changes require regenerated clips and transcripts together.

Synthesis is restricted to two CPU cores. No paid generation service, voice model
inference, user-text upload or network connection is needed during application use.
