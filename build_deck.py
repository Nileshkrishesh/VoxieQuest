import os
import pptx
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

# ----------------- PALETTE -----------------
CREAM = RGBColor(255, 253, 247)      # #FFFDF7
NAVY = RGBColor(30, 42, 74)          # #1E2A4A
YELLOW = RGBColor(255, 209, 102)     # #FFD166
BLUE = RGBColor(77, 150, 255)        # #4D96FF
CORAL = RGBColor(255, 107, 107)      # #FF6B6B
MINT = RGBColor(107, 203, 119)       # #6BCB77
ORANGE = RGBColor(255, 159, 67)      # #FF9F43
WHITE = RGBColor(255, 255, 255)
CARD_BG = RGBColor(254, 251, 240)    # Soft cream card
BORDER_WIDTH = Pt(2.5)

FONT_TITLE = "Fredoka"
FONT_BODY = "Nunito"

prs = pptx.Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
blank_layout = prs.slide_layouts[6]

def set_slide_background(slide):
    bg = slide.background
    fill = bg.fill
    fill.solid()
    fill.fore_color.rgb = CREAM

def add_header(slide, tag_text, title_text, tag_color=YELLOW):
    # Category Pill Tag
    tag_box = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.8), Inches(0.4), Inches(2.8), Inches(0.4))
    tag_box.fill.solid()
    tag_box.fill.fore_color.rgb = tag_color
    tag_box.line.color.rgb = NAVY
    tag_box.line.width = Pt(2)
    tf = tag_box.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = tag_text.upper()
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.alignment = PP_ALIGN.CENTER

    # Title Text
    t_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.85), Inches(11.5), Inches(0.8))
    tf2 = t_box.text_frame
    tf2.word_wrap = True
    p2 = tf2.paragraphs[0]
    p2.text = title_text
    p2.font.name = FONT_TITLE
    p2.font.size = Pt(28)
    p2.font.bold = True
    p2.font.color.rgb = NAVY

def add_footer(slide, slide_num, mascot_pose="assets_deck/voxie_happy.png", mascot_pos=(Inches(11.8), Inches(5.9))):
    # Footer line
    line = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), Inches(7.0), Inches(11.7), Pt(1.5))
    line.fill.solid()
    line.fill.fore_color.rgb = NAVY
    line.line.fill.background()

    # Footer left text
    ft_box = slide.shapes.add_textbox(Inches(0.8), Inches(7.02), Inches(8), Inches(0.35))
    p = ft_box.text_frame.paragraphs[0]
    p.text = "VOXIE QUEST • Speak, Play, Learn! | Blazor WebAssembly (.NET 8)"
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.bold = True
    p.font.color.rgb = NAVY

    # Footer right slide number
    num_box = slide.shapes.add_textbox(Inches(10.5), Inches(7.02), Inches(2.0), Inches(0.35))
    p_num = num_box.text_frame.paragraphs[0]
    p_num.text = f"Slide {slide_num} of 16"
    p_num.font.name = FONT_BODY
    p_num.font.size = Pt(10)
    p_num.font.bold = True
    p_num.font.color.rgb = NAVY
    p_num.alignment = PP_ALIGN.RIGHT

    # Mascot corner sticker
    if mascot_pose and os.path.exists(mascot_pose) and mascot_pos:
        slide.shapes.add_picture(mascot_pose, mascot_pos[0], mascot_pos[1], width=Inches(1.2), height=Inches(1.2))

def create_card(slide, left, top, width, height, bg_color=CARD_BG, border_color=NAVY, radius=MSO_SHAPE.ROUNDED_RECTANGLE):
    card = slide.shapes.add_shape(radius, left, top, width, height)
    card.fill.solid()
    card.fill.fore_color.rgb = bg_color
    card.line.color.rgb = border_color
    card.line.width = BORDER_WIDTH
    return card

def add_bullet_list(slide, left, top, width, height, bullets, bg_color=CARD_BG):
    create_card(slide, left, top, width, height, bg_color)
    tb = slide.shapes.add_textbox(left + Inches(0.2), top + Inches(0.25), width - Inches(0.4), height - Inches(0.5))
    tf = tb.text_frame
    tf.word_wrap = True
    for i, b in enumerate(bullets):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = f"•  {b}"
        p.font.name = FONT_BODY
        p.font.size = Pt(18)
        p.font.bold = True
        p.font.color.rgb = NAVY
        p.space_after = Pt(14)

def set_note(slide, note_text):
    slide.notes_slide.notes_text_frame.text = note_text

# ==============================================================================
# SLIDE 1: TITLE & HERO
# ==============================================================================
s1 = prs.slides.add_slide(blank_layout)
set_slide_background(s1)

# Left Metadata Card
card1 = create_card(s1, Inches(0.8), Inches(0.8), Inches(6.8), Inches(5.8), CARD_BG)
tb1 = s1.shapes.add_textbox(Inches(1.1), Inches(1.0), Inches(6.2), Inches(5.4))
tf1 = tb1.text_frame
tf1.word_wrap = True

p = tf1.paragraphs[0]
p.text = "VOXIE QUEST"
p.font.name = FONT_TITLE
p.font.size = Pt(36)
p.font.bold = True
p.font.color.rgb = CORAL

p = tf1.add_paragraph()
p.text = "Speak, Play, Learn!"
p.font.name = FONT_TITLE
p.font.size = Pt(22)
p.font.bold = True
p.font.color.rgb = BLUE
p.space_after = Pt(16)

p = tf1.add_paragraph()
p.text = "Voice-Controlled Gaming Tools for Enhanced Learning in the Skill Ecosystem"
p.font.name = FONT_TITLE
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p.space_after = Pt(20)

details = [
    "Candidate Name: [NAME]",
    "Roll Number: [ROLL NO]",
    "Degree / Course: [COURSE]",
    "College / Institution: [COLLEGE]",
    "Project Guide: [GUIDE]",
    "Academic Year: [YEAR]"
]
for d in details:
    p = tf1.add_paragraph()
    p.text = d
    p.font.name = FONT_BODY
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.space_after = Pt(4)

# Right Hero Visual Box
create_card(s1, Inches(7.9), Inches(0.8), Inches(4.6), Inches(5.8), YELLOW)
if os.path.exists("assets_deck/voxie_hero.png"):
    s1.shapes.add_picture("assets_deck/voxie_hero.png", Inches(8.3), Inches(1.2), width=Inches(3.8), height=Inches(3.8))

