"""Import an Instagram data download and hand it to the legacy exporter.

Instagram exports differ between download versions. This bridge deliberately
normalises each inbox thread before the existing GPT exporter sees it. Every
message carries a stable thread id, and pagination duplicates are removed per
thread, so messages from similarly named people are not merged.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path
from typing import Any, Iterable


MEDIA_FIELDS = {
    "photos": "image",
    "videos": "video",
    "audio_files": "audio",
    "audio": "audio",
    "files": "file",
    "gifs": "emoji",
    "stickers": "emoji",
    "sticker": "emoji",
}
PLACEHOLDERS = {
    "image": "[图片]",
    "video": "[视频]",
    "audio": "[语音]",
    "emoji": "[表情包]",
    "file": "[文件]",
}
MEDIA_SUFFIXES = {".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".m4a", ".mp3", ".aac", ".wav", ".opus", ".pdf", ".zip"}


def _text(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, (str, int, float)):
        return str(value).strip()
    return ""


def _timestamp(value: Any) -> str | None:
    try:
        number = float(value)
        if number > 10_000_000_000:
            number /= 1000
        return datetime.fromtimestamp(number, timezone.utc).isoformat()
    except (TypeError, ValueError, OSError, OverflowError):
        return _text(value) or None


def _safe_name(value: str) -> str:
    value = re.sub(r"[^0-9A-Za-z_.-]+", "_", value).strip("._")
    return value or "instagram-export"


def _iter_values(value: Any) -> Iterable[Any]:
    if isinstance(value, list):
        return value
    if value in (None, ""):
        return []
    return [value]


def _path_candidates(value: Any, source_dir: Path, export_root: Path) -> list[Path]:
    if isinstance(value, dict):
        raw = value.get("uri") or value.get("path") or value.get("filename") or value.get("name") or value.get("url")
    else:
        raw = value
    raw_text = _text(raw).replace("\\", "/")
    if not raw_text:
        return []
    candidates = [source_dir / raw_text, export_root / raw_text.lstrip("/"), Path(raw_text)]
    if Path(raw_text).name:
        candidates.extend([source_dir / Path(raw_text).name, export_root / Path(raw_text).name])
    return candidates


def _resolve_media(value: Any, source_dir: Path, export_root: Path) -> Path | None:
    for candidate in _path_candidates(value, source_dir, export_root):
        try:
            if candidate.is_file():
                return candidate.resolve()
        except OSError:
            continue
    basename = Path(_text(value)).name
    if basename:
        matches = [path for path in export_root.rglob(basename) if path.is_file()]
        if len(matches) == 1:
            return matches[0].resolve()
    return None


def _media_item(kind: str, value: Any, source_dir: Path, export_root: Path) -> dict[str, Any]:
    resolved = _resolve_media(value, source_dir, export_root)
    name = Path(_text(value)).name or (resolved.name if resolved else None)
    return {
        "kind": kind,
        "type": kind,
        "placeholder": PLACEHOLDERS[kind],
        "source": _text(value) or None,
        "path": str(resolved) if resolved else None,
        "resolved_path": str(resolved) if resolved else None,
        "name": name,
        "exists": resolved is not None,
        "origin": "instagram-export",
    }


def _thread_id(path: Path, export_root: Path) -> str:
    try:
        relative = path.parent.relative_to(export_root).as_posix()
    except ValueError:
        relative = path.parent.name
    return relative or path.parent.name or path.stem


def _thread_name(payload: dict[str, Any], path: Path, thread_id: str) -> str:
    title = _text(payload.get("title") or payload.get("conversation") or payload.get("thread_name"))
    if title:
        return title
    participants = payload.get("participants")
    names: list[str] = []
    if isinstance(participants, list):
        for participant in participants:
            if isinstance(participant, dict):
                name = _text(participant.get("name") or participant.get("username") or participant.get("user_name"))
            else:
                name = _text(participant)
            if name and name not in names:
                names.append(name)
    if names:
        return "、".join(names)
    return path.parent.name or thread_id


def _message_media(message: dict[str, Any], source_dir: Path, export_root: Path) -> list[dict[str, Any]]:
    attachments: list[dict[str, Any]] = []
    for field, kind in MEDIA_FIELDS.items():
        for value in _iter_values(message.get(field)):
            attachments.append(_media_item(kind, value, source_dir, export_root))
    return attachments


def _normalise_message(message: dict[str, Any], conversation: str, thread_id: str, source_dir: Path, export_root: Path) -> dict[str, Any]:
    attachments = _message_media(message, source_dir, export_root)
    content = _text(message.get("content") or message.get("text") or message.get("message"))
    if not content and message.get("is_unsent"):
        content = "[已撤回消息]"
    if not content and message.get("type"):
        content = "[Instagram 系统消息]"
    if not content and attachments:
        content = "\n".join(item["placeholder"] for item in attachments)
    if not content:
        content = "[空消息]"
    timestamp = _timestamp(message.get("timestamp_ms") or message.get("timestamp") or message.get("created_at"))
    sender = _text(message.get("sender_name") or message.get("sender") or message.get("from") or message.get("username")) or "未知用户"
    stable = {
        "thread_id": thread_id,
        "sender": sender,
        "timestamp": timestamp,
        "content": content,
        "media": [(item["kind"], item.get("source"), item.get("name")) for item in attachments],
    }
    record = {
        "timestamp": timestamp,
        "sender": sender,
        "speaker": sender,
        "conversation": conversation,
        "conversation_id": thread_id,
        "content": content,
        "text": content,
        "attachments": attachments,
        "type": "text" if not attachments else attachments[0]["kind"],
        "instagram_source": stable,
        "raw": message,
    }
    return record


class _InstagramHTMLParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.lines: list[str] = []
        self.links: list[str] = []

    def handle_data(self, data: str) -> None:
        value = " ".join(data.split())
        if value:
            self.lines.append(value)

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        for key, value in attrs:
            if key.lower() == "href" and value:
                self.links.append(value)


def _parse_html(path: Path, export_root: Path) -> tuple[str, list[dict[str, Any]]]:
    parser = _InstagramHTMLParser()
    parser.feed(path.read_text(encoding="utf-8", errors="replace"))
    lines = list(dict.fromkeys(parser.lines))
    media = [_media_item("image" if Path(link).suffix.lower() in {".jpg", ".jpeg", ".png", ".gif", ".webp"} else "video", link, path.parent, export_root) for link in parser.links if Path(link).suffix.lower() in MEDIA_SUFFIXES]
    content = "\n".join(lines) or "[Instagram HTML 消息]"
    thread_id = _thread_id(path, export_root)
    conversation = path.parent.name or thread_id
    record = {
        "timestamp": None,
        "sender": "未知用户",
        "speaker": "未知用户",
        "conversation": conversation,
        "conversation_id": thread_id,
        "content": content,
        "text": content,
        "attachments": media,
        "type": "text" if not media else media[0]["kind"],
        "instagram_source": {"thread_id": thread_id, "file": str(path.resolve())},
        "raw": {"html_file": str(path.resolve())},
    }
    return thread_id, [record]


def _json_files(input_path: Path) -> list[Path]:
    if input_path.is_file():
        return [input_path]
    result: list[Path] = []
    for path in input_path.rglob("*"):
        if not path.is_file():
            continue
        name = path.name.casefold()
        if path.suffix.casefold() in {".json", ".html", ".htm"} and (name.startswith("message") or "inbox" in path.as_posix().casefold()):
            result.append(path)
    return sorted(result)


def load_instagram_records(input_path: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    export_root = input_path if input_path.is_dir() else input_path.parent
    files = _json_files(input_path)
    records: list[dict[str, Any]] = []
    seen: set[str] = set()
    candidate_count = 0
    threads: dict[str, dict[str, Any]] = {}
    for path in files:
        thread_id = _thread_id(path, export_root)
        try:
            if path.suffix.casefold() in {".html", ".htm"}:
                current_id, current_records = _parse_html(path, export_root)
                thread_id = current_id
            else:
                payload = json.loads(path.read_text(encoding="utf-8-sig"))
                if isinstance(payload, list):
                    payload = {"messages": payload}
                if not isinstance(payload, dict):
                    continue
                items = payload.get("messages")
                if not isinstance(items, list):
                    continue
                conversation = _thread_name(payload, path, thread_id)
                current_records = [_normalise_message(item, conversation, thread_id, path.parent, export_root) for item in items if isinstance(item, dict)]
            bucket = threads.setdefault(thread_id, {"name": current_records[0].get("conversation", thread_id) if current_records else thread_id, "files": [], "messages": 0})
            bucket["files"].append(str(path.resolve()))
            for record in current_records:
                candidate_count += 1
                fingerprint = json.dumps(record.get("instagram_source", record), ensure_ascii=False, sort_keys=True, default=str)
                if fingerprint in seen:
                    continue
                seen.add(fingerprint)
                records.append(record)
                bucket["messages"] += 1
        except Exception as exc:  # noqa: BLE001 - keep other threads exportable
            threads.setdefault(thread_id, {"name": thread_id, "files": [], "messages": 0}).setdefault("errors", []).append(f"{path}: {type(exc).__name__}: {exc}")
    records.sort(key=lambda item: (item.get("conversation_id", ""), item.get("timestamp") or "", item.get("speaker", "")))
    return records, {
        "source_type": "instagram-export",
        "input": str(input_path.resolve()),
        "files_seen": len(files),
        "records_emitted": len(records),
        "duplicate_records_removed": max(0, candidate_count - len(records)),
        "threads": list(threads.values()),
    }


def _write_normalised(path: Path, records: list[dict[str, Any]]) -> None:
    path.write_text(json.dumps({"messages": records}, ensure_ascii=False, indent=2, default=str) + "\n", encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Convert Instagram message exports to the existing GPT exporter format")
    parser.add_argument("--input", required=True)
    parser.add_argument("--legacy-core", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--conversation")
    parser.add_argument("--user-speaker")
    parser.add_argument("--assistant-speaker")
    parser.add_argument("--transcript-map")
    parser.add_argument("--transcriber")
    parser.add_argument("--copy-media", action="store_true")
    args = parser.parse_args(argv)
    input_path = Path(args.input).expanduser()
    output_dir = Path(args.output_dir).expanduser()
    records, report = load_instagram_records(input_path)
    if not records:
        print(json.dumps({"ok": False, "status": "partial", "source_type": "instagram-export", "error": "未找到 Instagram message_*.json/html 或可读消息"}, ensure_ascii=False))
        return 2
    output_dir.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="instagram-normalized-") as temporary:
        normalized = Path(temporary) / "instagram.normalized.json"
        _write_normalised(normalized, records)
        command = [str(Path(args.legacy_core).resolve()), "--input", str(normalized), "--output-dir", str(output_dir)]
        for name, value in (("--conversation", args.conversation), ("--user-speaker", args.user_speaker), ("--assistant-speaker", args.assistant_speaker), ("--transcript-map", args.transcript_map), ("--transcriber", args.transcriber)):
            if value:
                command.extend((name, value))
        if args.copy_media:
            command.append("--copy-media")
        completed = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    manifest_path = output_dir / "export_manifest.json"
    if manifest_path.exists():
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["source_type"] = "instagram-export"
            manifest["instagram_import"] = report
            manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        except (OSError, json.JSONDecodeError):
            pass
    if completed.stdout:
        try:
            summary = json.loads(completed.stdout.strip().splitlines()[-1])
            summary["source_type"] = "instagram-export"
            summary["instagram_threads"] = len(report.get("threads", []))
            print(json.dumps(summary, ensure_ascii=False))
        except json.JSONDecodeError:
            print(completed.stdout, end="")
    if completed.stderr:
        print(completed.stderr, file=sys.stderr, end="")
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
