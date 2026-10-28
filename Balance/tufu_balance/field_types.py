"""Serialized field types per script class, used to decode enum ints into names."""

import re
from pathlib import Path

from .csharp_enums import COMMENT

FIELD = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*(?:public|private|protected|internal)\s+(?:readonly\s+)?"
    r"([\w.]+(?:<[\w.<>, ]+>)?(?:\[\])?)\s+(\w+)\s*(?:=[^;]*)?;",
    re.M)
TYPE_DECL = re.compile(r"\b(?:class|struct)\s+(\w+)")
LIST_TYPE = re.compile(r"^(?:List|IList|IReadOnlyList)<\s*([\w.]+)\s*>$")


def element_type(type_name):
    """'List<StatModifier>' / 'StatModifier[]' -> ('StatModifier', True); 'int' -> ('int', False)."""
    match = LIST_TYPE.match(type_name)
    if match:
        return match.group(1).split(".")[-1], True
    if type_name.endswith("[]"):
        return type_name[:-2].split(".")[-1], True
    return type_name.split(".")[-1], False


class FieldTypes:
    """Approximates Unity serialization: fields of each class/struct declared in a file,
    attributed to the nearest preceding type declaration. Good enough for enum decoding."""

    def __init__(self, scripts_root, enum_decoder):
        self.enums = enum_decoder
        self.fields = {}
        for path in sorted(Path(scripts_root).rglob("*.cs")):
            text = COMMENT.sub("", path.read_text(encoding="utf-8", errors="replace"))
            declarations = [(m.start(), m.group(1)) for m in TYPE_DECL.finditer(text)]
            if not declarations:
                continue
            for match in FIELD.finditer(text):
                type_name, field = match.group(1), match.group(2)
                if type_name in ("static", "const", "event", "return", "new"):
                    continue
                owner = None
                for start, name in declarations:
                    if start < match.start():
                        owner = name
                if owner:
                    self.fields.setdefault(owner, {}).setdefault(field, type_name)

    def _enum(self, type_name):
        variants = self.enums.by_name.get(type_name)
        if variants and len(variants) == 1:
            return next(iter(variants.values()))
        return None

    def decode(self, class_name, value):
        """Return a copy of value with '<field>__name' siblings for enum-typed fields."""
        if isinstance(value, list):
            return [self.decode(class_name, item) for item in value]
        if not isinstance(value, dict) or class_name not in self.fields:
            return value
        result = {}
        for key, item in value.items():
            type_name = self.fields[class_name].get(key)
            if type_name is None:
                result[key] = item
                continue
            element, is_list = element_type(type_name)
            members = self._enum(element)
            if members is not None:
                result[key] = item
                if is_list and isinstance(item, list):
                    result[f"{key}__name"] = [members.get(v, f"?{v}") if isinstance(v, int) else v for v in item]
                elif isinstance(item, int):
                    result[f"{key}__name"] = members.get(item, f"?{item}")
            else:
                result[key] = self.decode(element, item)
        return result