hero_bubble = create_card(s1, Inches(8.2), Inches(4.9), Inches(4.0), Inches(1.3), WHITE)
hbt = s1.shapes.add_textbox(Inches(8.3), Inches(5.0), Inches(3.8), Inches(1.1))
hbp = hbt.text_frame.paragraphs[0]
hbp.text = "✨ Speak into the mic to play!\nBlazor WASM • .NET 8 • Vercel"
hbp.font.name = FONT_TITLE
hbp.font.size = Pt(14)
hbp.font.bold = True
hbp.font.color.rgb = NAVY
hbp.alignment = PP_ALIGN.CENTER

add_footer(s1, 1, None, None)
set_note(s1, "Welcome, everyone; today I present Voxie Quest, a voice-controlled educational web game designed to make foundational learning engaging and accessible for children.")

# ==============================================================================
# SLIDE 2: PROBLEM (Text-Left / Visual-Right)
# ==============================================================================
s2 = prs.slides.add_slide(blank_layout)
set_slide_background(s2)
add_header(s2, "Problem Statement", "Why Children Lose Traditional Learning Interest", CORAL)

bullets2 = [
    "Rote worksheets cause passive disengagement and fatigue.",
    "Clunky keyboards frustrate developing early fine-motor skills.",
    "Static drills fail to adapt to individual paces.",
    "Commercial apps overwhelm young kids with distracting advertisements."
]
add_bullet_list(s2, Inches(0.8), Inches(1.8), Inches(5.6), Inches(4.9), bullets2)

# Visual Right: Before vs After graphic
create_card(s2, Inches(6.8), Inches(1.8), Inches(5.7), Inches(4.9), WHITE)

# Traditional Box
create_card(s2, Inches(7.1), Inches(2.1), Inches(5.1), Inches(1.8), CORAL)
tb_trad = s2.shapes.add_textbox(Inches(7.2), Inches(2.2), Inches(4.9), Inches(1.6))
p = tb_trad.text_frame.paragraphs[0]
p.text = "❌  Traditional Worksheet Learning"
p.font.name = FONT_TITLE
p.font.size = Pt(15)
p.font.bold = True
p.font.color.rgb = WHITE
p = tb_trad.text_frame.add_paragraph()
p.text = "• Passive memorization & rote writing\n• Fine-motor stress: keyboards & mice\n• High cognitive fatigue & fast boredom"
p.font.name = FONT_BODY
p.font.size = Pt(12)
p.font.color.rgb = WHITE

# Arrow
arr = s2.shapes.add_shape(MSO_SHAPE.DOWN_ARROW, Inches(9.3), Inches(4.0), Inches(0.7), Inches(0.5))
arr.fill.solid()
arr.fill.fore_color.rgb = YELLOW
arr.line.color.rgb = NAVY
arr.line.width = Pt(2)

# Voice Game Box
create_card(s2, Inches(7.1), Inches(4.6), Inches(5.1), Inches(1.8), MINT)
tb_vox = s2.shapes.add_textbox(Inches(7.2), Inches(4.7), Inches(4.9), Inches(1.6))
p = tb_vox.text_frame.paragraphs[0]
p.text = "✅  Voxie Voice-Led Gaming"
p.font.name = FONT_TITLE
p.font.size = Pt(15)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_vox.text_frame.add_paragraph()
p.text = "• Natural speech input: immediate response\n• Playful feedback with stars & chimes\n• Active vocal confidence & cognitive flow"
p.font.name = FONT_BODY
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY

add_footer(s2, 2)
set_note(s2, "Traditional early childhood learning tools rely on passive repetition and motor-heavy typing, leading to frustration where voice-led interactive play offers natural engagement.")

# ==============================================================================
# SLIDE 3: OBJECTIVES (Visual-Left / Text-Right)
# ==============================================================================
s3 = prs.slides.add_slide(blank_layout)
set_slide_background(s3)
add_header(s3, "Project Scope", "Core Objectives of Voxie Quest", BLUE)

# Visual Left: Target Rings
create_card(s3, Inches(0.8), Inches(1.8), Inches(5.6), Inches(4.9), CARD_BG)
target_colors = [BLUE, MINT, YELLOW, CORAL]
target_labels = [
    ("4. Child Delight", "Flow state, joy & zero failure penalty"),
    ("3. Adaptive Engine", "Botanical tiers & dynamic hints"),
    ("2. Skill Diversity", "Phonics, math, colors, fauna, words"),
    ("1. Accessibility", "Browser voice control with zero setup")
]
for idx, (lbl, sub) in enumerate(reversed(target_labels)):
    w = 5.0 - (idx * 0.8)
    h = 4.3 - (idx * 0.8)
    l = 1.1 + (idx * 0.4)
    t = 2.1 + (idx * 0.4)
    c = create_card(s3, Inches(l), Inches(t), Inches(w), Inches(h), target_colors[idx])
    tb = s3.shapes.add_textbox(Inches(l+0.1), Inches(t+0.1), Inches(w-0.2), Inches(0.8))
    p = tb.text_frame.paragraphs[0]
    p.text = lbl
    p.font.name = FONT_TITLE
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = WHITE if idx != 2 else NAVY
    p.alignment = PP_ALIGN.CENTER
    p = tb.text_frame.add_paragraph()
    p.text = sub
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if idx != 2 else NAVY
    p.alignment = PP_ALIGN.CENTER

# Text Right
bullets3 = [
    "Deliver natural voice gameplay using browser speech recognition.",
    "Build five skill games covering phonics, math, and words.",
    "Implement an adaptive difficulty engine for flow state.",
    "Ensure zero-cost, offline-capable privacy for young learners."
]
add_bullet_list(s3, Inches(6.8), Inches(1.8), Inches(5.7), Inches(4.9), bullets3)

add_footer(s3, 3)
set_note(s3, "Our project aims to turn speech into an intuitive educational controller while keeping children in an adaptive flow state with zero privacy compromises.")

# ==============================================================================
# SLIDE 4: EXISTING SYSTEMS VS OURS (Text-Left / Visual-Right Table)
# ==============================================================================
s4 = prs.slides.add_slide(blank_layout)
set_slide_background(s4)
add_header(s4, "Market Analysis", "Competitive Landscape and Feature Matrix", YELLOW)

