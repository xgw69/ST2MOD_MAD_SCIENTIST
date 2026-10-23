"""Verify a generated release pack against every source resource, without Godot."""
import hashlib
import json
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def verify(deploy: Path) -> None:
    data = (deploy / "MadScientist.pck").read_bytes()
    assert data[:4] == b"GDPC" and struct.unpack_from("<I", data, 4)[0] == 3, "PCK v3 header"
    assert struct.unpack_from("<3I", data, 8) == (4, 5, 1), "Godot version"
    file_base, directory = struct.unpack_from("<QQ", data, 24)
    assert 112 <= file_base <= directory < len(data), "PCK offsets"
    count = struct.unpack_from("<I", data, directory)[0]
    pos = directory + 4
    entries = {}
    for _ in range(count):
        length = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        name = data[pos:pos + length].decode("utf-8").rstrip("\0")
        pos += length
        offset, size = struct.unpack_from("<QQ", data, pos)
        digest = data[pos + 16:pos + 32]
        flags = struct.unpack_from("<I", data, pos + 32)[0]
        pos += 36
        start = file_base + offset
        assert flags == 0 and file_base <= start <= start + size <= directory, name
        payload = data[start:start + size]
        assert hashlib.md5(payload).digest() == digest, "PCK checksum: " + name
        assert name not in entries, "Duplicate pack path: " + name
        entries[name] = payload
    assert pos == len(data), "Unexpected trailing PCK bytes"
    expected = set()
    images = texts = scenes = 0
    for source in sorted((ROOT / "MadScientist").rglob("*")):
        if not source.is_file():
            continue
        name = source.relative_to(ROOT).as_posix()
        if source.suffix == ".png":
            importer = name + ".import"
            expected.add(importer)
            config = entries[importer].decode("utf-8")
            assert f'source_file="res://{name}"' in config, importer
            targets = re.findall(r'^path(?:\.[\w]+)?="res://([^"]+)"', config, re.M)
            assert targets, "Missing texture remap: " + importer
            for target in targets:
                expected.add(target)
                assert len(entries[target]) > 32, "Empty imported texture: " + target
            images += 1
        else:
            expected.add(name)
            assert entries[name] == source.read_bytes(), "Resource differs from source: " + name
            texts += 1
    for source in sorted((ROOT / "scenes").rglob("*")):
        if source.is_file():
            name = "MadScientist/scenes/" + source.relative_to(ROOT / "scenes").as_posix()
            expected.add(name)
            assert entries[name] == source.read_bytes(), "Scene differs from source: " + name
            scenes += 1
    assert set(entries) == expected, "Pack contains missing or unexpected resources"
    assert (deploy / "MadScientist.dll").read_bytes()[:2] == b"MZ", "Missing PE assembly"
    assert json.loads((deploy / "MadScientist.json").read_text(encoding="utf-8-sig")) == json.loads(
        (ROOT / "MadScientist.json").read_text(encoding="utf-8-sig")), "Manifest differs"
    print(f"PASS: {count} PCK entry checksums; {images} image remaps, {texts} source resources, {scenes} scenes.")
    print("PASS: release DLL header and manifest; no gameplay/rendering/save claim.")
    for name in ("MadScientist.dll", "MadScientist.pck", "MadScientist.json"):
        file = deploy / name
        print(f"{name}: {file.stat().st_size} bytes; SHA256 {hashlib.sha256(file.read_bytes()).hexdigest()}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python tests/validate_release.py <isolated-release-directory>")
    verify(Path(sys.argv[1]))
