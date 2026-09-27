#!/usr/bin/env python3
"""Exercise the packed, MAUI-free Compose consumer on an Android API-36 emulator."""

import argparse
import json
import re
import subprocess
import time
import uuid
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def utc_now():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


class Probe:
    def __init__(self, serial, package, evidence_root):
        self.serial = serial
        self.package = package
        run_id = f"{datetime.now(timezone.utc):%Y%m%dT%H%M%SZ}-{uuid.uuid4().hex[:8]}"
        self.output = evidence_root / f"direct-consumer-{run_id}"
        self.output.mkdir(parents=True, exist_ok=False)
        self.remote_prefix = f"/sdcard/Download/compose-direct-consumer-{run_id}"

    def adb(self, *args):
        command = ["adb", "-s", self.serial, *args]
        result = subprocess.run(command, capture_output=True, timeout=90, check=False)
        if result.returncode:
            raise RuntimeError(
                f"{' '.join(command)} failed ({result.returncode}): "
                f"{result.stderr.decode(errors='replace').strip()}"
            )
        return result.stdout

    def dump(self, label):
        safe_label = re.sub(r"[^A-Za-z0-9_-]", "-", label)
        remote = f"{self.remote_prefix}-{safe_label}.xml"
        self.adb("shell", "uiautomator", "dump", remote)
        data = self.adb("exec-out", "cat", remote)
        require(data.lstrip().startswith(b"<?xml"), f"UIAutomator did not produce XML for {label!r}")
        (self.output / f"{safe_label}.xml").write_bytes(data)
        return ET.fromstring(data)

    def screenshot(self, label):
        image = self.adb("exec-out", "screencap", "-p")
        require(image.startswith(b"\x89PNG\r\n\x1a\n"), "Device did not return a PNG screenshot")
        (self.output / f"{label}.png").write_bytes(image)

    def tag(self, root, resource_id):
        node = next(
            (item for item in root.iter("node") if item.attrib.get("resource-id") == resource_id),
            None,
        )
        require(node is not None, f"Missing visible resource-id {resource_id}")
        return node

    def text(self, root, prefix):
        node = next(
            (item for item in root.iter("node") if item.attrib.get("text", "").startswith(prefix)),
            None,
        )
        require(node is not None, f"Missing visible text starting with {prefix!r}")
        return node.attrib["text"]

    def bounds(self, node):
        coordinates = tuple(map(int, re.findall(r"\d+", node.attrib["bounds"])))
        require(len(coordinates) == 4, f"Unexpected bounds for {node.attrib}")
        return coordinates

    def tap(self, node):
        x1, y1, x2, y2 = self.bounds(node)
        self.adb("shell", "input", "tap", str((x1 + x2) // 2), str((y1 + y2) // 2))
        time.sleep(0.8)

    def check_accessibility(self, root, switch_checked, selected_option):
        names = {
            "toggle": ("Example switch", "android.view.View"),
            "level-slider": ("Example level", "android.widget.SeekBar"),
            "option-a": ("Option A", "android.widget.RadioButton"),
            "option-b": ("Option B", "android.widget.RadioButton"),
        }
        for resource_id, (name, role) in names.items():
            node = self.tag(root, resource_id)
            require(node.attrib.get("content-desc") == name, f"{resource_id} is not named {name!r}")
            require(node.attrib.get("class") == role, f"{resource_id} lacks {role} semantics")
        group = self.tag(root, "option-group")
        descendants = {node.attrib.get("resource-id") for node in group.iter("node")}
        require({"option-a", "option-b"} <= descendants, "Radio options are not in the tagged selection group")
        require(
            self.tag(root, "toggle").attrib["checked"] == str(switch_checked).lower(),
            "Switch checked state is missing",
        )
        for option in ("a", "b"):
            checked = option == selected_option
            require(
                self.tag(root, f"option-{option}").attrib["checked"] == str(checked).lower(),
                f"Radio option {option} checked state is missing",
            )

    def scroll_until(self, prefix, maximum=4):
        for attempt in range(maximum):
            root = self.dump(f"scroll-{prefix}-{attempt}")
            if any(node.attrib.get("text", "").startswith(prefix) for node in root.iter("node")):
                return root
            viewport = next(
                (node for node in root.iter("node")
                 if node.attrib.get("class") == "android.widget.ScrollView"),
                None,
            )
            require(viewport is not None, f"No scrollable viewport to reach {prefix!r}")
            x1, y1, x2, y2 = self.bounds(viewport)
            x = (x1 + x2) // 2
            self.adb(
                "shell", "input", "swipe", str(x), str(y1 + (y2 - y1) * 85 // 100),
                str(x), str(y1 + (y2 - y1) * 20 // 100), "500",
            )
            time.sleep(1)
        raise RuntimeError(f"Could not scroll to {prefix!r} after {maximum} attempts")

    def lifecycle(self, pid):
        raw = self.adb("logcat", "-d", "-v", "threadtime", "-s", "ComposeDirectConsumer:I")
        (self.output / "lifecycle.log").write_bytes(raw)
        pattern = re.compile(
            rf"\s{re.escape(pid)}\s+\d+\s+I ComposeDirectConsumer:\s+"
            r"MainActivity\.OnCreate instance=(\d+) restored=(True|False)"
        )
        return [(int(match[1]), match[2] == "True") for match in pattern.finditer(raw.decode())]

    def configuration(self):
        return self.adb("shell", "cmd", "activity", "get-config", "--device").decode().splitlines()[0]


def verify(probe):
    require(probe.serial.startswith("emulator-"), "Only Android emulators may be rotated")
    require(probe.adb("shell", "getprop", "ro.build.version.sdk").strip() == b"36",
            "An Android API-36 emulator is required")
    require(probe.adb("shell", "settings", "get", "system", "accelerometer_rotation").strip() == b"1",
            "Auto-rotate is off; do not change the device setting for this check")
    initial_config = probe.configuration()
    require("-port-" in initial_config, f"Start in portrait orientation: {initial_config}")
    packages = probe.adb("shell", "pm", "list", "packages", probe.package).decode().splitlines()
    require(f"package:{probe.package}" in packages, f"{probe.package} is not installed")
    activity = probe.adb(
        "shell", "cmd", "package", "resolve-activity", "--brief", probe.package
    ).decode().splitlines()[-1].strip()
    require(activity.startswith(f"{probe.package}/"), f"Unexpected resolved activity: {activity}")

    probe.adb("shell", "am", "start", "-n", activity)
    time.sleep(5)
    activities = probe.adb("shell", "dumpsys", "activity", "activities").decode()
    resumed = next(
        (line for line in activities.splitlines() if "topResumedActivity=" in line), ""
    )
    require(
        probe.package in resumed,
        f"{probe.package} is not foreground after launch ({resumed.strip()}); "
        "inspect adb logcat for a runtime crash",
    )
    root = probe.dump("initial")
    require(probe.tag(root, "count-value").attrib.get("text") == "Count: 0",
            "Use a newly installed, unmodified sample; existing data will not be reset")
    require(probe.tag(root, "text-selection").attrib.get("text") == "Selection: 0..0",
            "Expected a fresh text input")
    probe.check_accessibility(root, switch_checked=False, selected_option="a")
    probe.screenshot("initial")
    pid = probe.adb("shell", "pidof", probe.package).decode().strip()
    instances_before = probe.lifecycle(pid)
    require(instances_before, "The sample did not log its initial Activity.OnCreate")

    probe.tap(probe.tag(root, "increment"))
    root = probe.dump("count")
    require(probe.tag(root, "count-value").attrib.get("text") == "Count: 1",
            "Button did not update count")
    probe.tap(probe.tag(root, "text-input"))
    for character in "ABC":
        probe.adb("shell", "input", "text", character)
        time.sleep(0.45)
    root = probe.dump("input")
    require(probe.tag(root, "text-input").attrib.get("text") == "ABC",
            "TextFieldValue did not retain paced input")
    require(probe.tag(root, "text-selection").attrib.get("text") == "Selection: 3..3",
            "IME caret did not reach the end")

    size = probe.adb("shell", "wm", "size").decode()
    match = re.search(r"Physical size:\s*(\d+)x(\d+)", size)
    require(match is not None, f"Cannot determine portrait IME coordinates: {size}")
    width, height = map(int, match.groups())
    probe.adb("shell", "input", "tap", str(width * 94 // 100), str(height * 91 // 100))
    time.sleep(1)
    root = probe.dump("ime")
    require(probe.tag(root, "ime-result").attrib.get("text") == "IME submitted: ABC",
            "Android IME Done did not submit the text")
    probe.adb("shell", "input", "keyevent", "KEYCODE_DPAD_LEFT")
    time.sleep(0.5)
    root = probe.dump("selection")
    require(probe.tag(root, "text-selection").attrib.get("text") == "Selection: 2..2",
            "TextFieldValue selection did not move")
    input_method = probe.adb("shell", "dumpsys", "input_method").decode()
    if "mInputShown=true" in input_method:
        probe.adb("shell", "input", "keyevent", "KEYCODE_BACK")
        time.sleep(0.8)

    root = probe.dump("keyboard-hidden")
    probe.tap(probe.tag(root, "toggle"))
    root = probe.dump("toggle")
    require(probe.text(root, "Switch: ") == "Switch: On", "Switch action did not update state")
    probe.check_accessibility(root, switch_checked=True, selected_option="a")
    x1, y1, x2, y2 = probe.bounds(probe.tag(root, "level-slider"))
    probe.adb(
        "shell", "input", "tap", str(x1 + (x2 - x1) * 84 // 100), str((y1 + y2) // 2)
    )
    time.sleep(0.8)
    root = probe.dump("slider")
    slider_label = probe.text(root, "Slider: ")
    require(float(slider_label.split(": ", 1)[1]) > 0.7,
            f"Slider action did not change value: {slider_label}")
    probe.tap(probe.tag(root, "option-b"))
    root = probe.dump("radio")
    require(probe.text(root, "Selected: ") == "Selected: B", "Radio selection did not update")
    probe.check_accessibility(root, switch_checked=True, selected_option="b")
    probe.tap(probe.tag(root, "open-dialog"))
    root = probe.dump("dialog")
    require(probe.text(root, "Example dialog") == "Example dialog", "Modal did not open")
    probe.screenshot("dialog")
    confirm = next(
        (node for node in root.iter("node") if node.attrib.get("text") == "Confirm"), None
    )
    require(confirm is not None, "Modal confirm button missing")
    probe.tap(confirm)
    root = probe.dump("confirmed")
    require(probe.text(root, "Dialog: ") == "Dialog: Confirmed", "Modal result did not update")
    probe.screenshot("controls-before-recreation")

    probe.tap(probe.tag(root, "open-items"))
    root = probe.dump("items")
    require(probe.tag(root, "first-item").attrib.get("text") == "First item: Item 01",
            "Initial keyed list order is wrong")
    probe.tap(probe.tag(root, "reverse-items"))
    root = probe.dump("reversed")
    require(probe.tag(root, "first-item").attrib.get("text") == "First item: Item 60",
            "Reversed keyed list order is wrong")
    probe.screenshot("items-before-recreation")

    rotated = False
    try:
        probe.adb("emu", "rotate")
        rotated = True
        time.sleep(6)
        require("-land-" in probe.configuration(), "Emulator rotation did not change the configuration")
        after_pid = probe.adb("shell", "pidof", probe.package).decode().strip()
        require(after_pid == pid, "The sample restarted its process instead of recreating the Activity")
        instances_after = probe.lifecycle(pid)
        require(
            len(instances_after) > len(instances_before)
            and instances_after[-1][0] > instances_before[-1][0]
            and instances_after[-1][1],
            f"Activity recreation with savedInstanceState was not observed: {instances_after}",
        )
        root = probe.dump("items-after-recreation")
        require(probe.tag(root, "first-item").attrib.get("text") == "First item: Item 60",
                "Keyed list order or navigation was lost on Activity recreation")
        probe.screenshot("items-after-recreation")
        probe.tap(probe.tag(root, "back-to-controls"))
        root = probe.dump("controls-after-recreation")
        for resource_id, expected in (
            ("count-value", "Count: 1"),
            ("text-input", "ABC"),
            ("text-selection", "Selection: 2..2"),
            ("ime-result", "IME submitted: ABC"),
        ):
            require(probe.tag(root, resource_id).attrib.get("text") == expected,
                    f"{resource_id} was lost on Activity recreation")
        root = probe.scroll_until("Selected: ")
        require(probe.text(root, "Switch: ") == "Switch: On",
                "Switch state was lost on Activity recreation")
        require(probe.text(root, "Slider: ") == slider_label,
                "Slider value was lost on Activity recreation")
        require(probe.text(root, "Selected: ") == "Selected: B",
                "Radio selection was lost on Activity recreation")
        probe.check_accessibility(root, switch_checked=True, selected_option="b")
        probe.screenshot("controls-after-recreation")
        root = probe.scroll_until("Dialog: ")
        require(probe.text(root, "Dialog: ") == "Dialog: Confirmed",
                "Dialog result was lost on Activity recreation")
        return {
            "pid": pid,
            "activityInstancesBefore": instances_before,
            "activityInstancesAfter": instances_after,
            "submittedText": "ABC",
            "selection": "2..2",
            "slider": slider_label,
            "firstItemAfterRecreation": "Item 60",
        }
    finally:
        if rotated:
            for _ in range(3):
                probe.adb("emu", "rotate")
                time.sleep(1)
            time.sleep(3)
            require("-port-" in probe.configuration(), "Could not restore original portrait orientation")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--serial", required=True, help="Android API-36 emulator serial from adb devices -l")
    parser.add_argument("--package", required=True, help="Freshly installed direct-consumer application ID")
    parser.add_argument("--evidence-root", type=Path, default=Path("artifacts/evidence"))
    args = parser.parse_args()
    started = utc_now()
    probe = Probe(args.serial, args.package, args.evidence_root)
    print(f"Start UTC: {started}; evidence: {probe.output}")
    result = verify(probe)
    result.update({"startUtc": started, "endUtc": utc_now(), "serial": args.serial, "package": args.package})
    (probe.output / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    print(f"Evidence: {probe.output}")


if __name__ == "__main__":
    main()