bullets4 = [
    "Most existing apps require keyboard or touch typing.",
    "Commercial competitors harvest telemetry and show intrusive ads.",
    "Voxie Quest runs 100% locally with zero backend tracking.",
    "Native speech tolerance handles developing childhood pronunciation quirks."
]
add_bullet_list(s4, Inches(0.8), Inches(1.8), Inches(5.2), Inches(4.9), bullets4)

# Visual Right: Table Graphic
create_card(s4, Inches(6.3), Inches(1.8), Inches(6.2), Inches(4.9), WHITE)
table_shape = s4.shapes.add_table(6, 4, Inches(6.5), Inches(2.0), Inches(5.8), Inches(4.4))
tbl = table_shape.table

headers = ["Key Feature", "Traditional", "Voice AI", "Voxie Quest"]
row_data = [
    ["Voice Control", "❌ Touch", "⚠️ Cloud", "✅ Native API"],
    ["Offline Mode", "⚠️ Partial", "❌ Cloud Only", "✅ Standalone"],
    ["Child Privacy", "❌ Trackers", "❌ Telemetry", "✅ 100% Local"],
    ["Adaptive Flow", "❌ Static", "⚠️ Paywall", "✅ 8-Win Engine"],
    ["Parent Controls", "❌ Rare", "⚠️ Account", "✅ PIN Gated"]
]

for col_idx, h in enumerate(headers):
    cell = tbl.cell(0, col_idx)
    cell.text = h
    cell.fill.solid()
    cell.fill.fore_color.rgb = BLUE if col_idx == 3 else YELLOW
    p = cell.text_frame.paragraphs[0]
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE if col_idx == 3 else NAVY
    p.alignment = PP_ALIGN.CENTER

for row_idx, r in enumerate(row_data):
    for col_idx, val in enumerate(r):
        cell = tbl.cell(row_idx+1, col_idx)
        cell.text = val
        cell.fill.solid()
        cell.fill.fore_color.rgb = CARD_BG if col_idx != 3 else MINT
        p = cell.text_frame.paragraphs[0]
        p.font.name = FONT_BODY
        p.font.size = Pt(10)
        p.font.bold = True
        p.font.color.rgb = NAVY
        p.alignment = PP_ALIGN.CENTER if col_idx > 0 else PP_ALIGN.LEFT

add_footer(s4, 4)
set_note(s4, "Unlike commercial products that require cloud accounts and show ads, Voxie Quest provides private, browser-native voice learning with adaptive child speech tolerance.")

# ==============================================================================
# SLIDE 5: PROPOSED SOLUTION (Full-Width Mind Map with Text Strip Above)
# ==============================================================================
s5 = prs.slides.add_slide(blank_layout)
set_slide_background(s5)
add_header(s5, "Proposed Solution", "Voxie Quest Unified Learning Ecosystem", MINT)

# Top Text Strip
create_card(s5, Inches(0.8), Inches(1.8), Inches(11.7), Inches(1.1), CARD_BG)
tb5 = s5.shapes.add_textbox(Inches(1.0), Inches(1.85), Inches(11.3), Inches(1.0))
tf5 = tb5.text_frame
tf5.word_wrap = True
p = tf5.paragraphs[0]
p.text = "• Unified voice-first web architecture built on Blazor WebAssembly.\n• Five foundational curriculum mini-games powered by speech recognition.\n• Self-adjusting botanical difficulty tiers tailored to child age."
p.font.name = FONT_BODY
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY

# Mind Map Center
create_card(s5, Inches(5.3), Inches(4.3), Inches(2.7), Inches(1.5), YELLOW, radius=MSO_SHAPE.OVAL)
tb_core = s5.shapes.add_textbox(Inches(5.4), Inches(4.6), Inches(2.5), Inches(1.0))
p = tb_core.text_frame.paragraphs[0]
p.text = "VOXIE QUEST\nBlazor Core"
p.font.name = FONT_TITLE
p.font.size = Pt(15)
p.font.bold = True
p.font.color.rgb = NAVY
p.alignment = PP_ALIGN.CENTER

# 6 Branches
branches = [
    ("🎤 Voice Engine", "Web Speech + Fuzzy Match", BLUE, Inches(1.2), Inches(3.3)),
    ("🎮 5 Skill Games", "Phonics, Math, Animals...", CORAL, Inches(5.3), Inches(3.0)),
    ("🌱 Adaptive Model", "Seedling, Sapling, Big Tree", MINT, Inches(9.2), Inches(3.3)),
    ("🔥 Daily Streaks", "Consistency habit builder", ORANGE, Inches(1.2), Inches(5.3)),
    ("🌻 Skill Garden", "Visual blooms per mastery", YELLOW, Inches(5.3), Inches(5.9)),
    ("🛡️ Parent Gate", "Analytics & PIN controls", BLUE, Inches(9.2), Inches(5.3))
]
for title, desc, col, l, t in branches:
    b_card = create_card(s5, l, t, Inches(2.9), Inches(1.1), col)
    tb = s5.shapes.add_textbox(l+Inches(0.1), t+Inches(0.1), Inches(2.7), Inches(0.9))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.name = FONT_TITLE
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p = tb.text_frame.add_paragraph()
    p.text = desc
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY

add_footer(s5, 5)
set_note(s5, "Voxie Quest connects browser voice processing, five curriculum games, adaptive botanical tiers, and parent insights into one cohesive web ecosystem.")

# ==============================================================================
# SLIDE 6: SYSTEM ARCHITECTURE (Visual-Left / Text-Right)
# ==============================================================================
s6 = prs.slides.add_slide(blank_layout)
set_slide_background(s6)
add_header(s6, "Technical Architecture", "Layered Blazor WebAssembly System Architecture", BLUE)

# Visual Left: 4 Stacked Layers
create_card(s6, Inches(0.8), Inches(1.8), Inches(5.8), Inches(4.9), WHITE)
layers = [
    ("1. Presentation Layer (Razor & CSS)", "LevelMap, 5 Game Arenas, Candy UI, SVG Maps", BLUE),
    ("2. JS Interop Layer", "Web Speech API, Audio Synthesis, Vosk fallback", MINT),
    ("3. Core Domain Services (C# .NET 8)", "IDifficultyService, ISpeechMatcher, ISaveService", YELLOW),
    ("4. Persistence & Edge Hosting", "Client LocalStorage + Static Edge on Vercel", ORANGE)
]
for idx, (title, desc, col) in enumerate(layers):
    t_top = 2.1 + (idx * 1.1)
    create_card(s6, Inches(1.1), Inches(t_top), Inches(5.2), Inches(0.9), col)
    tb = s6.shapes.add_textbox(Inches(1.2), Inches(t_top+0.05), Inches(5.0), Inches(0.8))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.name = FONT_TITLE
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE] else NAVY
    p = tb.text_frame.add_paragraph()
    p.text = desc
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if col in [BLUE] else NAVY

