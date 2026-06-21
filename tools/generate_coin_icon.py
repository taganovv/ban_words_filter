#!/usr/bin/env python3
from __future__ import annotations

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def mix_color(c1: tuple[int, int, int], c2: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return (
        int(lerp(c1[0], c2[0], t)),
        int(lerp(c1[1], c2[1], t)),
        int(lerp(c1[2], c2[2], t)),
    )


def generate_coin(size: int = 64) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    cx = cy = size / 2
    radius = size * 0.46

    for y in range(size):
        for x in range(size):
            dx = x - cx + 0.5
            dy = y - cy + 0.5
            dist = math.hypot(dx, dy)
            if dist > radius:
                continue

            edge = dist / radius
            angle = math.atan2(dy, dx)
            light = 0.55 + 0.35 * math.cos(angle + 0.8) - 0.25 * edge

            base = mix_color((184, 134, 11), (255, 215, 64), light)
            rim = mix_color((120, 85, 8), (210, 170, 40), 0.5 + 0.5 * math.sin(angle * 3))
            color = mix_color(base, rim, min(1.0, edge * 1.15))

            highlight = max(0.0, 1.0 - math.hypot(x - cx * 0.72, y - cy * 0.68) / (radius * 0.55))
            if highlight > 0:
                color = mix_color(color, (255, 245, 200), highlight * 0.55)

            shadow = max(0.0, 1.0 - math.hypot(x - cx * 1.25, y - cy * 1.2) / (radius * 0.7))
            if shadow > 0:
                color = mix_color(color, (90, 60, 10), shadow * 0.35)

            alpha = 255
            if dist > radius - 0.8:
                alpha = int(255 * max(0.0, (radius - dist) / 0.8))

            img.putpixel((x, y), (*color, alpha))

    draw = ImageDraw.Draw(img)
    inner_r = radius * 0.78
    draw.ellipse(
        (cx - inner_r, cy - inner_r, cx + inner_r, cy + inner_r),
        outline=(160, 120, 30, 180),
        width=max(1, size // 32),
    )
    draw.ellipse(
        (cx - inner_r * 0.92, cy - inner_r * 0.92, cx + inner_r * 0.92, cy + inner_r * 0.92),
        outline=(255, 230, 140, 90),
        width=1,
    )

    return img


def main() -> int:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("coin-icon.png")
    coin = generate_coin(64)
    out.parent.mkdir(parents=True, exist_ok=True)
    coin.save(out, "PNG")
    print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
