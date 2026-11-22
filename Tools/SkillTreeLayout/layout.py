#!/usr/bin/env python3
"""Lay out an existing skill graph and export an SVG preview and coordinates."""
import argparse
import csv
import json
import math
import random
import sys
from collections import defaultdict
from html import escape
from pathlib import Path

MODES = ("radial", "constellation", "fan", "cluster", "spiral", "diamond")
PALETTES = {
    "jewel": ("#111827", ["#71c9ee", "#ffad80", "#9adb9b", "#ccadff", "#f5cc72", "#f39baa", "#92aafa"]),
    "antique": ("#181511", ["#e4b47b", "#d77963", "#b9c28c", "#b59ec9", "#e3ca87", "#a8c0bf", "#dba185"]),
    "night": ("#0a101c", ["#56e3e6", "#ff79ae", "#a4f87b", "#a98bff", "#ffda65", "#77a9ff", "#ff946b"]),
}
NODE_WIDTH = 172


def read_graph(path):
    graph = json.loads(path.read_text(encoding="utf-8-sig"))
    groups = graph.get("groups", {})
    rows = graph["nodes"]
    nodes = {str(row["id"]): row for row in rows}
    root = str(graph["root"])
    if len(nodes) != len(rows) or root not in nodes:
        raise ValueError("Node IDs must be unique and root must exist.")
    depth = {}
    visiting = set()

    def visit(key):
        if key in depth:
            return depth[key]
        if key in visiting:
            raise ValueError(f"Cycle at {key}")
        if key not in nodes:
            raise ValueError(f"Unknown node {key}")
        visiting.add(key)
        parents = [str(parent) for parent in nodes[key].get("parents", [])]
        if len(set(parents)) != len(parents):
            raise ValueError(f"Duplicate parent on {key}")
        if (key == root) != (not parents):
            raise ValueError(f"Only the root may have no parents ({key})")
        depth[key] = 0 if not parents else 1 + max(visit(parent) for parent in parents)
        visiting.remove(key)
        return depth[key]

    for key in nodes:
        visit(key)
    return nodes, root, groups, depth


def weights(spec):
    result = []
    for part in spec.split("+"):
        mode, _, raw = part.partition(":")
        amount = float(raw or "1")
        if mode not in MODES or not math.isfinite(amount) or amount <= 0:
            raise ValueError(f"Invalid layout mix: {spec}")
        result.append((mode, amount))
    total = sum(amount for _, amount in result)
    return [(mode, amount / total) for mode, amount in result]


def projection(mode, angle, level, lateral, gap, twist, spread):
    side = lateral * spread
    if mode == "constellation":
        bend = twist + 0.16 * math.sin(level * 1.77)
        radius = level * gap * (1 + 0.1 * math.sin(level * 2.31))
    elif mode == "spiral":
        bend, radius = level * twist, level * gap
    elif mode == "fan":
        bend, radius = side * 0.29, gap * (level + abs(side) * 0.25)
        side *= 0.3
    elif mode == "cluster":
        bend = twist * math.sin(level)
        radius = gap * (level + 0.27 * (-1) ** level)
        side *= 1.45
    elif mode == "diamond":
        bend = 0
        radius = level * gap
        side *= 0.7 if level % 2 else 1.3
    else:
        bend, radius = 0, level * gap
    a = angle + bend
    return (math.cos(a) * radius - math.sin(a) * side * gap * 0.55,
            math.sin(a) * radius + math.cos(a) * side * gap * 0.55)


def place(nodes, root, groups, depth, args):
    rng = random.Random(args.seed)
    per_group = dict(option.split("=", 1) for option in args.group)
    active_groups = list(dict.fromkeys(row.get("group", "default") for key, row in nodes.items() if key != root))
    if per_group.keys() - set(active_groups):
        raise ValueError(f"Unknown branches: {', '.join(sorted(per_group.keys() - set(active_groups)))}")
    divisions = len(active_groups) if args.sweep == 360 else max(1, len(active_groups) - 1)
    buckets = defaultdict(list)
    for key, row in nodes.items():
        if key != root:
            buckets[row.get("group", "default"), depth[key]].append(key)
    coords = {root: (0.0, 0.0)}
    fixed = {root}
    for index, group in enumerate(active_groups):
        config = groups.get(group, {})
        overrides = per_group.get(group)
        mix = weights(overrides or config.get("mix") or args.mix)
        angle = math.radians(config.get("angle", args.rotate + index * args.sweep / divisions))
        gap = args.spacing * config.get("spacing", 1)
        for (bucket, level), ids in buckets.items():
            if bucket != group:
                continue
            for position, key in enumerate(ids):
                manual = nodes[key].get("position")
                if manual is not None:
                    x, y = map(float, manual)
                    if not all(map(math.isfinite, (x, y))):
                        raise ValueError(f"Invalid fixed position for {key}")
                    coords[key] = (x, y)
                    fixed.add(key)
                    continue
                lateral = position - (len(ids) - 1) / 2
                offsets = [(w, projection(m, angle, level, lateral, gap, args.twist, args.spread)) for m, w in mix]
                jitter = args.jitter * gap
                coords[key] = tuple(
                    sum(w * offset[axis] for w, offset in offsets) + rng.uniform(-jitter, jitter) for axis in (0, 1)
                )
    separate(coords, fixed, args.clearance)
    return coords, active_groups