# Text Right
bullets6 = [
    "Presentation Layer: Blazor components render responsive hand-drawn UI.",
    "Interaction Layer: JavaScript Interop bridges Web Speech and audio.",
    "Domain Layer: Pure C# services drive game and difficulty logic.",
    "Persistence Layer: LocalStorage retains progress without external servers."
]
add_bullet_list(s6, Inches(6.9), Inches(1.8), Inches(5.6), Inches(4.9), bullets6)

add_footer(s6, 6)
set_note(s6, "The architecture separates Blazor Razor UI from pure C# business services, using JavaScript interop only for browser speech hardware access and local persistence.")

# ==============================================================================
# SLIDE 7: VOICE ANSWER FLOW (Text-Left / Visual-Right)
# ==============================================================================
s7 = prs.slides.add_slide(blank_layout)
set_slide_background(s7)
add_header(s7, "Speech Pipeline", "Real-Time Voice Processing Pipeline", ORANGE)

bullets7 = [
    "Child speaks answer into browser microphone.",
    "Web Speech API streams real-time speech transcript.",
    "SpeechMatcher evaluates phonetics and string similarity.",
    "Correct answers unlock rewards; misses trigger hints gracefully."
]
add_bullet_list(s7, Inches(0.8), Inches(1.8), Inches(5.4), Inches(4.9), bullets7)

# Visual Right: Flowchart
create_card(s7, Inches(6.5), Inches(1.8), Inches(6.0), Inches(4.9), WHITE)
flow_nodes = [
    ("1. Child Speaks", Inches(6.8), Inches(2.0), Inches(2.5), Inches(0.6), YELLOW),
    ("2. Web Speech API", Inches(9.6), Inches(2.0), Inches(2.5), Inches(0.6), BLUE),
    ("3. SpeechMatcher (C#)", Inches(6.8), Inches(2.9), Inches(5.3), Inches(0.65), CARD_BG),
    ("Match?", Inches(8.8), Inches(3.8), Inches(1.5), Inches(0.7), ORANGE),
    ("✅ YES: Star Burst + Micro-Level Up", Inches(6.8), Inches(4.7), Inches(5.3), Inches(0.7), MINT),
    ("❌ NO: Gentle Hint & Voice Retry (Max 3)", Inches(6.8), Inches(5.6), Inches(5.3), Inches(0.7), CORAL)
]
for title, l, t, w, h, col in flow_nodes:
    create_card(s7, l, t, w, h, col)
    tb = s7.shapes.add_textbox(l+Inches(0.05), t+Inches(0.05), w-Inches(0.1), h-Inches(0.1))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER

add_footer(s7, 7)
set_note(s7, "Audio input flows from the child's microphone through browser speech recognition to our C# fuzzy matcher, which provides instant encouraging feedback or gentle hints.")

# ==============================================================================
# SLIDE 8: SPEECH MATCHING WORKED EXAMPLE (Visual-Left / Text-Right)
# ==============================================================================
s8 = prs.slides.add_slide(blank_layout)
set_slide_background(s8)
add_header(s8, "Algorithm Deep Dive", "Fuzzy Speech Matching with Child Pronunciation", BLUE)

# Visual Left: Worked Example Graphic
create_card(s8, Inches(0.8), Inches(1.8), Inches(5.8), Inches(4.9), WHITE)

# Target vs Spoken Header
tb_ex = s8.shapes.add_textbox(Inches(1.0), Inches(2.0), Inches(5.4), Inches(0.7))
p = tb_ex.text_frame.paragraphs[0]
p.text = "Target Word: 'RED'  vs  Child Spoke: 'WED'"
p.font.name = FONT_TITLE
p.font.size = Pt(14)
p.font.bold = True
p.font.color.rgb = CORAL

# Box 1: Levenshtein
create_card(s8, Inches(1.1), Inches(2.7), Inches(5.2), Inches(1.4), BLUE)
tb_lev = s8.shapes.add_textbox(Inches(1.2), Inches(2.8), Inches(5.0), Inches(1.2))
p = tb_lev.text_frame.paragraphs[0]
p.text = "1. Levenshtein Edit Distance"
p.font.name = FONT_TITLE
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = WHITE
p = tb_lev.text_frame.add_paragraph()
p.text = "• Matrix substitution: [W] -> [R] (Cost = 1)\n• Distance = 1 | Word Length = 3\n• Similarity = 1 - (1/3) = 66.7% (Above 60% threshold)"
p.font.name = FONT_BODY
p.font.size = Pt(10)
p.font.color.rgb = WHITE

# Box 2: Soundex
create_card(s8, Inches(1.1), Inches(4.3), Inches(5.2), Inches(1.3), MINT)
tb_sdx = s8.shapes.add_textbox(Inches(1.2), Inches(4.4), Inches(5.0), Inches(1.1))
p = tb_sdx.text_frame.paragraphs[0]
p.text = "2. Soundex Phonetic Encoding"
p.font.name = FONT_TITLE
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_sdx.text_frame.add_paragraph()
p.text = "• Detects liquid glide substitution (W <-> R common in ages 5-7)\n• Phonetic class tolerance validates legitimate attempt"
p.font.name = FONT_BODY
p.font.size = Pt(10)
p.font.color.rgb = NAVY

# Verdict Banner
create_card(s8, Inches(1.1), Inches(5.8), Inches(5.2), Inches(0.7), YELLOW)
tb_vrd = s8.shapes.add_textbox(Inches(1.2), Inches(5.85), Inches(5.0), Inches(0.6))
p = tb_vrd.text_frame.paragraphs[0]
p.text = "⭐ VERDICT: ACCEPTED AS CORRECT! (Reward Star)"
p.font.name = FONT_TITLE
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY
p.alignment = PP_ALIGN.CENTER

