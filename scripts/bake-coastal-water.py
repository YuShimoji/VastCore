"""Bake periodic foam and tangent-space ripple textures for DS-013 (NumPy and Pillow)."""

import argparse
from pathlib import Path

import numpy as np
from PIL import Image


SIZE = 512
GRID = np.arange(SIZE, dtype=np.float32) / SIZE
X, Y = np.meshgrid(GRID, GRID)


def smooth(lo, hi, value):
    blend = np.clip((value - lo) / (hi - lo), 0.0, 1.0)
    return blend * blend * (3.0 - 2.0 * blend)


def noise(rng, cells):
    field = rng.random((cells, cells), dtype=np.float32)
    x, y = X * cells, Y * cells
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    u, v = smooth(0, 1, x - ix), smooth(0, 1, y - iy)
    right, top = (ix + 1) % cells, (iy + 1) % cells
    bottom = field[iy, ix] * (1 - u) + field[iy, right] * u
    upper = field[top, ix] * (1 - u) + field[top, right] * u
    return bottom * (1 - v) + upper * v


def foam_mask():
    rng = np.random.default_rng(71561)
    count = 27
    sites = rng.uniform(0.12, 0.88, (count, count, 2))
    x = (X + (noise(rng, 9) - 0.5) * 0.071) * count
    y = (Y + (noise(rng, 10) - 0.5) * 0.064) * count
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    first = np.full_like(X, 100.0, dtype=np.float64)
    second = first.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            feature = sites[cy % count, cx % count]
            distance = (cx + feature[..., 0] - x) ** 2 + (cy + feature[..., 1] - y) ** 2
            second = np.minimum(second, np.maximum(first, distance))
            first = np.minimum(first, distance)

    edge = np.sqrt(second) - np.sqrt(first)
    clusters = noise(rng, 6) * 0.57 + noise(rng, 16) * 0.29 + noise(rng, 35) * 0.14
    grain = noise(rng, 93) * 0.65 + rng.random((SIZE, SIZE), dtype=np.float32) * 0.35
    ribbons = (1.0 - smooth(0.021, 0.112, edge)) * smooth(0.28, 0.67, clusters + grain * 0.15)
    bodies = smooth(0.43, 0.78, clusters * 0.76 + grain * 0.42 - first * 0.25)
    foam = np.clip(ribbons * 0.77 + bodies * 0.88, 0.0, 1.0)
    return Image.fromarray(np.rint(foam * 255).astype(np.uint8))


def ripple_normal():
    rng = np.random.default_rng(21093)
    x_slope = np.zeros_like(X, dtype=np.float64)
    y_slope = np.zeros_like(X, dtype=np.float64)
    for count, lower, upper, scale in ((18, 3, 11, 0.089), (31, 12, 29, 0.049), (39, 30, 63, 0.026)):
        for _ in range(count):
            kx = rng.integers(lower, upper)
            ky = rng.integers(-upper // 2, upper // 2 + 1)
            phase = 2 * np.pi * (kx * X + ky * Y) + rng.uniform(0, 2 * np.pi)
            slope = np.cos(phase) * scale / np.hypot(kx, ky) * rng.uniform(0.48, 1.0)
            x_slope += slope * kx
            y_slope += slope * ky

    length = np.sqrt(1.0 + x_slope * x_slope + y_slope * y_slope)
    normal = np.stack((-x_slope / length, -y_slope / length, 1.0 / length), axis=-1)
    return Image.fromarray(np.rint((normal * 0.5 + 0.5) * 255).astype(np.uint8))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=Path(__file__).resolve().parents[1]
                        / "Assets/Art/Textures/NAT/coastal_water")
    output = parser.parse_args().out
    output.mkdir(parents=True, exist_ok=True)
    foam_mask().save(output / "CoastalFoam_Mask.png")
    ripple_normal().save(output / "CoastalRipples_Normal.png")


if __name__ == "__main__":
    main()
