from __future__ import annotations

import html
import re
import textwrap
from pathlib import Path

import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages
from matplotlib.font_manager import FontProperties


ROOT = Path(__file__).resolve().parent
HTML_PATH = ROOT / "GiaiThich_Assets_Scripts_VN.html"
PDF_PATH = ROOT / "GiaiThich_Assets_Scripts_VN.pdf"

REGEX = re.compile(r"<(h1|h2|h3|p|li)[^>]*>(.*?)</\1>", re.IGNORECASE | re.DOTALL)
TAG_STRIP = re.compile(r"<[^>]+>")
WHITESPACE = re.compile(r"\s+")


def extract_blocks(html_text: str) -> list[tuple[str, str]]:
    blocks: list[tuple[str, str]] = []
    for tag, raw_text in REGEX.findall(html_text):
        text = TAG_STRIP.sub("", raw_text)
        text = html.unescape(text)
        text = WHITESPACE.sub(" ", text).strip()
        if not text:
            continue
        blocks.append((tag.lower(), text))
    return blocks


def style_for(tag: str) -> dict:
    if tag == "h1":
        return {"size": 19, "weight": "bold", "wrap": 58, "gap": 0.014, "x": 0.07}
    if tag == "h2":
        return {"size": 14, "weight": "bold", "wrap": 74, "gap": 0.012, "x": 0.07}
    if tag == "h3":
        return {"size": 11.5, "weight": "bold", "wrap": 84, "gap": 0.009, "x": 0.07}
    if tag == "li":
        return {"size": 9.6, "weight": "normal", "wrap": 92, "gap": 0.007, "x": 0.09, "bullet": True}
    return {"size": 9.5, "weight": "normal", "wrap": 96, "gap": 0.008, "x": 0.07}


def line_step(size: float) -> float:
    return (size / 72.0) / 11.69 * 1.75


def render(pdf: PdfPages, blocks: list[tuple[str, str]]) -> None:
    regular_font = FontProperties(fname=r"C:\Windows\Fonts\arial.ttf")
    bold_font = FontProperties(fname=r"C:\Windows\Fonts\arialbd.ttf")

    fig = None
    ax = None
    y = 0.96

    def new_page() -> None:
        nonlocal fig, ax, y
        if fig is not None:
            pdf.savefig(fig, bbox_inches="tight")
            plt.close(fig)
        fig = plt.figure(figsize=(8.27, 11.69))
        ax = fig.add_axes([0, 0, 1, 1])
        ax.axis("off")
        y = 0.96

    new_page()

    for tag, text in blocks:
        style = style_for(tag)
        font = bold_font if style["weight"] == "bold" else regular_font
        prefix = "• " if style.get("bullet") else ""
        wrapped = textwrap.wrap(prefix + text, width=style["wrap"], break_long_words=False, break_on_hyphens=False)
        if not wrapped:
            wrapped = [prefix + text]

        needed = len(wrapped) * line_step(style["size"]) + style["gap"]
        if y - needed < 0.05:
            new_page()

        for line in wrapped:
            ax.text(
                style["x"],
                y,
                line,
                fontsize=style["size"],
                va="top",
                ha="left",
                color="#1f2937",
                fontproperties=font,
            )
            y -= line_step(style["size"])

        y -= style["gap"]

    if fig is not None:
        pdf.savefig(fig, bbox_inches="tight")
        plt.close(fig)


def main() -> None:
    html_text = HTML_PATH.read_text(encoding="utf-8")
    blocks = extract_blocks(html_text)
    with PdfPages(PDF_PATH) as pdf:
        render(pdf, blocks)
    print(PDF_PATH)


if __name__ == "__main__":
    main()