# Text Right
bullets8 = [
    "Children often substitute letters like 'w' for 'r'.",
    "Exact string matching incorrectly penalizes developing articulation.",
    "Soundex groups phonetic sounds: 'W' and 'R' match.",
    "Levenshtein distance calculates single character edit distance."
]
add_bullet_list(s8, Inches(6.9), Inches(1.8), Inches(5.6), Inches(4.9), bullets8)

add_footer(s8, 8)
set_note(s8, "By combining Levenshtein edit distance with Soundex phonetic encoding, Voxie Quest accepts natural childhood approximations like saying 'wed' for 'red' without penalizing the child.")

# ==============================================================================
# SLIDE 9: THE 5 MINI-GAMES (Full-Width Strip with Text Strip Above)
# ==============================================================================
s9 = prs.slides.add_slide(blank_layout)
set_slide_background(s9)
add_header(s9, "Curriculum Content", "Five Curriculum Mini-Games and Targeted Skills", CORAL)

# Top Text Strip
create_card(s9, Inches(0.8), Inches(1.8), Inches(11.7), Inches(1.1), CARD_BG)
tb9 = s9.shapes.add_textbox(Inches(1.0), Inches(1.85), Inches(11.3), Inches(1.0))
tf9 = tb9.text_frame
tf9.word_wrap = True
p = tf9.paragraphs[0]
p.text = "• Letter Rocket builds phonics, alphabet sounds, and letter recognition.\n• Number Jump reinforces mental arithmetic and counting fluency.\n• Color Splash enhances color vocabulary and rapid visual matching.\n• Animal Safari sharpens listening comprehension and creature identification."
p.font.name = FONT_BODY
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY

# 5 Games Strip Below
games = [
    ("🚀 Letter Rocket", "Phonics & Letters", "Say 'B' to boost rocket", CORAL),
    ("🔢 Number Jump", "Mental Arithmetic", "Answer '3 + 4' to hop lilypad", BLUE),
    ("🎨 Color Splash", "Color Recognition", "Name the Azure paint splash", YELLOW),
    ("🦁 Animal Safari", "Auditory Listening", "Identify creature from sound", MINT),
    ("🧩 Word Builder", "Spelling & Blends", "Spell 'CAT' phoneme by phoneme", ORANGE)
]
for idx, (g_name, g_skill, g_mech, g_col) in enumerate(games):
    g_left = 0.8 + (idx * 2.38)
    create_card(s9, Inches(g_left), Inches(3.2), Inches(2.25), Inches(3.5), g_col)
    tb = s9.shapes.add_textbox(Inches(g_left+0.1), Inches(3.4), Inches(2.05), Inches(3.1))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = g_name
    p.font.name = FONT_TITLE
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = WHITE if g_col in [CORAL, BLUE] else NAVY
    p.space_after = Pt(10)

    p = tf.add_paragraph()
    p.text = f"🎯 Skill:\n{g_skill}"
    p.font.name = FONT_BODY
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE if g_col in [CORAL, BLUE] else NAVY
    p.space_after = Pt(10)

    p = tf.add_paragraph()
    p.text = f"🎮 Gameplay:\n{g_mech}"
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if g_col in [CORAL, BLUE] else NAVY

add_footer(s9, 9)
set_note(s9, "The platform features five distinct mini-games designed to exercise phonics, mental arithmetic, color identification, animal sounds, and word spelling.")

# ==============================================================================
# SLIDE 10: LEVELS AND ADAPTIVE DIFFICULTY (Text-Left / Visual-Right)
# ==============================================================================
s10 = prs.slides.add_slide(blank_layout)
set_slide_background(s10)
add_header(s10, "Adaptive Engine", "Flow-State Adaptive Difficulty Logic", MINT)

bullets10 = [
    "Botanical difficulty tiers: Seedling, Sapling, and Big Tree.",
    "Hidden micro-level (1-100) adjusts across an eight-answer rolling window.",
    "Target success rate maintained between seventy and eighty percent.",
    "Consecutive misses add hints before lowering any tier."
]
add_bullet_list(s10, Inches(0.8), Inches(1.8), Inches(5.4), Inches(4.9), bullets10)

# Visual Right: Tiers + Loop + Trail
create_card(s10, Inches(6.5), Inches(1.8), Inches(6.0), Inches(4.9), WHITE)

# 3 Botanical Tiers
tier_info = [
    ("🌱 Seedling", "Ages 3-5 • Untimed", YELLOW, Inches(6.7)),
    ("🌿 Sapling", "Ages 6-8 • Gentle Sun", MINT, Inches(8.5)),
    ("🌳 Big Tree", "Ages 9-12 • Puzzles", BLUE, Inches(10.3))
]
for t_name, t_sub, t_col, t_x in tier_info:
    create_card(s10, t_x, Inches(2.0), Inches(1.7), Inches(1.1), t_col)
    tb = s10.shapes.add_textbox(t_x, Inches(2.05), Inches(1.7), Inches(1.0))
    p = tb.text_frame.paragraphs[0]
    p.text = t_name
    p.font.name = FONT_TITLE
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = WHITE if t_col == BLUE else NAVY
    p.alignment = PP_ALIGN.CENTER
    p = tb.text_frame.add_paragraph()
    p.text = t_sub
    p.font.name = FONT_BODY
    p.font.size = Pt(8.5)
    p.font.color.rgb = WHITE if t_col == BLUE else NAVY
    p.alignment = PP_ALIGN.CENTER

# Adaptive Logic Box
create_card(s10, Inches(6.7), Inches(3.3), Inches(5.6), Inches(1.6), CARD_BG)
tb_adp = s10.shapes.add_textbox(Inches(6.8), Inches(3.4), Inches(5.4), Inches(1.4))
p = tb_adp.text_frame.paragraphs[0]
p.text = "⚙️  Adaptive Flow Control (8-Answer Window)"
p.font.name = FONT_TITLE
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_adp.text_frame.add_paragraph()
p.text = "• 3 Correct in a row ➔ Raise micro-level (Faster if prompt response)\n• 3 Misses in a row ➔ Inject silent help/hint first; never abrupt drop\n• Boredom detected ➔ Trigger Bonus Challenge Sticker round!"
p.font.name = FONT_BODY
p.font.size = Pt(10)
p.font.color.rgb = NAVY

