import os
from PIL import Image, ImageDraw, ImageFont

os.makedirs("assets_70_30", exist_ok=True)

NAVY = "#1E2A4A"
CREAM = "#FFFDF7"
YELLOW = "#FFD166"
BLUE = "#4D96FF"
CORAL = "#FF6B6B"
MINT = "#6BCB77"
ORANGE = "#FF9F43"

# 1. Slide 2: Split screen comparison illustration (Worksheet vs Voice Tablet)
img2 = Image.new('RGBA', (700, 420), (255, 255, 255, 0))
d2 = ImageDraw.Draw(img2)
# Left: Dull Worksheet Card
d2.rounded_rectangle([20, 20, 330, 400], radius=24, fill='#FEE2E2', outline=NAVY, width=5)
d2.rectangle([60, 60, 290, 300], fill='#FFFFFF', outline=NAVY, width=3)
d2.line([(90, 100), (260, 100)], fill='#94A3B8', width=4)
d2.line([(90, 140), (260, 140)], fill='#94A3B8', width=4)
d2.line([(90, 180), (220, 180)], fill='#94A3B8', width=4)
d2.line([(90, 220), (240, 220)], fill='#94A3B8', width=4)
# Red cross
d2.line([(150, 130), (200, 180)], fill=CORAL, width=8)
d2.line([(200, 130), (150, 180)], fill=CORAL, width=8)
# Sad icon
d2.ellipse([145, 320, 205, 380], fill=CORAL, outline=NAVY, width=4)
d2.arc([160, 350, 190, 375], start=180, end=360, fill='#FFFFFF', width=4)

# Right: Cheerful Voice Tablet
d2.rounded_rectangle([370, 20, 680, 400], radius=24, fill='#DCFCE7', outline=NAVY, width=5)
d2.rounded_rectangle([400, 60, 650, 310], radius=16, fill='#FFFFFF', outline=NAVY, width=3)
# Tablet screen graphics (Voxie silhouette + soundwaves)
d2.ellipse([490, 110, 560, 180], fill=ORANGE, outline=NAVY, width=4)
d2.arc([575, 125, 615, 165], start=-60, end=60, fill=YELLOW, width=5)
d2.arc([590, 115, 635, 175], start=-60, end=60, fill=BLUE, width=5)
# Green checkmark
d2.line([(500, 230), (520, 260)], fill=MINT, width=8)
d2.line([(520, 260), (560, 210)], fill=MINT, width=8)
# Smiling icon
d2.ellipse([495, 325, 555, 385], fill=YELLOW, outline=NAVY, width=4)
d2.arc([510, 345, 540, 370], start=0, end=180, fill=NAVY, width=4)
img2.save("assets_70_30/split_problem.png")

# 2. Slide 3: Big game screen mockup with colorful soundwaves
img3 = Image.new('RGBA', (800, 450), (255, 255, 255, 0))
d3 = ImageDraw.Draw(img3)
# Monitor/Tablet frame
d3.rounded_rectangle([40, 30, 760, 410], radius=28, fill='#FFFFFF', outline=NAVY, width=6)
d3.rounded_rectangle([60, 50, 740, 390], radius=18, fill='#EFF6FF', outline=NAVY, width=3)
# Game arena elements
d3.ellipse([140, 140, 300, 300], fill=ORANGE, outline=NAVY, width=5)
d3.polygon([(170, 140), (130, 70), (220, 120)], fill=ORANGE, outline=NAVY, width=4)
d3.polygon([(270, 140), (310, 70), (220, 120)], fill=ORANGE, outline=NAVY, width=4)
# Muzzle & eyes
d3.chord([180, 200, 260, 280], start=0, end=180, fill=CREAM, outline=NAVY, width=4)
d3.ellipse([210, 225, 230, 240], fill=NAVY)
d3.ellipse([180, 180, 200, 200], fill=NAVY)
d3.ellipse([240, 180, 260, 200], fill=NAVY)
# Microphone
d3.rounded_rectangle([340, 190, 380, 250], radius=10, fill=BLUE, outline=NAVY, width=4)
d3.line([(360, 250), (360, 290)], fill=NAVY, width=5)
d3.ellipse([335, 285, 385, 305], fill=YELLOW, outline=NAVY, width=3)
# Bold colorful radiating soundwaves
for r, col, w in [(420, YELLOW, 8), (470, BLUE, 8), (520, CORAL, 8), (570, MINT, 8), (620, ORANGE, 8)]:
    d3.arc([200, 220 - r//2, 200 + r, 220 + r//2], start=-55, end=55, fill=col, width=w)
# Star particles
for sx, sy in [(650, 120), (690, 260), (450, 90), (580, 330)]:
    d3.ellipse([sx, sy, sx+18, sy+18], fill=YELLOW, outline=NAVY, width=3)
img3.save("assets_70_30/game_soundwaves.png")

# 3. Slide 5: The 5 mini-game cards
def make_game_card(icon_text, title, color_hex, filename):
    c = Image.new('RGBA', (260, 360), (255, 255, 255, 0))
    dc = ImageDraw.Draw(c)
    dc.rounded_rectangle([10, 10, 250, 350], radius=24, fill=color_hex, outline=NAVY, width=5)
    dc.rounded_rectangle([25, 30, 235, 200], radius=16, fill='#FFFFFF', outline=NAVY, width=3)
    # Circle badge
    dc.ellipse([90, 80, 170, 160], fill=YELLOW, outline=NAVY, width=4)
    # Bottom sticker
    dc.rounded_rectangle([30, 240, 230, 320], radius=14, fill=CREAM, outline=NAVY, width=3)
    c.save(f"assets_70_30/{filename}")

make_game_card("🚀", "Letter Rocket", CORAL, "card_rocket.png")
make_game_card("🔢", "Number Jump", BLUE, "card_number.png")
make_game_card("🎨", "Color Splash", YELLOW, "card_color.png")
make_game_card("🦁", "Animal Safari", MINT, "card_safari.png")
make_game_card("🧩", "Word Builder", ORANGE, "card_words.png")

# 4. Slide 8: Tilted phone mockups for Streak and Skill Garden
def make_phone(title, inner_col, badge_text, filename):
    ph = Image.new('RGBA', (300, 480), (255, 255, 255, 0))
    dp = ImageDraw.Draw(ph)
    # Phone Body
    dp.rounded_rectangle([15, 15, 285, 465], radius=36, fill='#FFFFFF', outline=NAVY, width=6)
    # Screen
    dp.rounded_rectangle([30, 45, 270, 435], radius=24, fill=inner_col, outline=NAVY, width=3)
    # Speaker notch
    dp.rounded_rectangle([115, 25, 185, 33], radius=4, fill=NAVY)
    # Inner card
    dp.rounded_rectangle([45, 75, 255, 250], radius=18, fill='#FFFFFF', outline=NAVY, width=3)
    # 5 stars / items
    for i in range(5):
        dp.ellipse([60 + i*38, 140, 92 + i*38, 172], fill=YELLOW, outline=NAVY, width=2)
    # Banner sticker
    dp.rounded_rectangle([45, 280, 255, 380], radius=16, fill=YELLOW, outline=NAVY, width=3)
    ph.save(f"assets_70_30/{filename}")

make_phone("Streak", "#FFFBEB", "🔥 5-Day Streak", "phone_streak.png")
make_phone("Garden", "#F0FDF4", "🌻 5 Flowers Blooming", "phone_garden.png")

print("All 70/30 assets generated successfully!")
