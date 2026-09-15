from __future__ import annotations

import hashlib
import json
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from release_lib.runtime_layout import SHARED_MODULES, VARIANT_MODULES, read_module_manifest, validate_runtime_payload


class RuntimeLayoutTests(unittest.TestCase):
    def setUp(self) -> None:
        self.files = {
            "STS2-RitsuLib.dll": b"MZ loader fixture",
            "viewer/index.html": b"<html></html>",
            "RitsuLib.References.props": b"<Project/>",
            "mod_manifest.json": b'{"id":"STS2-RitsuLib","has_dll":true}',
        }

        def modules(directory: str, names: tuple[str, ...]) -> list[dict]:
            entries = []
            for name in names:
                payload = b"MZ module fixture " + name.encode()
                path = f"{directory}/{name}.dll"
                self.files[path] = payload
                self.files[path[:-4] + ".xml"] = b"<doc/>"
                entries.append({"assembly": name, "sha256": hashlib.sha256(payload).hexdigest()})
            return entries

        self.manifest = {
            "schema": 2,
            "shared": modules("shared", SHARED_MODULES),
            "variants": [
                {"compatTarget": target, "files": modules("compat/" + target, VARIANT_MODULES)}
                for target in ("0.107.1", "0.111.0")
            ],
        }

    def validate(self) -> dict:
        self.files["ritsulib-variants.manifest"] = json.dumps(self.manifest).encode()
        return validate_runtime_payload(self.files.__getitem__)

    def test_complete_payload(self) -> None:
        self.assertEqual(len(self.validate()["variants"]), 2)

    def test_utf8_bom(self) -> None:
        payload = b"\xef\xbb\xbf" + json.dumps(self.manifest).encode()
        self.assertEqual(read_module_manifest(payload)["schema"], 2)

    def test_monolithic_schema_is_rejected(self) -> None:
        self.manifest["schema"] = 1
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_missing_shared_module_is_rejected(self) -> None:
        self.manifest["shared"].pop()
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_corrupt_shared_binary_is_rejected(self) -> None:
        self.files["shared/STS2-RitsuLib.Ui.dll"] += b"corrupt"
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_corrupt_nonlatest_variant_is_rejected(self) -> None:
        self.files["compat/0.107.1/STS2-RitsuLib.Runtime.dll"] += b"corrupt"
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_traversal_name_is_rejected(self) -> None:
        self.manifest["shared"][0]["assembly"] = "../STS2-RitsuLib.Shared"
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_duplicate_target_is_rejected(self) -> None:
        self.manifest["variants"].append(self.manifest["variants"][0])
        with self.assertRaises(RuntimeError):
            self.validate()

    def test_missing_documentation_is_rejected(self) -> None:
        del self.files["shared/STS2-RitsuLib.Ui.xml"]
        with self.assertRaises(KeyError):
            self.validate()


if __name__ == "__main__":
    unittest.main()
