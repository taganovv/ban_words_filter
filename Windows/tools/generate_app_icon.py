#!/usr/bin/env python3
from __future__ import annotations

import struct
import sys
import zlib
from pathlib import Path


def paeth(a: int, b: int, c: int) -> int:
    p = a + b - c
    pa = abs(p - a)
    pb = abs(p - b)
    pc = abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    if pb <= pc:
        return b
    return c


def read_png_rgba(path: Path) -> tuple[int, int, list[bytes]]:
    png = path.read_bytes()
    if png[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"Not a PNG file: {path}")

    pos = 8
    idat = b""
    width = height = 0
    color_type = 0
    while pos < len(png):
        length = int.from_bytes(png[pos : pos + 4], "big")
        chunk_type = png[pos + 4 : pos + 8]
        data = png[pos + 8 : pos + 8 + length]
        pos += 12 + length
        if chunk_type == b"IHDR":
            width = int.from_bytes(data[0:4], "big")
            height = int.from_bytes(data[4:8], "big")
            color_type = data[9]
        elif chunk_type == b"IDAT":
            idat += data
        elif chunk_type == b"IEND":
            break

    if color_type not in (2, 6):
        raise ValueError(f"Unsupported PNG color type: {color_type}")

    bytes_per_pixel = 4 if color_type == 6 else 3
    raw = zlib.decompress(idat)
    rows: list[bytes] = []
    prev = bytes(width * bytes_per_pixel)
    i = 0
    for _ in range(height):
        filt = raw[i]
        i += 1
        row = bytearray(raw[i : i + width * bytes_per_pixel])
        i += width * bytes_per_pixel
        if filt == 1:
            for x in range(bytes_per_pixel, len(row)):
                row[x] = (row[x] + row[x - bytes_per_pixel]) & 255
        elif filt == 2:
            for x in range(len(row)):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(len(row)):
                left = row[x - bytes_per_pixel] if x >= bytes_per_pixel else 0
                row[x] = (row[x] + ((left + prev[x]) // 2)) & 255
        elif filt == 4:
            for x in range(len(row)):
                left = row[x - bytes_per_pixel] if x >= bytes_per_pixel else 0
                up = prev[x]
                up_left = prev[x - bytes_per_pixel] if x >= bytes_per_pixel else 0
                row[x] = (row[x] + paeth(left, up, up_left)) & 255

        rgba = bytearray(width * 4)
        if color_type == 6:
            rgba[:] = row
        else:
            for px in range(width):
                rgba[px * 4 : px * 4 + 3] = row[px * 3 : px * 3 + 3]
                rgba[px * 4 + 3] = 255
        rows.append(bytes(rgba))
        prev = row

    return width, height, rows


def write_png(w: int, h: int, rgba_rows: list[bytes]) -> bytes:
    def chunk(tag: bytes, payload: bytes) -> bytes:
        return (
            struct.pack(">I", len(payload))
            + tag
            + payload
            + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF)
        )

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    raw_rows = b"".join(b"\x00" + row for row in rgba_rows)
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(raw_rows, 9))
        + chunk(b"IEND", b"")
    )


def resize_nearest(rows: list[bytes], src_w: int, src_h: int, dst_w: int, dst_h: int) -> list[bytes]:
    out: list[bytes] = []
    for y in range(dst_h):
        sy = min(src_h - 1, int(y * src_h / dst_h))
        src_row = rows[sy]
        row = bytearray()
        for x in range(dst_w):
            sx = min(src_w - 1, int(x * src_w / dst_w))
            i = sx * 4
            row.extend(src_row[i : i + 4])
        out.append(bytes(row))
    return out


def write_ico(src_png: Path, dst_ico: Path) -> None:
    width, height, rows = read_png_rgba(src_png)
    sizes = [16, 32, 48, 64, 128, 256]
    images: list[tuple[int, bytes]] = []
    for size in sizes:
        resized = resize_nearest(rows, width, height, size, size)
        images.append((size, write_png(size, size, resized)))

    offset = 6 + 16 * len(images)
    parts = [struct.pack("<HHH", 0, 1, len(images))]
    for size, png in images:
        stored = 0 if size >= 256 else size
        parts.append(struct.pack("<BBBBHHII", stored, stored, 0, 0, 1, 32, len(png), offset))
        offset += len(png)
    for _, png in images:
        parts.append(png)

    dst_ico.write_bytes(b"".join(parts))


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit(f"Usage: {sys.argv[0]} input.png output.ico")

    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    write_ico(src, dst)
    print(dst)


if __name__ == "__main__":
    main()
