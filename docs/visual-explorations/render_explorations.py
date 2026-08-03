# render_explorations.py — gera as 6 explorações visuais do revoa.me
# Renderiza em 2x e faz downscale LANCZOS para anti-aliasing suave.
# Saida: docs/visual-explorations/0N-*.png (1200x1500)
import math
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTS = r"C:\Users\rodne\.kilo\skills\canvas-design\canvas-fonts"
OUT = os.path.dirname(os.path.abspath(__file__))  # mesmo dir do script: docs/visual-explorations
os.makedirs(OUT, exist_ok=True)

W, H = 1200, 1500
SCALE = 2  # supersample

INK = (10, 10, 10)
CHARCOAL = (23, 23, 23)
SMOKE = (38, 38, 38)
SILVER = (115, 115, 115)
WHITE = (250, 250, 250)
EMERALD = (16, 185, 129)
SKY = (14, 165, 233)
PURPLE = (168, 85, 247)
PINK = (236, 72, 153)
AMBER = (245, 158, 11)
TERRACOTA = (188, 108, 37)
CREAM = (254, 250, 224)
LIME = (132, 204, 22)
GOLD = (234, 179, 8)


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), int(size * SCALE))


def new_canvas(bg=INK):
    img = Image.new("RGB", (W * SCALE, H * SCALE), bg)
    return img, ImageDraw.Draw(img)


def finalize(img, path):
    img = img.resize((W, H), Image.LANCZOS)
    img.save(path, "PNG")
    print("gerado:", os.path.basename(path))


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def vgradient(draw, x0, y0, x1, y1, c0, c1, steps=256):
    for i in range(steps):
        t = i / (steps - 1)
        c = lerp(c0, c1, t)
        y = y0 + (y1 - y0) * t
        draw.line([(x0, y), (x1, y)], fill=c, width=max(1, int((y1 - y0) / steps) + 2))


def center_text(draw, text, fnt, cx, cy, fill):
    bbox = draw.textbbox((0, 0), text, font=fnt)
    w = bbox[2] - bbox[0]
    h = bbox[3] - bbox[1]
    draw.text((cx - w / 2 - bbox[0], cy - h / 2 - bbox[1]), text, font=fnt, fill=fill)


def rtext(draw, text, fnt, x, y, fill):
    draw.text((x, y), text, font=fnt, fill=fill)


def monogram(draw, cx, cy, r, fill, ring=None):
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=ring, width=int(3 * SCALE))
    f = font("WorkSans-Bold.ttf", r * 0.9)
    center_text(draw, "RV", f, cx, cy, fill)


def label(draw, text, x, y, fnt=None, fill=SILVER):
    fnt = fnt or font("WorkSans-Regular.ttf", 13)
    rtext(draw, text.upper(), fnt, x, y, fill)


