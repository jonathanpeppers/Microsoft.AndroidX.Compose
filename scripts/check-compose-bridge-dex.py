#!/usr/bin/env python3
"""Check packed ComposeBridge JNI contracts and Text compatibility against R8 DEX."""

import argparse
import os
import re
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path

OWNER = "Landroidx/compose/material3/TextKt;"
NAME = "Text--4IGK_g"
COMMON = (
    "Landroidx/compose/ui/Modifier;JJ"
    "Landroidx/compose/ui/text/font/FontStyle;"
    "Landroidx/compose/ui/text/font/FontWeight;"
    "Landroidx/compose/ui/text/font/FontFamily;J"
    "Landroidx/compose/ui/text/style/TextDecoration;"
    "Landroidx/compose/ui/text/style/TextAlign;JIZI"
)
SUFFIX = (
    "Lkotlin/jvm/functions/Function1;"
    "Landroidx/compose/ui/text/TextStyle;"
    "Landroidx/compose/runtime/Composer;III)V"
)
EXPECTED = {
    "String": "(Ljava/lang/String;" + COMMON + "I" + SUFFIX,
    "AnnotatedString": (
        "(Landroidx/compose/ui/text/AnnotatedString;"
        + COMMON
        + "Ljava/util/Map;"
        + SUFFIX
    ),
}


def find_dexdump(explicit):
    if explicit is not None:
        if not explicit.is_file() or not os.access(explicit, os.X_OK):
            raise ValueError(f"Not an executable dexdump: {explicit}")
        return explicit

    for variable in ("ANDROID_SDK_ROOT", "ANDROID_HOME"):
        sdk = os.environ.get(variable)
        if sdk:
            binaries = list((Path(sdk) / "build-tools").glob("*/dexdump"))
            if binaries:
                return max(binaries, key=lambda path: tuple(
                    int(part) for part in re.findall(r"\d+", path.parent.name)
                ))
    executable = shutil.which("dexdump")
    if executable:
        return Path(executable)
    raise ValueError("dexdump not found; set ANDROID_SDK_ROOT or pass --dexdump")


def read_contract(path):
    if not path.is_file():
        raise ValueError(f"Generated bridge rules not found: {path}")
    bridges = []
    for line_number, line in enumerate(path.read_text().splitlines(), start=1):
        if "# bridge " not in line:
            continue
        match = re.fullmatch(r"\s*# bridge ([^|]+)\|([^|]+)\|(\S+)", line)
        if match is None or not match[3].startswith("("):
            raise ValueError(f"Invalid bridge contract at {path}:{line_number}")
        bridges.append((f"L{match[1]};", match[2], match[3]))
    if not bridges or len(bridges) != len(set(bridges)):
        raise ValueError(f"Empty or duplicate bridge contracts in {path}")
    return set(bridges)


def declarations(dexdump, path, owners):
    output = subprocess.run(
        [str(dexdump), "-n", str(path)],
        capture_output=True,
        text=True,
        check=True,
        timeout=180,
    ).stdout
    owner_header = re.compile(r"^  Class descriptor\s+: '([^']+)'$", re.MULTILINE)
    classes = set()
    methods = set()
    for match in owner_header.finditer(output):
        owner = match[1]
        if owner not in owners:
            continue
        if owner in classes:
            raise ValueError(f"Duplicate DEX class {owner} in {path}")
        classes.add(owner)
        end = output.find("\nClass #", match.end())
        section = output[match.end():end if end != -1 else None]
        method_start = section.find("\n  Direct methods")
        if method_start == -1:
            method_start = section.find("\n  Virtual methods")
        if method_start == -1:
            continue
        methods.update(
            (owner, name, signature)
            for name, signature in re.findall(
                r"^\s+name\s+: '([^']+)'\n\s+type\s+: '([^']+)'",
                section[method_start:],
                re.MULTILINE,
            )
        )
    return classes, methods


def check_apk(apk, dexdump, contracts):
    owners = {owner for owner, _, _ in contracts} | {OWNER}
    classes = set()
    found = set()
    with zipfile.ZipFile(apk) as archive, tempfile.TemporaryDirectory(
        prefix="compose-bridge-dex-"
    ) as scratch:
        entries = [
            entry for entry in archive.infolist()
            if re.fullmatch(r"classes(?:[2-9]\d*)?\.dex", entry.filename)
        ]
        if not entries:
            raise ValueError(f"No classes.dex entries in {apk}")
        for entry in entries:
            dex = Path(scratch) / entry.filename
            with archive.open(entry) as source, dex.open("wb") as destination:
                shutil.copyfileobj(source, destination)
            dex_classes, methods = declarations(dexdump, dex, owners)
            if classes & dex_classes:
                raise ValueError(f"Duplicate DEX classes in {apk}: {classes & dex_classes}")
            classes.update(dex_classes)
            found.update(methods)

    missing = sorted(contracts - found)
    print(f"{apk}: {len(contracts) - len(missing)}/{len(contracts)} [ComposeBridge] JNI triples present")
    for owner, name, signature in missing[:20]:
        print(f"  MISSING {owner}|{name}|{signature}")
    if len(missing) > 20:
        print(f"  ... and {len(missing) - 20} more missing JNI triples")

    missing_text = [
        label for label, signature in EXPECTED.items()
        if (OWNER, NAME, signature) not in found
    ]
    for label, signature in EXPECTED.items():
        print(f"{apk}: {NAME}({label}) {'present' if label not in missing_text else 'MISSING'}")
        if label in missing_text:
            print(f"  expected JNI descriptor: {signature}")
    return not missing and not missing_text


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path, help="Signed R8 APK to inspect")
    parser.add_argument("--dexdump", type=Path, help="Android SDK dexdump executable")
    parser.add_argument("--bridge-rules", type=Path, required=True,
                        help="Generated Microsoft.AndroidX.Compose.bridges.pro")
    args = parser.parse_args()
    if not args.apk.is_file():
        parser.error(f"APK not found: {args.apk}")
    return 0 if check_apk(
        args.apk, find_dexdump(args.dexdump), read_contract(args.bridge_rules)
    ) else 1


if __name__ == "__main__":
    raise SystemExit(main())