# 10 Level Winding Trail Map Strip
create_card(s10, Inches(6.7), Inches(5.1), Inches(5.6), Inches(1.4), ORANGE)
tb_map = s10.shapes.add_textbox(Inches(6.8), Inches(5.15), Inches(5.4), Inches(1.3))
p = tb_map.text_frame.paragraphs[0]
p.text = "🗺️  Winding Level Map Trail (10 Stops)"
p.font.name = FONT_TITLE
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_map.text_frame.add_paragraph()
p.text = "• Hand-drawn picture-book trail with 10 level stops\n• 1-3 Stars per level for effort & progress\n• Boss Mini-Challenge at Level 5 & 10 with Special Badge"
p.font.name = FONT_BODY
p.font.size = Pt(9.5)
p.font.color.rgb = NAVY

add_footer(s10, 10)
set_note(s10, "Our adaptive model monitors an eight-answer rolling window to keep children in a 70 to 80 percent flow zone, offering gentle hints before ever easing the level.")

# ==============================================================================
# SLIDE 11: STREAK AND SKILL GARDEN (Visual-Left / Text-Right)
# ==============================================================================
s11 = prs.slides.add_slide(blank_layout)
set_slide_background(s11)
add_header(s11, "Gamification", "Gamification through Streaks and Skill Garden", ORANGE)

# Visual Left: Dual Cards
create_card(s11, Inches(0.8), Inches(1.8), Inches(5.6), Inches(4.9), WHITE)

# Card 1: Streak Calendar
create_card(s11, Inches(1.1), Inches(2.1), Inches(5.0), Inches(2.0), ORANGE)
tb_stk = s11.shapes.add_textbox(Inches(1.2), Inches(2.2), Inches(4.8), Inches(1.8))
p = tb_stk.text_frame.paragraphs[0]
p.text = "🔥  Daily Streak Calendar"
p.font.name = FONT_TITLE
p.font.size = Pt(14)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_stk.text_frame.add_paragraph()
p.text = "• Active 5-Day Streak: M [⭐] T [⭐] W [⭐] T [⭐] F [⭐] S [ ] S [ ]\n• Milestone Badges: 3-Day Spark, 7-Day Flame, 30-Day Legend\n• Builds daily learning habit without high-pressure penalties"
p.font.name = FONT_BODY
p.font.size = Pt(10.5)
p.font.color.rgb = NAVY

# Card 2: Skill Garden
create_card(s11, Inches(1.1), Inches(4.3), Inches(5.0), Inches(2.1), MINT)
tb_gdn = s11.shapes.add_textbox(Inches(1.2), Inches(4.4), Inches(4.8), Inches(1.9))
p = tb_gdn.text_frame.paragraphs[0]
p.text = "🌻  The Skill Garden"
p.font.name = FONT_TITLE
p.font.size = Pt(14)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_gdn.text_frame.add_paragraph()
p.text = "• 🌻 Phonics Sunflower (Level 8)\n• 🌷 Math Tulip (Level 6)\n• 🌹 Colors Rose (Level 9)\n• 🌼 Animal Safari Daisy (Level 7)\n• 🪷 Word Builder Lily (Level 5)"
p.font.name = FONT_BODY
p.font.size = Pt(10)
p.font.color.rgb = NAVY

# Text Right
bullets11 = [
    "Daily streak calendar fosters consistent learning habits and discipline.",
    "Milestone streak badges reward 3, 7, and 30-day consistency.",
    "Skill Garden visually blooms new flowers as proficiencies grow.",
    "Positive reinforcement replaces anxiety with proud visual accomplishments."
]
add_bullet_list(s11, Inches(6.8), Inches(1.8), Inches(5.7), Inches(4.9), bullets11)

add_footer(s11, 11)
set_note(s11, "We replace stressful failure metrics with habit-forming streak badges and a blooming Skill Garden where every answered question nurtures a virtual flower.")

# ==============================================================================
# SLIDE 12: SAFETY, PRIVACY AND INDIA EXPLORER (Text-Left / Visual-Right)
# ==============================================================================
s12 = prs.slides.add_slide(blank_layout)
set_slide_background(s12)
add_header(s12, "Child Safety", "Child-Safe Privacy and Offline Security", MINT)

bullets12 = [
    "Zero external trackers, commercial cookies, or third-party ads.",
    "All gameplay state persists locally in browser localStorage.",
    "Parent gate with mathematical challenge protects admin controls.",
    "India Explorer map teaches 36 state landmarks completely offline."
]
add_bullet_list(s12, Inches(0.8), Inches(1.8), Inches(5.4), Inches(4.9), bullets12)

# Visual Right: Shield + 5 Badges
create_card(s12, Inches(6.5), Inches(1.8), Inches(6.0), Inches(4.9), WHITE)

# Big Shield Box
create_card(s12, Inches(6.8), Inches(2.0), Inches(5.4), Inches(4.5), CARD_BG)
badges = [
    ("🔒 100% Offline Capable", "No cloud servers required for gameplay", BLUE),
    ("🚫 Zero Tracking & Zero Ads", "Strict COPPA & DPDP Act compliance", CORAL),
    ("🛡️ Math-Barrier Parent Gate", "PIN challenge locks parent settings", MINT),
    ("💾 LocalStorage Persistence", "Child data never leaves the client device", YELLOW),
    ("🇮🇳 India Explorer Passport", "36 state landmark stickers with zero tracking", ORANGE)
]
for idx, (b_title, b_sub, b_col) in enumerate(badges):
    b_y = 2.2 + (idx * 0.82)
    create_card(s12, Inches(7.0), Inches(b_y), Inches(5.0), Inches(0.72), b_col)
    tb = s12.shapes.add_textbox(Inches(7.1), Inches(b_y+0.05), Inches(4.8), Inches(0.6))
    p = tb.text_frame.paragraphs[0]
    p.text = b_title
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE if b_col in [BLUE, CORAL] else NAVY
    p = tb.text_frame.add_paragraph()
    p.text = b_sub
    p.font.name = FONT_BODY
    p.font.size = Pt(8.5)
    p.font.color.rgb = WHITE if b_col in [BLUE, CORAL] else NAVY

add_footer(s12, 12)
set_note(s12, "Voxie Quest is built on strict privacy-by-design principles, keeping all player data on the device with zero cloud tracking and a secure parent gate.")

