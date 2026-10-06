"""Compare actual native files semantically, including every identity and property."""
import json
import sys
from pathlib import Path


def differences(left, right, path=""):
    if isinstance(left, dict) and isinstance(right, dict):
        return [change for key in sorted(left.keys() | right.keys())
                for change in differences(left.get(key), right.get(key), path + "/" + key)]
    if isinstance(left, list) and isinstance(right, list):
        if len(left) != len(right):
            return [{"path": path + "/length", "source": len(left), "saved": len(right)}]
        return [change for index, (a, b) in enumerate(zip(left, right))
                for change in differences(a, b, path + "/" + str(index))]
    return [] if left == right else [{"path": path, "source": left, "saved": right}]


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("Usage: compare-native-documents.py source.json saved.json absent-report.json")
    report = Path(sys.argv[3])
    if report.exists():
        raise SystemExit("Report already exists.")
    source = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    saved = json.loads(Path(sys.argv[2]).read_text(encoding="utf-8-sig"))
    changes = differences(source, saved)
    result = {"source": sys.argv[1], "saved": sys.argv[2], "equal": not changes,
              "sourceCount": len(source["diagram"]["components"]),
              "savedCount": len(saved["diagram"]["components"]), "differences": changes}
    report.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"equal": not changes, "differences": len(changes)}))
    raise SystemExit(1 if changes else 0)
