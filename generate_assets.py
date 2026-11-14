import os
import pptx
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE
from PIL import Image, ImageDraw

def create_mascot_images():
    os.makedirs("assets_deck", exist_ok=True)
    
    # 1. Standard happy mascot
    img = Image.new('RGBA', (320, 320), (255, 255, 255, 0))
    d = ImageDraw.Draw(img)
    # Ears
    d.polygon([(90, 120), (55, 45), (135, 90)], fill='#FF9F43', outline='#1E2A4A', width=5)
    d.polygon([(230, 120), (265, 45), (185, 90)], fill='#FF9F43', outline='#1E2A4A', width=5)
    d.polygon([(85, 110), (68, 60), (120, 90)], fill='#FFFDF7', outline='#1E2A4A', width=3)
    d.polygon([(235, 110), (252, 60), (200, 90)], fill='#FFFDF7', outline='#1E2A4A', width=3)
    # Head
    d.ellipse([70, 80, 250, 230], fill='#FF9F43', outline='#1E2A4A', width=5)
    # White muzzle
    d.chord([100, 130, 220, 220], start=0, end=180, fill='#FFFDF7', outline='#1E2A4A', width=4)
    # Nose
    d.ellipse([150, 155, 170, 170], fill='#1E2A4A')
    # Eyes
    d.ellipse([110, 120, 135, 145], fill='#1E2A4A')
    d.ellipse([115, 123, 122, 130], fill='#FFFFFF')
    d.ellipse([185, 120, 210, 145], fill='#1E2A4A')
    d.ellipse([190, 123, 197, 130], fill='#FFFFFF')
    # Cheeks
    d.ellipse([90, 155, 110, 175], fill='#FF6B6B')
    d.ellipse([210, 155, 230, 175], fill='#FF6B6B')
    # Smile
    d.arc([140, 165, 180, 195], start=20, end=160, fill='#1E2A4A', width=4)
    img.save("assets_deck/voxie_happy.png")

    # 2. Hero Mascot with retro microphone
    hero = Image.new('RGBA', (400, 400), (255, 255, 255, 0))
    hd = ImageDraw.Draw(hero)
    # Paste mascot
    hero.paste(img, (20, 50), img)
    # Retro Mic
    hd.rectangle([250, 220, 280, 310], fill='#1E2A4A')
    hd.ellipse([230, 300, 300, 330], fill='#FFD166', outline='#1E2A4A', width=4)
    hd.rounded_rectangle([240, 140, 290, 220], radius=15, fill='#4D96FF', outline='#1E2A4A', width=4)
    # Soundwaves
    hd.arc([305, 150, 345, 210], start=-60, end=60, fill='#FFD166', width=5)
    hd.arc([320, 135, 370, 225], start=-60, end=60, fill='#FF9F43', width=5)
    hero.save("assets_deck/voxie_hero.png")

    # 3. Waving / Celebrating mascot
    wave = Image.new('RGBA', (360, 360), (255, 255, 255, 0))
    wd = ImageDraw.Draw(wave)
    wave.paste(img, (20, 40), img)
    # Hand waving
    wd.ellipse([240, 60, 290, 110], fill='#FF9F43', outline='#1E2A4A', width=4)
    # Sparkle star
    wd.polygon([(300, 40), (305, 55), (320, 60), (305, 65), (300, 80), (295, 65), (280, 60), (295, 55)], fill='#FFD166', outline='#1E2A4A', width=2)
    wave.save("assets_deck/voxie_waving.png")

    # 4. QR Code mockup placeholder image
    qr = Image.new('RGBA', (300, 300), (255, 253, 247, 255))
    qd = ImageDraw.Draw(qr)
    qd.rectangle([10, 10, 290, 290], outline='#1E2A4A', width=6)
    qd.rectangle([30, 30, 90, 90], fill='#1E2A4A')
    qd.rectangle([45, 45, 75, 75], fill='#FFFDF7')
    qd.rectangle([210, 30, 270, 90], fill='#1E2A4A')
    qd.rectangle([225, 45, 255, 75], fill='#FFFDF7')
    qd.rectangle([30, 210, 90, 270], fill='#1E2A4A')
    qd.rectangle([45, 225, 75, 255], fill='#FFFDF7')
    # Decorative blocks
    for x, y in [(120, 40), (150, 70), (130, 120), (170, 140), (210, 170), (140, 220), (180, 240), (240, 230)]:
        qd.rectangle([x, y, x+30, y+30], fill='#1E2A4A')
    qr.save("assets_deck/qr_mock.png")

create_mascot_images()
print("Assets created successfully!")