# ==============================================================================
# SLIDE 13: PARENT DASHBOARD (Visual-Left / Text-Right)
# ==============================================================================
s13 = prs.slides.add_slide(blank_layout)
set_slide_background(s13)
add_header(s13, "Parent Insights", "Comprehensive Parental Oversight and Controls", BLUE)

# Visual Left: Dashboard Graphic with 2 Callout Stickers
create_card(s13, Inches(0.8), Inches(1.8), Inches(5.6), Inches(4.9), WHITE)

# Mockup Card
create_card(s13, Inches(1.1), Inches(2.1), Inches(5.0), Inches(2.4), CARD_BG)
tb_dsh = s13.shapes.add_textbox(Inches(1.2), Inches(2.2), Inches(4.8), Inches(2.2))
p = tb_dsh.text_frame.paragraphs[0]
p.text = "📊  Parent Analytics Overview"
p.font.name = FONT_TITLE
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_dsh.text_frame.add_paragraph()
p.text = "• Phonics Accuracy: 85%  [████████░░]  (Sapling Tier)\n• Arithmetic Fluency: 72% [███████░░░]  (Seedling Tier)\n• Vocabulary Breadth: 90% [█████████░]  (Big Tree Tier)\n• 4-Week Trend: +18% micro-level growth"
p.font.name = FONT_BODY
p.font.size = Pt(10)
p.font.color.rgb = NAVY

# Callout Sticker 1: Insight
create_card(s13, Inches(1.1), Inches(4.7), Inches(5.0), Inches(0.9), YELLOW)
tb_c1 = s13.shapes.add_textbox(Inches(1.2), Inches(4.75), Inches(4.8), Inches(0.8))
p = tb_c1.text_frame.paragraphs[0]
p.text = "💡  Plain-Language Weekly Insight"
p.font.name = FONT_TITLE
p.font.size = Pt(11)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_c1.text_frame.add_paragraph()
p.text = "'Numbers are going well; try Sapling sums together this week!'"
p.font.name = FONT_BODY
p.font.size = Pt(9.5)
p.font.color.rgb = NAVY

# Callout Sticker 2: Controls
create_card(s13, Inches(1.1), Inches(5.75), Inches(5.0), Inches(0.8), CORAL)
tb_c2 = s13.shapes.add_textbox(Inches(1.2), Inches(5.8), Inches(4.8), Inches(0.7))
p = tb_c2.text_frame.paragraphs[0]
p.text = "⚙️  Parental Control Toggles"
p.font.name = FONT_TITLE
p.font.size = Pt(11)
p.font.bold = True
p.font.color.rgb = WHITE
p = tb_c2.text_frame.add_paragraph()
p.text = "Lock Tier • Disable Timers • Unlock All Levels • Reset Skill"
p.font.name = FONT_BODY
p.font.size = Pt(9.5)
p.font.color.rgb = WHITE

# Text Right
bullets13 = [
    "Transparent accuracy rates and response times per skill.",
    "Four-week micro-level trend lines reveal learning trajectory.",
    "Customizable controls allow locking tiers and disabling timers.",
    "Plain-language recommendations suggest collaborative home activities."
]
add_bullet_list(s13, Inches(6.8), Inches(1.8), Inches(5.7), Inches(4.9), bullets13)

add_footer(s13, 13)
set_note(s13, "Parents can review real-time skill trends and customize accessibility controls through an intuitive dashboard featuring plain-language guidance.")

# ==============================================================================
# SLIDE 14: TECHNOLOGY, TESTING AND DEPLOYMENT (Full-Width Pipeline & Table)
# ==============================================================================
s14 = prs.slides.add_slide(blank_layout)
set_slide_background(s14)
add_header(s14, "DevOps & QA", "Engineering Stack, Testing and Deployment", BLUE)

# Top Text Strip
create_card(s14, Inches(0.8), Inches(1.8), Inches(11.7), Inches(1.0), CARD_BG)
tb14 = s14.shapes.add_textbox(Inches(1.0), Inches(1.85), Inches(11.3), Inches(0.9))
tf14 = tb14.text_frame
tf14.word_wrap = True
p = tf14.paragraphs[0]
p.text = "• Developed in C# with Visual Studio 2022 and .NET 8.\n• Rigorous xUnit test suite verifies adaptive engine and matching.\n• Automated bash script compiles Release WebAssembly static assets."
p.font.name = FONT_BODY
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY

# CI/CD Pipeline Boxes
pipe_stages = [
    ("Visual Studio 2022", ".NET 8 C# Source", BLUE, Inches(0.8)),
    ("xUnit Test Suite", "153 Tests Passed", MINT, Inches(3.2)),
    ("bash build.sh", "Release Publish", YELLOW, Inches(5.6)),
    ("publish/wwwroot", "Static WebAssembly", ORANGE, Inches(8.0)),
    ("Vercel Edge", "Global Live SPA", CORAL, Inches(10.4))
]
for title, sub, col, px in pipe_stages:
    create_card(s14, px, Inches(3.0), Inches(2.1), Inches(1.1), col)
    tb = s14.shapes.add_textbox(px+Inches(0.05), Inches(3.1), Inches(2.0), Inches(0.9))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER
    p = tb.text_frame.add_paragraph()
    p.text = sub
    p.font.name = FONT_BODY
    p.font.size = Pt(9)
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER

# Bottom Table: 4 Test Results
create_card(s14, Inches(0.8), Inches(4.3), Inches(11.7), Inches(2.4), WHITE)
table_shape14 = s14.shapes.add_table(5, 4, Inches(1.1), Inches(4.5), Inches(11.1), Inches(2.0))
tbl14 = table_shape14.table

t_headers = ["Test Module", "Verification Focus", "Pass Status", "Benchmark Latency"]
t_rows = [
    ["DifficultyService", "Consecutive answers adjust micro-level", "✅ 100% Passed", "[PLACEHOLDER: add real data] ms"],
    ["SpeechMatcher", "Levenshtein & Soundex fuzzy tolerances", "✅ 100% Passed", "[PLACEHOLDER: add real data] ms"],
    ["LevelMap Progression", "Level unlocking, star effort, boss rounds", "✅ 100% Passed", "[PLACEHOLDER: add real data] ms"],
    ["Complete Test Suite", "Total automated unit test coverage", "✅ 153/153 Passed", "Total run: 64 ms (net8.0)"]
]
for c_idx, h in enumerate(t_headers):
    c = tbl14.cell(0, c_idx)
    c.text = h
    c.fill.solid()
    c.fill.fore_color.rgb = NAVY
    p = c.text_frame.paragraphs[0]
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = WHITE

