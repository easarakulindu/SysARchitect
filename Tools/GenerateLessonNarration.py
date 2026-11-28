"""Generate bundled English clips from LessonNarrationSetup.ExportScripts' JSON.

External build-time tools only: Piper 2023.11.14-2, en_US-ljspeech-high, ffmpeg.
No speech engine, model or network dependency is shipped with the application.
"""
import argparse
import array
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import wave


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--piper", required=True, type=Path)
    parser.add_argument("--model", required=True, type=Path)
    parser.add_argument("--scripts", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    manifest = json.loads(args.scripts.read_text())
    args.output.mkdir(parents=True, exist_ok=True)
    # Restrict synthesis to two cores; the application never synthesizes on a phone.
    if hasattr(os, "sched_getaffinity"):
        os.sched_setaffinity(0, sorted(os.sched_getaffinity(0))[:2])
    with tempfile.TemporaryDirectory(prefix="sysarchitect-voice-") as temp:
        work = Path(temp)
        lines = []
        for entry in manifest["clips"]:
            name = Path(entry["resource"]).name
            lines.append(json.dumps({"text": entry["text"], "output_file": str(work / (name + ".wav"))}))
        subprocess.run([str(args.piper.resolve()), "--model", str(args.model.resolve()), "--json-input",
                        "--sentence_silence", "0.25"], input="\n".join(lines) + "\n", text=True, check=True)
        for entry in manifest["clips"]:
            name = Path(entry["resource"]).name
            wav = work / (name + ".wav")
            with wave.open(str(wav)) as sound:
                duration = sound.getnframes() / sound.getframerate()
                if duration < 2:
                    raise ValueError("Incomplete narration: " + name)
                samples = array.array("h", sound.readframes(sound.getnframes()))
                if not samples or max(abs(sample) for sample in samples) < 128:
                    raise ValueError("Silent narration: " + name)
            target = args.output / (name + ".ogg")
            subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-ac", "1",
                            "-c:a", "libvorbis", "-q:a", "4", str(target)], check=True)
            entry["seconds"] = round(duration, 3)
            entry["audioSha256"] = hashlib.sha256(target.read_bytes()).hexdigest()
            print(f"{name}: {duration:.1f} seconds", flush=True)
    manifest["generator"] = "Piper 2023.11.14-2, en_US-ljspeech-high; synthetic English female voice"
    manifest["modelSha256"] = hashlib.sha256(args.model.read_bytes()).hexdigest()
    (args.output / "Transcripts.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")


if __name__ == "__main__":
    main()