def separate(coords, fixed, clearance):
    keys = list(coords)
    width, height = NODE_WIDTH + clearance, 85 + clearance
    for _ in range(300):
        changed = False
        for i, a in enumerate(keys):
            for b in keys[i + 1:]:
                ax, ay = coords[a]
                bx, by = coords[b]
                dx, dy = bx - ax, by - ay
                overlap_x, overlap_y = width - abs(dx), height - abs(dy)
                if overlap_x <= 0.1 or overlap_y <= 0.1:
                    continue
                if a in fixed and b in fixed:
                    raise ValueError(f"Fixed nodes {a} and {b} overlap.")
                changed = True
                sx, sy = (math.copysign(overlap_x, dx), 0) if overlap_x < overlap_y else (0, math.copysign(overlap_y, dy))
                if a not in fixed:
                    share = 1 if b in fixed else 0.5
                    coords[a] = (ax - share * sx, ay - share * sy)
                if b not in fixed:
                    share = 1 if a in fixed else 0.5
                    coords[b] = (bx + share * sx, by + share * sy)
        if not changed:
            return
    raise ValueError("Could not separate all nodes. Increase spacing or reduce clearance.")


def edge(a, b, style, bend, start_padding, end_padding):
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dy)
    ux, uy = dx / length, dy / length
    x, y = a[0] + ux * start_padding, a[1] + uy * start_padding
    u, v = b[0] - ux * end_padding, b[1] - uy * end_padding
    if style == "straight":
        return f"M{x:.1f},{y:.1f} L{u:.1f},{v:.1f}"
    if style == "angular":
        px = x + (u - x) * 0.38 - uy * bend
        py = y + (v - y) * 0.38 + ux * bend
        return f"M{x:.1f},{y:.1f} L{px:.1f},{py:.1f} L{u:.1f},{v:.1f}"
    mx, my = (x + u) / 2 - uy * bend, (y + v) / 2 + ux * bend
    return f"M{x:.1f},{y:.1f} Q{mx:.1f},{my:.1f} {u:.1f},{v:.1f}"


