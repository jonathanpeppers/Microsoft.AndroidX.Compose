"""Check SharedStateLifetime's eager reflection contract in final APK DEX files."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile


def declared_fields(data, methods=None):
    """Read class_data fields, not field_ids (which also includes references)."""
    if data[:4] != b"dex\n" or data[4:8] not in (b"035\0", b"037\0", b"038\0", b"039\0", b"040\0"):
        raise ValueError("Unsupported DEX format.")
    if struct.unpack_from("<I", data, 40)[0] != 0x12345678:
        raise ValueError("Unsupported DEX endianness.")

    def u32(offset):
        return struct.unpack_from("<I", data, offset)[0]

    def uleb(offset):
        result = 0
        for shift in range(0, 35, 7):
            value = data[offset]
            offset += 1
            result |= (value & 127) << shift
            if value < 128:
                return result, offset
        raise ValueError("Invalid DEX ULEB128.")

    count, offset = struct.unpack_from("<II", data, 56)
    strings = []
    for index in range(count):
        _, start = uleb(u32(offset + index * 4))
        # Contract identifiers are ASCII; other strings can contain DEX MUTF-8.
        strings.append(data[start:data.index(0, start)].decode("utf-8", errors="replace"))
    count, offset = struct.unpack_from("<II", data, 64)
    types = [strings[u32(offset + index * 4)] for index in range(count)]
    _, fields_offset = struct.unpack_from("<II", data, 80)
    _, methods_offset = struct.unpack_from("<II", data, 88)
    _, protos_offset = struct.unpack_from("<II", data, 72)
    count, classes_offset = struct.unpack_from("<II", data, 96)
    classes = {}
    for index in range(count):
        start = classes_offset + index * 32
        owner = types[u32(start)]
        fields = {}
        classes[owner] = fields
        position = u32(start + 24)
        if not position:
            continue
        sizes = []
        for _ in range(4):
            size, position = uleb(position)
            sizes.append(size)
        for size in sizes[:2]:
            field_index = 0
            for _ in range(size):
                delta, position = uleb(position)
                access, position = uleb(position)
                field_index += delta
                owner_index, type_index, name_index = struct.unpack_from(
                    "<HHI", data, fields_offset + field_index * 8)
                if types[owner_index] != owner:
                    raise ValueError("DEX field is declared on a different class.")
                fields[strings[name_index]] = {"type": types[type_index], "access": access}
        if methods is not None:
            declared = methods.setdefault(owner, [])
            for size in sizes[2:]:
                method_index = 0
                for _ in range(size):
                    delta, position = uleb(position)
                    access, position = uleb(position)
                    _, position = uleb(position)
                    method_index += delta
                    owner_index, proto_index, name_index = struct.unpack_from(
                        "<HHI", data, methods_offset + method_index * 8)
                    if types[owner_index] != owner:
                        raise ValueError("DEX method is declared on a different class.")
                    _, return_index, parameters_offset = struct.unpack_from(
                        "<III", data, protos_offset + proto_index * 12)
                    parameters = ""
                    if parameters_offset:
                        for parameter in range(u32(parameters_offset)):
                            type_index = struct.unpack_from("<H", data, parameters_offset + 4 + parameter * 2)[0]
                            parameters += types[type_index]
                    declared.append({"name": strings[name_index],
                                     "signature": "(" + parameters + ")" + types[return_index],
                                     "access": access})
    return classes


def reflection_contract(source):
    imports = dict((name.rsplit(".", 1)[1], name) for name in
                   re.findall(r"^import ([\w.]+);", source, re.MULTILINE))
    contract = []
    for owner, field in re.findall(r'\bfield\(\s*([\w.]+)\.class,\s*"(\w+)"\s*\)', source):
        owner = imports.get(owner, owner)
        if "." not in owner:
            raise ValueError(f"Unresolved reflection owner: {owner}")
        contract.append(("L" + owner.replace(".", "/") + ";", field))
    if not contract:
        raise ValueError("No reflection contract found in SharedStateLifetime.java.")
    return contract


def backend_selector_is_preserved(classes):
    field = classes.get("Landroidx/compose/runtime/ComposeRuntimeFlags;", {}).get(
        "isLinkBufferComposerEnabled")
    return field is not None and field["type"] == "Z" and field["access"] & 9 == 9


def confirm_getter_is_preserved(methods):
    return any(method["name"] == "getConfirmStateChange$material3"
               and method["signature"] == "()Lkotlin/jvm/functions/Function1;"
               and not method["access"] & 8
               for method in methods.get("Landroidx/compose/material3/DrawerState;", []))


def inspect(apk_path, contract, require_smoke_harness=False):
    classes = {}
    methods = {}
    dex_names = []
    with zipfile.ZipFile(apk_path) as apk:
        for name in apk.namelist():
            if re.fullmatch(r"classes\d*\.dex", name):
                dex_names.append(name)
                for owner, fields in declared_fields(apk.read(name), methods if require_smoke_harness else None).items():
                    if owner in classes:
                        raise ValueError(f"Duplicate DEX class: {owner}")
                    classes[owner] = fields
    if not dex_names:
        raise ValueError(f"No DEX files in {apk_path}")
    fields = []
    for owner, name in contract:
        member = classes.get(owner, {}).get(name)
        fields.append({
            "class": owner, "field": name, "present": member is not None,
            "declaration": member,
        })
    helper_present = "Lcomposenet/compose/SharedStateLifetime;" in classes
    selector_present = backend_selector_is_preserved(classes)
    getter_present = confirm_getter_is_preserved(methods) if require_smoke_harness else None
    return {
        "apk": str(apk_path.resolve()),
        "sha256": hashlib.sha256(apk_path.read_bytes()).hexdigest(),
        "dex": dex_names,
        "helper_present": helper_present,
        "fields": fields,
        "smoke_harness_required": require_smoke_harness,
        "backend_selector_present": selector_present,
        "confirm_getter_present": getter_present,
        "passed": helper_present and all(field["present"] for field in fields)
                  and (not require_smoke_harness or (selector_present and getter_present)),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path, nargs="+")
    parser.add_argument("--smoke-harness", action="store_true",
                        help="Also check the smoke runner's backend switch and JNI veto getter.")
    args = parser.parse_args()
    source = Path(__file__).resolve().parents[1] / "src" / "Microsoft.AndroidX.Compose" / "Java" / "SharedStateLifetime.java"
    contract = reflection_contract(source.read_text(encoding="utf-8"))
    results = [inspect(path, contract, args.smoke_harness) for path in args.apk]
    print(json.dumps(results, indent=2))
    return 0 if all(result["passed"] for result in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
