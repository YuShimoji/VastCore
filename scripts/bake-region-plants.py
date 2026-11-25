"""Bake two perpendicular views of each deterministic plant into the mesa cutout atlas."""

from math import cos, exp, floor, pi, sin, sqrt
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw


TILE = 256
ASPECT = (1.35, 1.65, 1.12, 1.12)  # Must match RegionPlantView.


def tone(rgb, factor):
    return tuple(min(255, max(0, round(value * factor))) for value in rgb)


def plant(kind, rng):
    shapes = []

    def polygon(points, color):
        shapes.append((points, color))

    def stem(start, end, radius):
        # The projected thickness of a twig is independent of viewing direction.
        shapes.append(([start, end], (101, 79, 56, radius)))

    def leaves(tip, size):
        colors = ((0, 0, 0), (128, 139, 109), (84, 102, 65), (141, 153, 66))
        for _ in range((0, 22, 54, 36)[kind]):
            angle = rng.uniform(0, 2 * pi)
            radius = sqrt(rng.random()) * size
            x = tip[0] + cos(angle) * radius
            y = tip[1] + rng.uniform(-0.53, 0.57) * size * (1.5 - radius / size)
            z = tip[2] + sin(angle) * radius
            width = size * rng.uniform(0.065, 0.18)
            length = width * (2.5 if kind != 3 else 1.4)
            color = tone(colors[kind], rng.uniform(0.74, 1.15) + (y - tip[1]) / size * 0.20)
            polygon([(x - width, y, z - width * 0.5), (x, y + length, z),
                     (x + width, y + length * 0.3, z + width), (x, y - width * 0.4, z)], color)

    def branch(start, heading, reach, rise, thickness, depth):
        angle = heading + rng.uniform(-0.30, 0.30)
        mid = (start[0] + 0.48 * reach * cos(angle), start[1] + rise * 0.45,
               start[2] + 0.48 * reach * sin(angle))
        end = (mid[0] + 0.52 * reach * cos(heading), start[1] + rise,
               mid[2] + 0.52 * reach * sin(heading))
        stem(start, mid, thickness)
        stem(mid, end, 0.74 * thickness)
        if depth == 0:
            leaves(end, (0, 0.061, 0.090, 0.103)[kind] * rng.uniform(0.78, 1.24))
            return
        for i in range(3):
            direction = heading + (i - 1) * rng.uniform(0.65, 1.38)
            branch(end if i == 1 else mid, direction, reach * rng.uniform(0.42, 0.77),
                   rise * rng.uniform(0.41, 0.80), thickness * 0.58, depth - 1)

    if kind == 0:
        for _ in range(86):
            angle = rng.uniform(0, 2 * pi)
            reach = rng.uniform(0.10, 0.61)
            h = rng.uniform(0.29, 0.60) + 0.42 * (0.6 - reach)
            dx, dz = cos(angle), sin(angle)
            x, z = rng.uniform(-0.055, 0.055), rng.uniform(-0.055, 0.055)
            width = rng.uniform(0.007, 0.020)
            mid = (x + dx * reach * 0.38, h * 0.68, z + dz * reach * 0.38)
            tip = (x + dx * reach, h, z + dz * reach)
            color = tone(rng.choice([(172, 151, 88), (132, 136, 79), (184, 164, 105)]), rng.uniform(0.74, 1.12))
            polygon([(x - dz * width, 0, z + dx * width), mid, tip,
                     (mid[0] + dz * width, mid[1], mid[2] - dx * width),
                     (x + dz * width, 0, z - dx * width)], color)
            if reach < 0.22 and rng.random() < 0.6:
                polygon([tip, (tip[0] + 0.014, h + 0.055, tip[2] + 0.012),
                         (tip[0] + 0.008, h + 0.12, tip[2] + 0.018)], (190, 161, 101))
    else:
        height = (0, 0.025, 0.14, 0.33)[kind]
        root = (0, 0, 0)
        fork = (rng.uniform(-0.045, 0.045), height, rng.uniform(-0.045, 0.045))
        stem(root, fork, (0, 0.012, 0.032, 0.023)[kind])
        count = (0, 14, 10, 8)[kind]
        for i in range(count):
            angle = i * 2.39996 + rng.uniform(-0.34, 0.34)
            reach = rng.uniform(0.10, 0.29) * (1.35 if kind == 1 else 1.0)
            rise = rng.uniform(0.14, 0.34) * (0.80 if kind == 1 else 1.0)
            offset = rng.uniform(0.02, 0.15) if kind == 1 else rng.uniform(0.02, 0.08)
            base = (fork[0] + offset * cos(angle), height + rng.uniform(0.01, 0.13),
                    fork[2] + offset * sin(angle))
            stem(root if kind == 1 else fork, base, (0, 0.008, 0.018, 0.014)[kind])
            branch(base, angle, reach, rise, (0, 0.007, 0.016, 0.013)[kind], 2 if kind == 1 else 3)
    return shapes


def render(shapes, axis, aspect):
    size = TILE * 2
    canvas = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(canvas)
    points = [p for vertices, _ in shapes for p in vertices]
    height = max(p[1] for p in points)
    radius = max(max(abs(p[0]), abs(p[2])) for p in points)
    scale = (size - 10) / height
    horizontal = min(scale / aspect, (size - 14) / (2 * radius))
    depth = 2 if axis == 0 else 0
    canopy = {}
    for vertices, _ in shapes:
        x, y, z = vertices[0]
        key = (floor(x * 24), floor(z * 24))
        canopy[key] = max(canopy.get(key, 0), y)
    for vertices, color in sorted(shapes, key=lambda shape: sum(p[depth] for p in shape[0]) / len(shape[0])):
        pixels = [(size / 2 + p[axis] * horizontal, size - 3 - p[1] * scale) for p in vertices]
        if len(vertices) == 2:
            draw.line(pixels, fill=color[:3], width=max(1, round(color[3] * 2 * scale)))
        else:
            x, y, z = vertices[0]
            overhead = max(0, canopy[floor(x * 24), floor(z * 24)] - y)
            shade = 0.49 + 0.51 * exp(-4.6 * overhead / height)
            draw.polygon(pixels, fill=tone(color, shade))
    return canvas.resize((TILE, TILE), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    atlas = Image.new("RGBA", (8 * TILE, 4 * TILE))
    for species in range(4):
        for variant in range(4):
            model = plant(species, Random(3701 + 71 * species + variant))
            for face, axis in enumerate((0, 2)):
                atlas.paste(render(model, axis, ASPECT[species]), ((2 * variant + face) * TILE, species * TILE))
    atlas.save(Path(__file__).resolve().parents[1] / "Assets/Resources/RegionMap3D/DesertPlants.png")