def svg(nodes, root, config, coords, groups, args):
    background, colors = PALETTES[args.palette]
    group_colors = {group: colors[index % len(colors)] for index, group in enumerate(groups)}
    margin = 130
    left, top = (math.floor(min(pair[axis] for pair in coords.values()) - margin) for axis in (0, 1))
    width = math.ceil(max(x for x, _ in coords.values()) + margin - left)
    height = math.ceil(max(y for _, y in coords.values()) + margin - top)
    result = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{left} {top} {width} {height}">',
              '<defs><marker id="arrow" markerWidth="9" markerHeight="8" refX="8" refY="4" orient="auto" markerUnits="userSpaceOnUse"><path d="M0,0 L8,4 L0,8 Z" fill="white"/></marker></defs>',
              f'<rect x="{left}" y="{top}" width="{width}" height="{height}" fill="{background}"/>']
    if args.rings:
        max_distance = max(math.hypot(*pair) for pair in coords.values())
        for level in range(1, math.ceil(max_distance / args.spacing) + 1):
            result.append(f'<circle r="{level * args.spacing}" fill="none" stroke="#fff" stroke-opacity=".09"/>')
    for key, row in nodes.items():
        group = row.get("group", "default")
        settings = config.get(group, {})
        style = settings.get("line", args.line)
        color = group_colors.get(group, "#eadbb2")
        for index, parent in enumerate(row.get("parents", [])):
            source = str(parent)
            bend = settings.get("curve", args.curve) * (-1 if index % 2 else 1)
            path = edge(coords[source], coords[key], style, bend, 33 if source == root else 21, 24)
            if args.glow:
                result.append(f'<path d="{path}" stroke="{color}" stroke-width="10" stroke-opacity=".13" fill="none"/>')
            result.append(f'<path d="{path}" stroke="{color}" stroke-width="2.4" stroke-opacity=".8" marker-end="url(#arrow)" fill="none"/>')
    result.append('<g font-family="Noto Sans CJK KR, Malgun Gothic, Apple SD Gothic Neo, sans-serif" text-anchor="middle">')
    for key, row in nodes.items():
        x, y = coords[key]
        color = group_colors.get(row.get("group", "default"), "#eadbb2")
        size = 30 if key == root else 19
        shape = row.get("shape", config.get(row.get("group", "default"), {}).get("shape", args.shape))
        label = escape(str(row.get("label", "")))
        title = escape(str(row.get("detail", row.get("label", ""))))
        result.append(f'<g transform="translate({x:.2f} {y:.2f})"><title>{title}</title>')
        if args.glow:
            result.append(f'<circle r="{size+9}" fill="{color}" opacity=".14"/>')
        if shape == "diamond":
            outline = f'<path d="M0,-{size} L{size},0 L0,{size} L-{size},0 Z"'
        elif shape == "hex":
            vertices = " ".join(f"{size*math.cos(math.tau*i/6):.2f},{size*math.sin(math.tau*i/6):.2f}" for i in range(6))
            outline = f'<polygon points="{vertices}"'
        else:
            outline = f'<circle r="{size}"'
        result.append(f'{outline} fill="{background}" stroke="{color}" stroke-width="2.5"/>')
        result.append(f'<text y="5" fill="{color}" font-weight="700" font-size="14">{escape(key)}</text>')
        if label and not args.hide_labels:
            result.append(f'<rect x="-86" y="{size+7}" width="172" height="24" rx="5" fill="{background}" opacity=".92"/>')
            result.append(f'<text y="{size+23}" fill="#edf1f6" font-size="13">{label}</text>')
        result.append('</g>')
    return "\n".join(result + ['</g></svg>'])


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("graph", type=Path)
    parser.add_argument("-o", "--output", type=Path, default=Path("skill-tree"), help="Output path prefix")
    parser.add_argument("--mix", default="constellation+radial:0.5", help="Example: fan:2+spiral:1")
    parser.add_argument("--group", action="append", default=[], metavar="ID=MIX", help="Per-branch mix; repeatable")
    parser.add_argument("--seed", type=int, default=23)
    parser.add_argument("--spacing", type=float, default=260)
    parser.add_argument("--clearance", type=float, default=12)
    parser.add_argument("--jitter", type=float, default=0.25)
    parser.add_argument("--twist", type=float, default=0.18, help="Spiral turn per depth, in radians")
    parser.add_argument("--spread", type=float, default=1.45)
    parser.add_argument("--rotate", type=float, default=-90)
    parser.add_argument("--sweep", type=float, default=360)
    parser.add_argument("--curve", type=float, default=32)
    parser.add_argument("--line", choices=("curve", "straight", "angular"), default="curve")
    parser.add_argument("--shape", choices=("circle", "diamond", "hex"), default="circle")
    parser.add_argument("--palette", choices=PALETTES, default="jewel")
    for flag in ("rings", "glow", "hide-labels"):
        parser.add_argument(f"--{flag}", action="store_true")
    args = parser.parse_args(argv)
    numeric = (args.spacing, args.clearance, args.jitter, args.twist, args.spread, args.rotate, args.sweep, args.curve)
    if not all(map(math.isfinite, numeric)):
        parser.error("Layout options must be finite numbers.")
    if args.spacing <= 0 or args.clearance < 0 or args.jitter < 0 or args.spread < 0 or not 0 < args.sweep <= 360:
        parser.error("Spacing must be positive, sweep must be in (0, 360], and clearance, jitter and spread must be non-negative.")
    try:
        nodes, root, config, depths = read_graph(args.graph)
        coords, groups = place(nodes, root, config, depths, args)
        preview = svg(nodes, root, config, coords, groups, args)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.with_suffix(".svg").write_text(preview, encoding="utf-8")
        with args.output.with_suffix(".csv").open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.writer(stream)
            writer.writerow(("id", "x", "y", "group", "parents", "label"))
            for key, row in nodes.items():
                x, y = coords[key]
                writer.writerow((key, round(x), round(-y), row.get("group", "default"),
                                 ";".join(map(str, row.get("parents", []))), row.get("label", "")))
    except (ValueError, KeyError, TypeError, OSError) as error:
        print(f"Skill tree layout: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
