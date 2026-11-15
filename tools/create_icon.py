"""Generate the original NexusRoles store icon (requires Pillow)."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SIZE = 1024


def main() -> None:
    image = Image.new("RGB", (SIZE, SIZE), "#10142d")
    pixels = image.load()
    for y in range(SIZE):
        for x in range(SIZE):
            dx = (x - SIZE * 0.48) / SIZE
            dy = (y - SIZE * 0.42) / SIZE
            glow = max(0.0, 1.0 - (dx * dx + dy * dy) ** 0.5 * 2.0)
            pixels[x, y] = (
                int(16 + 31 * glow),
                int(20 + 32 * glow),
                int(45 + 83 * glow),
            )

    glow_layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_layer)
    glow_draw.ellipse((185, 155, 840, 810), outline=(95, 239, 235, 90), width=48)
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(42))
    image = Image.alpha_composite(image.convert("RGBA"), glow_layer)
    draw = ImageDraw.Draw(image)

    draw.ellipse((214, 184, 810, 780), outline=(116, 240, 233, 255), width=18)
    draw.arc((161, 225, 865, 855), 205, 330, fill=(181, 118, 255, 255), width=22)
    draw.ellipse((760, 267, 820, 327), fill=(255, 223, 137, 255))
    draw.ellipse((228, 676, 269, 717), fill=(181, 118, 255, 255))

    # Angular N mark drawn as one closed path, without using a redistributable font.
    mark = [(365, 650), (365, 374), (439, 374), (589, 564), (589, 374),
            (659, 374), (659, 650), (590, 650), (435, 455), (435, 650)]
    draw.polygon(mark, fill=(245, 247, 255, 255))
    draw.line([(365, 374), (439, 374), (589, 564), (589, 374)],
              fill=(187, 251, 240, 255), width=10, joint="curve")

    image.resize((256, 256), Image.Resampling.LANCZOS).convert("RGB").save(
        ROOT / "assets" / "icon.png", format="PNG", optimize=True
    )


if __name__ == "__main__":
    main()