# ───────────────────────────────────────────────────────────────
# 1. CIRCUITO VIVO — órbitas concêntricas, emerald→sky + âmbar
# ───────────────────────────────────────────────────────────────
def e01_circuito_vivo():
    img, d = new_canvas(INK)
    cx, cy = W * SCALE / 2, 720 * SCALE
    # arcos concêntricos (anéis parciais) com gradiente emerald→sky
    for i in range(9):
        r = (120 + i * 70) * SCALE
        t = i / 8
        col = lerp(EMERALD, SKY, t)
        w = max(2, int((10 - i) * SCALE * 0.9))
        start = 200 + i * 6
        end = 340 - i * 4
        d.arc([cx - r, cy - r, cx + r, cy + r], start, end, fill=col, width=w)
    # acento âmbar — "pouso"
    ar = 230 * SCALE
    d.arc([cx - ar, cy - ar, cx + ar, cy + ar], 95, 120, fill=AMBER, width=int(8 * SCALE))
    # monograma central
    monogram(d, cx, cy, 95 * SCALE, WHITE, ring=EMERALD)
    # wordmark
    center_text(d, "revoa.me", font("WorkSans-Bold.ttf", 96), cx, 1180 * SCALE, WHITE)
    # RM$ orbital
    f = font("WorkSans-Bold.ttf", 40)
    d.ellipse([cx + 150 * SCALE, cy - 150 * SCALE, cx + 230 * SCALE, cy - 70 * SCALE],
              fill=CHARCOAL, outline=AMBER, width=int(2 * SCALE))
    center_text(d, "RM$", f, cx + 190 * SCALE, cy - 110 * SCALE, AMBER)
    label(d, "01 · Circuito Vivo", 80 * SCALE, 80 * SCALE, font("WorkSans-Bold.ttf", 16), EMERALD)
    label(d, "híbrida · ciclo com alma", 80 * SCALE, 112 * SCALE, font("WorkSans-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "01-circuito-vivo.png"))


# ───────────────────────────────────────────────────────────────
# 2. BAIRRO SOLAR — grade de mapa + pinos + raios, terracota/creme
# ───────────────────────────────────────────────────────────────
def e02_bairro_solar():
    img, d = new_canvas(INK)
    # grade de mapa
    step = 80 * SCALE
    for x in range(0, W * SCALE, step):
        d.line([(x, 0), (x, H * SCALE)], fill=(28, 28, 28), width=max(1, SCALE))
    for y in range(0, H * SCALE, step):
        d.line([(0, y), (W * SCALE, y)], fill=(28, 28, 28), width=max(1, SCALE))
    # raios 1/5/10/25 km a partir de um ponto
    cx, cy = 600 * SCALE, 720 * SCALE
    for r_km, col in [(1, TERRACOTA), (5, AMBER), (10, EMERALD), (25, SKY)]:
        r = (90 + r_km * 22) * SCALE
        d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=col, width=max(1, int(1.5 * SCALE)))
    # pinos de localização em uma malha
    import random
    random.seed(7)
    for _ in range(26):
        gx = (random.randint(1, 13)) * step
        gy = (random.randint(2, 15)) * step
        d.ellipse([gx - 5 * SCALE, gy - 5 * SCALE, gx + 5 * SCALE, gy + 5 * SCALE], fill=TERRACOTA)
    # monograma como pino grande
    d.polygon([(cx, cy - 120 * SCALE), (cx + 90 * SCALE, cy + 30 * SCALE),
               (cx, cy + 10 * SCALE), (cx - 90 * SCALE, cy + 30 * SCALE)], fill=TERRACOTA)
    monogram(d, cx, cy - 30 * SCALE, 70 * SCALE, CREAM, ring=CREAM)
    # wordmark editorial serif
    center_text(d, "revoa.me", font("YoungSerif-Regular.ttf", 104), cx, 1230 * SCALE, WHITE)
    center_text(d, "RM$", font("YoungSerif-Regular.ttf", 52), cx, 1340 * SCALE, TERRACOTA)
    label(d, "02 · Bairro Solar", 80 * SCALE, 80 * SCALE, font("YoungSerif-Regular.ttf", 16), TERRACOTA)
    label(d, "híbrida · hiperlocalidade", 80 * SCALE, 112 * SCALE, font("WorkSans-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "02-bairro-solar.png"))


# ───────────────────────────────────────────────────────────────
# 3. SEMENTE & VOO — trajetórias de asa, esmeralda+lima+dourado
# ───────────────────────────────────────────────────────────────
def e03_semente_voo():
    img, d = new_canvas((6, 22, 18))  # deep emerald
    # trajetórias de voo (curvas bezier-ish via arcos largos)
    cx = W * SCALE / 2
    for i in range(7):
        r = (160 + i * 90) * SCALE
        t = i / 6
        col = lerp(EMERALD, LIME, t)
        d.arc([cx - r, 200 * SCALE - r, cx + r, 200 * SCALE + r], 35, 75,
              fill=col, width=max(2, int((8 - i) * SCALE)))
    # "asa" — três traços ascendentes
    bx, by = cx, 760 * SCALE
    for k, (dx, dy, col) in enumerate([(-160, 120, GOLD), (0, 60, LIME), (160, 120, GOLD)]):
        d.line([(bx + dx * 0.2 * SCALE, by + dy * 0.2 * SCALE),
                (bx + dx * SCALE, by - dy * SCALE)], fill=col, width=int(5 * SCALE))
    # semente núcleo
    d.ellipse([cx - 90 * SCALE, 690 * SCALE, cx + 90 * SCALE, 870 * SCALE],
              outline=GOLD, width=int(4 * SCALE))
    center_text(d, "RV", font("WorkSans-Bold.ttf", 86), cx, 780 * SCALE, WHITE)
    center_text(d, "revoa.me", font("WorkSans-Bold.ttf", 96), cx, 1180 * SCALE, WHITE)
    center_text(d, "RM$", font("WorkSans-Bold.ttf", 44), cx, 1300 * SCALE, GOLD)
    label(d, "03 · Semente & Voo", 80 * SCALE, 80 * SCALE, font("WorkSans-Bold.ttf", 16), LIME)
    label(d, "híbrida · transformação", 80 * SCALE, 112 * SCALE, font("WorkSans-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "03-semente-voo.png"))


# ───────────────────────────────────────────────────────────────
# 4. MALHA ON-CHAIN — graph de nós sobre ink, fonte Tektur
# ───────────────────────────────────────────────────────────────
def e04_malha_onchain():
    img, d = new_canvas(INK)
    import random
    random.seed(42)
    nodes = [(random.uniform(0.08, 0.92) * W * SCALE,
              random.uniform(0.1, 0.78) * H * SCALE) for _ in range(34)]
    cx, cy = 600 * SCALE, 700 * SCALE
    nodes.insert(0, (cx, cy))
    # arestas: liga cada nó aos próximos
    for i, (ax, ay) in enumerate(nodes):
        for j, (bx, by) in enumerate(nodes):
            if j <= i:
                continue
            dist = math.hypot(ax - bx, ay - by)
            if dist < 320 * SCALE:
                alpha_col = lerp(SMOKE, SKY, max(0, 1 - dist / (320 * SCALE)))
                d.line([(ax, ay), (bx, by)], fill=alpha_col, width=max(1, SCALE))
    # nós
    for i, (x, y) in enumerate(nodes):
        r = (14 if i == 0 else 7) * SCALE
        col = EMERALD if i == 0 else SKY
        d.ellipse([x - r, y - r, x + r, y + r], fill=col)
        if i == 0:
            d.ellipse([x - r - 6 * SCALE, y - r - 6 * SCALE, x + r + 6 * SCALE, y + r + 6 * SCALE],
                      outline=EMERALD, width=int(2 * SCALE))
    # nó central = RV
    center_text(d, "RV", font("Tektur-Regular.ttf", 30), cx, cy, INK)
    # wordmark monoespaçado techy
    center_text(d, "revoa.me", font("Tektur-Regular.ttf", 92), cx, 1150 * SCALE, WHITE)
    # bloco "RM$"
    d.rectangle([cx - 90 * SCALE, 1260 * SCALE, cx + 90 * SCALE, 1360 * SCALE],
                outline=EMERALD, width=int(3 * SCALE))
    center_text(d, "RM$", font("Tektur-Regular.ttf", 40), cx, 1310 * SCALE, EMERALD)
    label(d, "04 · Malha On-Chain", 80 * SCALE, 80 * SCALE, font("Tektur-Regular.ttf", 16), EMERALD)
    label(d, "web3 · a rede é a verdade", 80 * SCALE, 112 * SCALE, font("Tektur-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "04-malha-onchain.png"))


# ───────────────────────────────────────────────────────────────
# 5. LEDGER PRISMÁTICO — shards triangulares, roxo→rosa→sky
# ───────────────────────────────────────────────────────────────
def e05_ledger_prismatico():
    img, d = new_canvas(INK)
    cx, cy = 600 * SCALE, 700 * SCALE
    # shards (triângulos) irradiando do centro com gradiente roxo→rosa→sky
    import random
    random.seed(11)
    cols = [PURPLE, PINK, SKY, PURPLE, PINK]
    for i in range(48):
        a0 = i * (360 / 48)
        a1 = a0 + (360 / 48)
        rr = (260 + random.randint(0, 160)) * SCALE
        x0 = cx + math.cos(math.radians(a0)) * 60 * SCALE
        y0 = cy + math.sin(math.radians(a0)) * 60 * SCALE
        x1 = cx + math.cos(math.radians(a1)) * 60 * SCALE
        y1 = cy + math.sin(math.radians(a1)) * 60 * SCALE
        x2 = cx + math.cos(math.radians((a0 + a1) / 2)) * rr
        y2 = cy + math.sin(math.radians((a0 + a1) / 2)) * rr
        col = cols[i % len(cols)]
        d.polygon([(x0, y0), (x1, y1), (x2, y2)], fill=col)
    # halo central escuro p/ legibilidade
    d.ellipse([cx - 110 * SCALE, cy - 110 * SCALE, cx + 110 * SCALE, cy + 110 * SCALE], fill=INK)
    d.ellipse([cx - 110 * SCALE, cy - 110 * SCALE, cx + 110 * SCALE, cy + 110 * SCALE],
              outline=PINK, width=int(3 * SCALE))
    center_text(d, "RV", font("WorkSans-Bold.ttf", 70), cx, cy, WHITE)
    center_text(d, "revoa.me", font("WorkSans-Bold.ttf", 96), cx, 1160 * SCALE, WHITE)
    center_text(d, "RM$", font("WorkSans-Bold.ttf", 44), cx, 1300 * SCALE, PINK)
    label(d, "05 · Ledger Prismático", 80 * SCALE, 80 * SCALE, font("WorkSans-Bold.ttf", 16), PINK)
    label(d, "web3 · tokenização é refração", 80 * SCALE, 112 * SCALE, font("WorkSans-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "05-ledger-prismatico.png"))


# ───────────────────────────────────────────────────────────────
# 6. PULSO SUBNET — mínimo suíço, ink + sky, linha de pulso
# ───────────────────────────────────────────────────────────────
def e06_pulso_subnet():
    img, d = new_canvas(INK)
    # linha de grade sutil
    for y in range(0, H * SCALE, 150 * SCALE):
        d.line([(80 * SCALE, y), (W * SCALE - 80 * SCALE, y)], fill=(20, 20, 20), width=max(1, SCALE))
    # linha de pulso (ECG-like) atravessando
    midy = 820 * SCALE
    pts = [(80 * SCALE, midy)]
    x = 80 * SCALE
    end = (W - 80) * SCALE
    import math as m
    while x < end:
        pts.append((x + 60 * SCALE, midy))
        # pico
        pts.append((x + 90 * SCALE, midy - 120 * SCALE))
        pts.append((x + 120 * SCALE, midy + 120 * SCALE))
        pts.append((x + 150 * SCALE, midy))
        x += 230 * SCALE
    pts.append((end, midy))
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i + 1]], fill=SKY, width=int(3 * SCALE))
    # monograma tipográfico puro
    center_text(d, "RV", font("WorkSans-Bold.ttf", 220), W * SCALE / 2, 520 * SCALE, WHITE)
    # wordmark enorme e calmo
    center_text(d, "revoa.me", font("WorkSans-Bold.ttf", 112), W * SCALE / 2, 1120 * SCALE, WHITE)
    center_text(d, "RM$", font("WorkSans-Bold.ttf", 40), W * SCALE / 2, 1280 * SCALE, SKY)
    label(d, "06 · Pulso Subnet", 80 * SCALE, 80 * SCALE, font("WorkSans-Bold.ttf", 16), SKY)
    label(d, "web3 · precisão silenciosa", 80 * SCALE, 112 * SCALE, font("WorkSans-Regular.ttf", 13), SILVER)
    finalize(img, os.path.join(OUT, "06-pulso-subnet.png"))


if __name__ == "__main__":
    e01_circuito_vivo()
    e02_bairro_solar()
    e03_semente_voo()
    e04_malha_onchain()
    e05_ledger_prismatico()
    e06_pulso_subnet()
    print("concluido.")