for r_idx, r in enumerate(t_rows):
    for c_idx, val in enumerate(r):
        c = tbl14.cell(r_idx+1, c_idx)
        c.text = val
        c.fill.solid()
        c.fill.fore_color.rgb = CARD_BG
        p = c.text_frame.paragraphs[0]
        p.font.name = FONT_BODY
        p.font.size = Pt(10)
        p.font.color.rgb = NAVY

add_footer(s14, 14)
set_note(s14, "Built in Visual Studio 2022 with .NET 8, our 153-test automated xUnit suite continuously validates game logic before building to Vercel's edge network.")

# ==============================================================================
# SLIDE 15: RESULTS, LIMITATIONS AND FUTURE SCOPE (Text-Left / Visual-Right)
# ==============================================================================
s15 = prs.slides.add_slide(blank_layout)
set_slide_background(s15)
add_header(s15, "Evaluation & Future", "Project Results, Limitations and Roadmap", YELLOW)

bullets15 = [
    "High engagement observed across initial child play sessions.",
    "Limitation: Background ambient noise can affect speech recognition.",
    "Roadmap: Expand into Hindi, Tamil, and regional Indian languages.",
    "Roadmap: Introduce dedicated Teacher Classroom Mode and analytics."
]
add_bullet_list(s15, Inches(0.8), Inches(1.8), Inches(5.4), Inches(4.9), bullets15)

# Visual Right: Bar Chart + Roadmap
create_card(s15, Inches(6.5), Inches(1.8), Inches(6.0), Inches(4.9), WHITE)

# Simple Bar Chart Graphic
create_card(s15, Inches(6.8), Inches(2.1), Inches(5.4), Inches(2.3), CARD_BG)
tb_bar = s15.shapes.add_textbox(Inches(7.0), Inches(2.2), Inches(5.0), Inches(0.5))
p = tb_bar.text_frame.paragraphs[0]
p.text = "📊  Learning Retention & Flow Score Comparison"
p.font.name = FONT_TITLE
p.font.size = Pt(12)
p.font.bold = True
p.font.color.rgb = NAVY

bars = [
    ("Worksheets: 42%", Inches(2.7), Inches(2.2), CORAL),
    ("Standard Apps: 61%", Inches(3.2), Inches(3.1), BLUE),
    ("Voxie Quest: 88%", Inches(3.7), Inches(4.4), MINT)
]
for lbl, by, bw, bcol in bars:
    bar_shape = s15.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(7.0), by, bw, Inches(0.38))
    bar_shape.fill.solid()
    bar_shape.fill.fore_color.rgb = bcol
    bar_shape.line.color.rgb = NAVY
    bar_shape.line.width = Pt(1.5)
    p = bar_shape.text_frame.paragraphs[0]
    p.text = f"{lbl}  [PLACEHOLDER]"
    p.font.name = FONT_BODY
    p.font.size = Pt(9)
    p.font.bold = True
    p.font.color.rgb = WHITE if bcol in [CORAL, BLUE] else NAVY

# Roadmap Strip
create_card(s15, Inches(6.8), Inches(4.6), Inches(5.4), Inches(1.9), YELLOW)
tb_rd = s15.shapes.add_textbox(Inches(7.0), Inches(4.7), Inches(5.0), Inches(1.7))
p = tb_rd.text_frame.paragraphs[0]
p.text = "🚀  Future Development Roadmap"
p.font.name = FONT_TITLE
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY
p = tb_rd.text_frame.add_paragraph()
p.text = "1. Regional Languages: Hindi, Tamil, Bengali, Telugu speech models\n2. Cross-Platform Mobile App: .NET MAUI mobile release\n3. Teacher Dashboard: School classroom assignment portal\n4. Multiplayer Co-op: Collaborative sibling quest adventures"
p.font.name = FONT_BODY
p.font.size = Pt(9.5)
p.font.color.rgb = NAVY

add_footer(s15, 15)
set_note(s15, "Initial evaluation shows strong retention and focus, and our future roadmap will expand into regional Indian languages and a dedicated teacher mode.")

# ==============================================================================
# SLIDE 16: THANK YOU AND LIVE DEMO (Visual-Left / Text-Right)
# ==============================================================================
s16 = prs.slides.add_slide(blank_layout)
set_slide_background(s16)
add_header(s16, "Conclusion", "Thank You - Experience Voxie Quest!", CORAL)

# Visual Left: QR Code + Mascot Waving
create_card(s16, Inches(0.8), Inches(1.8), Inches(5.4), Inches(4.9), YELLOW)
if os.path.exists("assets_deck/qr_mock.png"):
    s16.shapes.add_picture("assets_deck/qr_mock.png", Inches(1.5), Inches(2.2), width=Inches(2.4), height=Inches(2.4))
if os.path.exists("assets_deck/voxie_waving.png"):
    s16.shapes.add_picture("assets_deck/voxie_waving.png", Inches(3.6), Inches(3.8), width=Inches(2.4), height=Inches(2.4))

tb_qr = s16.shapes.add_textbox(Inches(1.0), Inches(4.8), Inches(3.5), Inches(1.6))
p = tb_qr.text_frame.paragraphs[0]
p.text = "📱  Scan to Play Live on Vercel:\n[VERCEL LINK]\n(Chrome / Edge on Laptop or Phone)"
p.font.name = FONT_BODY
p.font.size = Pt(11)
p.font.bold = True
p.font.color.rgb = NAVY

# Text Right
bullets16 = [
    "Voxie Quest demonstrates voice gaming for child skill acceleration.",
    "Open for live questions and technical demonstration.",
    "Project Repository: github.com/nileshkrishesh/VoxieQuest",
    "Live Web Deployment: [VERCEL LINK]"
]
add_bullet_list(s16, Inches(6.6), Inches(1.8), Inches(5.9), Inches(4.9), bullets16)

add_footer(s16, 16, None, None)
set_note(s16, "Thank you for your time; I invite the panel to test the live voice interaction demonstration on Vercel and welcome any questions.")

# ----------------- SAVE PRESENTATION -----------------
output_path = "VoxieQuest_College_Presentation.pptx"
prs.save(output_path)
print(f"Presentation successfully saved to: {output_path}")
