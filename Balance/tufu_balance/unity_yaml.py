"""Unity text-serialization (YAML) reading: document splitting, typed loading and GUID lookup."""

import json
import re
from dataclasses import dataclass
from pathlib import Path

import yaml
from yaml.constructor import SafeConstructor
from yaml.cyaml import CParser
from yaml.resolver import BaseResolver

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)( stripped)?\s*$")
GUID_VALUE = re.compile(r"(guid: )([0-9a-fA-F]{32})")
SCRIPT_GUID = re.compile(r"^  m_Script: \{fileID: (-?\d+), guid: ([0-9a-fA-F]{32})", re.M)
META_GUID = re.compile(r"^guid: ([0-9a-fA-F]{32})", re.M)

# Unity class ids that matter for hierarchy reconstruction.
CLASS_GAME_OBJECT = 1
CLASS_TRANSFORM = 4
CLASS_MONO_BEHAVIOUR = 114
CLASS_RECT_TRANSFORM = 224


class UnityResolver(BaseResolver):
    """Only null/int/float are implicit. YAML 1.1 bools (on/off/yes/no), octal and
    sexagesimal ints would corrupt Unity strings, and Unity floats such as 1e+10,
    Infinity and NaN are not floats to PyYAML's default resolver."""


UnityResolver.add_implicit_resolver(
    "tag:yaml.org,2002:null", re.compile(r"^(?:~|null|Null|NULL|)$"), ["~", "n", "N", ""])
UnityResolver.add_implicit_resolver(
    "tag:yaml.org,2002:int", re.compile(r"^[-+]?(?:0|[1-9][0-9]*)$"), list("-+0123456789"))
UnityResolver.add_implicit_resolver(
    "tag:yaml.org,2002:float",
    re.compile(r"^(?:[-+]?(?:[0-9]+\.[0-9]*|\.[0-9]+|[0-9]+)(?:[eE][-+]?[0-9]+)?|[-+]?Infinity|NaN)$"),
    list("-+0123456789.IN"))


class UnityLoader(CParser, SafeConstructor, UnityResolver):
    def __init__(self, stream):
        CParser.__init__(self, stream)
        SafeConstructor.__init__(self)
        UnityResolver.__init__(self)


def _construct_float(loader, node):
    value = loader.construct_scalar(node)
    return float(value)


def _construct_int(loader, node):
    return int(loader.construct_scalar(node))


UnityLoader.add_constructor("tag:yaml.org,2002:float", _construct_float)
UnityLoader.add_constructor("tag:yaml.org,2002:int", _construct_int)


@dataclass
class Document:
    class_id: int
    file_id: int
    stripped: bool
    text: str  # body without the header line

    def script_guid(self):
        match = SCRIPT_GUID.search(self.text)
        return match.group(2).lower() if match else None

    def load(self):
        body = GUID_VALUE.sub(r'\1"\2"', self.text)
        data = yaml.load(body, Loader=UnityLoader)
        if isinstance(data, dict) and len(data) == 1:
            return next(iter(data.values())) or {}
        return data or {}


def iter_documents(path):
    """Yield documents of a Unity YAML file without parsing their bodies."""
    header = None
    lines = []
    with open(path, encoding="utf-8", errors="replace") as handle:
        for line in handle:
            match = HEADER.match(line) if line.startswith("--- ") else None
            if match:
                if header:
                    yield Document(int(header[1]), int(header[2]), bool(header[3]), "".join(lines))
                header, lines = match, []
            elif header:
                lines.append(line)
    if header:
        yield Document(int(header[1]), int(header[2]), bool(header[3]), "".join(lines))


class GuidIndex:
    """Maps every asset GUID under Assets/ to its path, and script GUIDs to class names."""

    def __init__(self, assets_root):
        self.root = Path(assets_root)
        self.paths = {}
        for meta in self.root.rglob("*.meta"):
            try:
                with open(meta, encoding="utf-8", errors="replace") as handle:
                    head = handle.read(400)
            except OSError:
                continue
            match = META_GUID.search(head)
            if match:
                self.paths[match.group(1).lower()] = meta.with_suffix("").relative_to(self.root).as_posix()

    def path(self, guid):
        return self.paths.get(str(guid).lower()) if guid is not None else None

    def script_class(self, guid):
        path = self.path(guid)
        if path and path.endswith(".cs"):
            return Path(path).stem
        return None

    def is_game_script(self, guid):
        path = self.path(guid)
        return bool(path and path.endswith(".cs") and path.startswith("Scripts/"))


REF_KEYS = ({"fileID"}, {"fileID", "guid", "type"})


def is_ref(value):
    return isinstance(value, dict) and set(value) in REF_KEYS


def render_ref(value, index, unresolved=None):
    """Turn a Unity object reference into a readable string, or None for a null reference."""
    file_id = value.get("fileID", 0)
    guid = value.get("guid")
    if guid is None:
        return None if file_id == 0 else f"local:{file_id}"
    path = index.path(guid)
    if path is None and str(guid).startswith("0000000000000000"):
        return f"builtin:{guid}#{file_id}"  # Unity built-in resources
    if path is None:
        if unresolved is not None:
            unresolved.add(str(guid))
        return f"guid:{guid}#{file_id}"
    if file_id in (11400000, 100100000, 11500000) or path.endswith((".asset", ".cs")):
        return path
    return f"{path}#{file_id}"


def resolve_refs(value, index, unresolved=None):
    """Recursively replace references with rendered strings."""
    if is_ref(value):
        return render_ref(value, index, unresolved)
    if isinstance(value, dict):
        return {key: resolve_refs(item, index, unresolved) for key, item in value.items()}
    if isinstance(value, list):
        return [resolve_refs(item, index, unresolved) for item in value]
    return value


UNITY_HEADER_FIELDS = {
    "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
    "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier",
}


def payload(data):
    """MonoBehaviour fields without Unity's bookkeeping header."""
    return {key: value for key, value in data.items() if key not in UNITY_HEADER_FIELDS}


def flatten(value, prefix=""):
    """Yield (path, leaf) pairs; lists use [i], dicts use dotted keys."""
    if isinstance(value, dict):
        if not value:
            yield prefix, None
        for key, item in value.items():
            yield from flatten(item, f"{prefix}.{key}" if prefix else str(key))
    elif isinstance(value, list):
        if not value:
            yield prefix, None
        for i, item in enumerate(value):
            yield from flatten(item, f"{prefix}[{i}]")
    else:
        yield prefix, value


def to_json(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=False, allow_nan=True)
