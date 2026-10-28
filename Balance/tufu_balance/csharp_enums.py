"""Read C# enum declarations from the reconstructed scripts so numeric data can be decoded."""

import re
from pathlib import Path

NAMESPACE = re.compile(r"\bnamespace\s+([\w.]+)")
ENUM = re.compile(r"\benum\s+(\w+)\s*(?::\s*\w+)?\s*\{(.*?)\}", re.S)
COMMENT = re.compile(r"//[^\n]*|/\*.*?\*/", re.S)
ATTRIBUTE = re.compile(r"\[[^\]]*\]")


def _evaluate(expression, known):
    expression = expression.strip()
    for name, value in sorted(known.items(), key=lambda item: -len(item[0])):
        expression = re.sub(rf"\b{re.escape(name)}\b", str(value), expression)
    if not re.fullmatch(r"[0-9a-fA-FxX\s|&<>()+\-~]+", expression):
        raise ValueError(expression)
    return int(eval(expression, {"__builtins__": {}}))  # digits and bit operators only


def parse_enums(scripts_root):
    """Return rows (enum, namespace, member, value, file) for every enum under scripts_root."""
    root = Path(scripts_root)
    rows = []
    for path in sorted(root.rglob("*.cs")):
        text = COMMENT.sub("", path.read_text(encoding="utf-8", errors="replace"))
        namespace_match = NAMESPACE.search(text)
        namespace = namespace_match.group(1) if namespace_match else ""
        for match in ENUM.finditer(text):
            known, current = {}, -1
            for raw in ATTRIBUTE.sub("", match.group(2)).split(","):
                member = raw.strip()
                if not member:
                    continue
                if "=" in member:
                    name, expression = (part.strip() for part in member.split("=", 1))
                    try:
                        current = _evaluate(expression, known)
                    except (ValueError, SyntaxError):
                        continue
                else:
                    name, current = member, current + 1
                known[name] = current
                rows.append((match.group(1), namespace, name, current, path.relative_to(root.parent).as_posix()))
    return rows


class EnumDecoder:
    def __init__(self, rows):
        self.by_name = {}
        for enum, namespace, member, value, _ in rows:
            self.by_name.setdefault(enum, {}).setdefault(namespace, {})[value] = member

    def members(self, enum, namespace=None):
        variants = self.by_name.get(enum, {})
        if namespace is not None:
            return variants.get(namespace, {})
        if len(variants) != 1:
            raise KeyError(f"enum {enum} is ambiguous or missing: {sorted(variants)}")
        return next(iter(variants.values()))

    def name(self, enum, value, namespace=None):
        if value is None:
            return None
        return self.members(enum, namespace).get(int(value), f"?{value}")
