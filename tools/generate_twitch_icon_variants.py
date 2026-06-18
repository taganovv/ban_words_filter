#!/usr/bin/env python3
"""Generate multiple Twitch icon variants for review."""

from __future__ import annotations

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

TWITCH_PURPLE = (145, 70, 255, 255)
TWITCH_PURPLE_DARK = (119, 56, 210, 255)
WHITE = (255, 255, 255, 255)


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def draw_glitch_shape(
    draw: ImageDraw.ImageDraw,
    ox: float,
    oy: float,
    scale: float,
    color: tuple[int, int, int, int],
    *,
    bar_scale: float = 1.0,
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

    bar_w = 1.715 * bar_scale
    bar_h = 5.143 * bar_scale
    y0 = 4.714 + (5.143 - bar_h) / 2

    draw.rectangle([p(11.571, y0), p(11.571 + bar_w, y0 + bar_h)], fill=color)
    draw.rectangle([p(16.286, y0), p(16.286 + bar_w, y0 + bar_h)], fill=color)


def render_base(size: int, painter) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    painter(ImageDraw.Draw(img), size)
    return img


def upscale_smooth(img: Image.Image, target: int = 64) -> Image.Image:
    big = img.resize((target * 8, target * 8), Image.Resampling.NEAREST)
    return big.resize((target, target), Image.Resampling.LANCZOS)


def variant_v1_rounded_purple(size: int) -> Image.Image:
    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        m = s * 0.06
        draw.rounded_rectangle((m, m, s - m, s - m), radius=s * 0.22, fill=TWITCH_PURPLE)
        scale = s * 0.038
        ox = s * 0.17
        oy = s * 0.19
        draw_glitch_shape(draw, ox, oy, scale, WHITE)

    return upscale_smooth(render_base(size * 8, paint))


def variant_v2_full_bleed(size: int) -> Image.Image:
    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        draw.rounded_rectangle((0, 0, s, s), radius=s * 0.18, fill=TWITCH_PURPLE)
        scale = s * 0.048
        ox = s * 0.11
        oy = s * 0.13
        draw_glitch_shape(draw, ox, oy, scale, WHITE)

    return upscale_smooth(render_base(size * 8, paint))


def variant_v3_glitch_only(size: int) -> Image.Image:
    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        scale = s * 0.045
        ox = s * 0.14
        oy = s * 0.16
        draw_glitch_shape(draw, ox, oy, scale, WHITE)

    return upscale_smooth(render_base(size * 8, paint))


def variant_v4_gradient_bg(size: int) -> Image.Image:
    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        for y in range(s):
            t = y / max(1, s - 1)
            color = (
                int(lerp(TWITCH_PURPLE[0], TWITCH_PURPLE_DARK[0], t)),
                int(lerp(TWITCH_PURPLE[1], TWITCH_PURPLE_DARK[1], t)),
                int(lerp(TWITCH_PURPLE[2], TWITCH_PURPLE_DARK[2], t)),
                255,
            )
            draw.line([(0, y), (s, y)], fill=color)

        m = s * 0.04
        mask = Image.new("L", (s, s), 0)
        ImageDraw.Draw(mask).rounded_rectangle((m, m, s - m, s - m), radius=s * 0.22, fill=255)
        overlay = Image.new("RGBA", (s, s), (0, 0, 0, 0))
        overlay.paste(Image.new("RGBA", (s, s), TWITCH_PURPLE), mask=mask)

        # redraw on temp - simpler: just rounded rect on gradient
        draw.rounded_rectangle((m, m, s - m, s - m), radius=s * 0.22, outline=(255, 255, 255, 30), width=max(1, s // 64))

        scale = s * 0.041
        ox = s * 0.16
        oy = s * 0.18
        draw_glitch_shape(draw, ox, oy, scale, WHITE)

    return upscale_smooth(render_base(size * 8, paint))


def variant_v5_bold_bars(size: int) -> Image.Image:
    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        m = s * 0.06
        draw.rounded_rectangle((m, m, s - m, s - m), radius=s * 0.22, fill=TWITCH_PURPLE)
        scale = s * 0.038
        ox = s * 0.17
        oy = s * 0.19
        draw_glitch_shape(draw, ox, oy, scale, WHITE, bar_scale=1.18)

    return upscale_smooth(render_base(size * 8, paint))


def variant_v6_centered_compact(size: int) -> Image.Image:
    """Logo centered with equal padding, square purple background."""

    def paint(draw: ImageDraw.ImageDraw, s: int) -> None:
        draw.rectangle((0, 0, s, s), fill=TWITCH_PURPLE)
        logo_w, logo_h = 18.0, 15.714
        pad = s * 0.24
        scale = min((s - 2 * pad) / logo_w, (s - 2 * pad) / logo_h)
        ox = (s - logo_w * scale) / 2
        oy = (s - logo_h * scale) / 2
        draw_glitch_shape(draw, ox, oy, scale, WHITE)

    return upscale_smooth(render_base(size * 8, paint))


VARIANTS = [
    ("v1-rounded-purple", "Скруглённый фиолетовый фон, стандартные пропорции", variant_v1_rounded_purple),
    ("v2-full-bleed", "Логотип крупнее, меньше отступов", variant_v2_full_bleed),
    ("v3-glitch-only", "Только белый Glitch на прозрачном фоне", variant_v3_glitch_only),
    ("v4-gradient-bg", "Фиолетовый градиент + лёгкая обводка", variant_v4_gradient_bg),
    ("v5-bold-bars", "Более жирные вертикальные полоски", variant_v5_bold_bars),
    ("v6-centered-compact", "Квадратный фон, логотип по центру", variant_v6_centered_compact),
]


def main() -> int:
    out_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("twitch-icon-variants")
    preview_dir = out_dir / "preview-256"
    out_dir.mkdir(parents=True, exist_ok=True)
    preview_dir.mkdir(parents=True, exist_ok=True)

    for slug, _label, maker in VARIANTS:
        icon = maker(64)
        icon.save(out_dir / f"twitch-{slug}.png", "PNG")
        preview = icon.resize((256, 256), Image.Resampling.NEAREST)
        preview.save(preview_dir / f"twitch-{slug}-256.png", "PNG")
        print(out_dir / f"twitch-{slug}.png")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
