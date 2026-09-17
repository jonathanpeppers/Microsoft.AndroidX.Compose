import runpy
from pathlib import Path
import struct
import unittest


CHECKER = runpy.run_path(str(Path(__file__).with_name("check-shared-state-dex.py")), run_name="checker")


def dex_with_field(declared):
    owner = "Landroidx/compose/runtime/LinkComposer;"
    strings = [owner, "Ljava/lang/Object;", "changeListWriter"]
    string_ids = 112
    type_ids = string_ids + 4 * len(strings)
    field_ids = type_ids + 8
    class_defs = field_ids + 8
    data = bytearray(class_defs + 32)
    data[:8] = b"dex\n039\0"
    struct.pack_into("<I", data, 40, 0x12345678)
    struct.pack_into("<II", data, 56, len(strings), string_ids)
    struct.pack_into("<II", data, 64, 2, type_ids)
    struct.pack_into("<II", data, 80, 1, field_ids)
    struct.pack_into("<II", data, 96, 1, class_defs)
    struct.pack_into("<II", data, type_ids, 0, 1)
    struct.pack_into("<HHI", data, field_ids, 0, 1, 2)
    for index, value in enumerate(strings):
        struct.pack_into("<I", data, string_ids + 4 * index, len(data))
        data.extend(bytes([len(value)]) + value.encode("ascii") + b"\0")
    if declared:
        struct.pack_into("<I", data, class_defs + 24, len(data))
        # One instance field, delta index 0, private final; no methods.
        data.extend(b"\0\1\0\0\0\x12")
    return bytes(data), owner


class DexContractTests(unittest.TestCase):
    def test_field_reference_is_not_a_declaration(self):
        data, owner = dex_with_field(False)
        self.assertEqual(CHECKER["declared_fields"](data)[owner], {})

    def test_instance_field_is_declared_on_its_owner(self):
        data, owner = dex_with_field(True)
        self.assertEqual(CHECKER["declared_fields"](data)[owner], {
            "changeListWriter": {"type": "Ljava/lang/Object;", "access": 18}
        })

    def test_unsupported_dex_fails_explicitly(self):
        with self.assertRaisesRegex(ValueError, "Unsupported DEX format"):
            CHECKER["declared_fields"](b"not dex")

    def test_backend_selector_requires_public_static_boolean(self):
        for field_type, access, expected in (("Z", 9, True), ("Z", 1, False), ("I", 9, False), ("Z", 8, False)):
            with self.subTest(field_type=field_type, access=access):
                classes = {"Landroidx/compose/runtime/ComposeRuntimeFlags;": {
                    "isLinkBufferComposerEnabled": {"type": field_type, "access": access}
                }}
                self.assertEqual(CHECKER["backend_selector_is_preserved"](classes), expected)
        self.assertFalse(CHECKER["backend_selector_is_preserved"]({}))

    def test_contract_includes_both_uninstantiated_backend_paths(self):
        source = Path(__file__).resolve().parents[1] / "src" / "Microsoft.AndroidX.Compose" / "Java" / "SharedStateLifetime.java"
        contract = CHECKER["reflection_contract"](source.read_text(encoding="utf-8"))
        self.assertEqual(len(contract), 11)
        for backend in ("Gap", "Link"):
            self.assertIn((f"Landroidx/compose/runtime/{backend}Composer;", "changeListWriter"), contract)
            for owner, field in (("ComposerChangeListWriter", "changeList"), ("ChangeList", "operations")):
                self.assertIn((f"Landroidx/compose/runtime/composer/{backend.lower()}buffer/changelist/{owner};", field), contract)

    def test_confirm_getter_requires_exact_instance_signature(self):
        for signature, access, expected in (("()Lkotlin/jvm/functions/Function1;", 1, True),
                                            ("()Lkotlin/jvm/functions/Function1;", 9, False),
                                            ("()Ljava/lang/Object;", 1, False)):
            with self.subTest(signature=signature, access=access):
                methods = {"Landroidx/compose/material3/DrawerState;": [{
                    "name": "getConfirmStateChange$material3", "signature": signature, "access": access
                }]}
                self.assertEqual(CHECKER["confirm_getter_is_preserved"](methods), expected)
        self.assertFalse(CHECKER["confirm_getter_is_preserved"]({}))


if __name__ == "__main__":
    unittest.main()
