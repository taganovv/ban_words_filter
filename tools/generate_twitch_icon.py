#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw


TWITCH_PURPLE = (145, 70, 255, 255)
WHITE = (255, 255, 255, 255)


def rounded_rect(
    draw: ImageDraw.ImageDraw,
    box: tuple[float, float, float, float],
    radius: float,
    fill: tuple[int, int, int, int],
) -> None:
    x0, y0, x1, y1 = box
    draw.rounded_rectangle((x0, y0, x1, y1), radius=radius, fill=fill)


def draw_glitch(
    draw: ImageDraw.ImageDraw,
    ox: float,
    oy: float,
    scale: float,
    color: tuple[int, int, int, int],
) -> None:
    def p(x: float, y: float) -> tuple[float, float]:
        return (ox + x * scale, oy + y * scale)

    draw.polygon(
        [
            p(6, 0),
            p(1.714, 4.286),
            p(1.714, 15.143),
            p(6.857, 15.143),
            p(6.857, 12),
            p(10.714, 15.714),
            p(17.571, 9.5),
            p(17.571, 0),
        ],
        fill=color,
    )

    draw.rectangle([p(11.571, 4.714), p(13.286, 9.857)], fill=color)
    draw.rectangle([p(16.286, 4.714), p(18, 9.857)], fill=color)


def generate_twitch_icon(size: int = 64) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    margin = size * 0.06
    rounded_rect(
        draw,
        (margin, margin, size - margin, size - margin),
        radius=size * 0.22,
        fill=TWITCH_PURPLE,
    )

    logo_w, logo_h = 18.0, 15.714
    pad = size * 0.22
    scale = min((size - 2 * pad) / logo_w, (size - 2 * pad) / logo_h)
    ox = (size - logo_w * scale) / 2
    oy = (size - logo_h * scale) / 2 + scale * 0.15
    draw_glitch(draw, ox, oy, scale, WHITE)

    return img


def main() -> int:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("twitch-icon.png")
    icon = generate_twitch_icon(64)
    out.parent.mkdir(parents=True, exist_ok=True)
    icon.save(out, "PNG")
    print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
