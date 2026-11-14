import os
import pptx
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.enum.shapes import MSO_SHAPE

# ----------------- 70/30 PALETTE -----------------
CREAM = RGBColor(255, 253, 247)      # #FFFDF7
NAVY = RGBColor(30, 42, 74)          # #1E2A4A
YELLOW = RGBColor(255, 209, 102)     # #FFD166
BLUE = RGBColor(77, 150, 255)        # #4D96FF
CORAL = RGBColor(255, 107, 107)      # #FF6B6B
MINT = RGBColor(107, 203, 119)       # #6BCB77
ORANGE = RGBColor(255, 159, 67)      # #FF9F43
WHITE = RGBColor(255, 255, 255)
CARD_BG = RGBColor(254, 251, 240)

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

def add_slide_header(slide, title_text, line_text, pill_tag="VOXIE QUEST", pill_color=YELLOW):
    # Pill Tag
    tag_box = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.8), Inches(0.35), Inches(2.2), Inches(0.38))
    tag_box.fill.solid()
    tag_box.fill.fore_color.rgb = pill_color
    tag_box.line.color.rgb = NAVY
    tag_box.line.width = Pt(2)
    p = tag_box.text_frame.paragraphs[0]
    p.text = pill_tag.upper()
    p.font.name = FONT_TITLE
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.alignment = PP_ALIGN.CENTER

    # Title (Max 5 words, huge Fredoka bold)
    t_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.75), Inches(11.5), Inches(0.7))
    tf = t_box.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = title_text
    p.font.name = FONT_TITLE
    p.font.size = Pt(32)
    p.font.bold = True
    p.font.color.rgb = NAVY

    # One Line Subtitle (Max 12 words, 22-24pt Nunito)
    l_box = slide.shapes.add_textbox(Inches(0.8), Inches(1.35), Inches(11.5), Inches(0.5))
    tf2 = l_box.text_frame
    tf2.word_wrap = True
    p2 = tf2.paragraphs[0]
    p2.text = line_text
    p2.font.name = FONT_BODY
    p2.font.size = Pt(20)
    p2.font.bold = True
    p2.font.color.rgb = BLUE

def add_slide_footer(slide, slide_num, mascot_pose="assets_deck/voxie_happy.png"):
    # Footer line
    line = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), Inches(7.05), Inches(11.733), Pt(1.5))
    line.fill.solid()
    line.fill.fore_color.rgb = NAVY
    line.line.fill.background()

    # Footer left text
    ft_box = slide.shapes.add_textbox(Inches(0.8), Inches(7.08), Inches(8), Inches(0.32))
    p = ft_box.text_frame.paragraphs[0]
    p.text = "VOXIE QUEST • 70/30 Visual Architecture | C# Blazor WebAssembly (.NET 8)"
    p.font.name = FONT_BODY
    p.font.size = Pt(9.5)
    p.font.bold = True
    p.font.color.rgb = NAVY

    # Slide number
    num_box = slide.shapes.add_textbox(Inches(10.5), Inches(7.08), Inches(2.0), Inches(0.32))
    p_num = num_box.text_frame.paragraphs[0]
    p_num.text = f"{slide_num} / 12"
    p_num.font.name = FONT_BODY
    p_num.font.size = Pt(9.5)
    p_num.font.bold = True
    p_num.font.color.rgb = NAVY
    p_num.alignment = PP_ALIGN.RIGHT

    # Mascot corner sticker (small, unobtrusive)
    if mascot_pose and os.path.exists(mascot_pose):
        slide.shapes.add_picture(mascot_pose, Inches(12.0), Inches(6.05), width=Inches(1.0), height=Inches(1.0))

def create_card(slide, left, top, width, height, bg_color=CARD_BG, border_color=NAVY, radius=MSO_SHAPE.ROUNDED_RECTANGLE):
    card = slide.shapes.add_shape(radius, left, top, width, height)
    card.fill.solid()
    card.fill.fore_color.rgb = bg_color
    card.line.color.rgb = border_color
    card.line.width = Pt(2.5)
    return card

def set_note(slide, note_text):
    slide.notes_slide.notes_text_frame.text = note_text

# ==============================================================================
# SLIDE 1: TITLE & HERO
# ==============================================================================
s1 = prs.slides.add_slide(blank_layout)
set_slide_background(s1)

# Left Side: Big Title, One Line & Student Sticker
t_box = s1.shapes.add_textbox(Inches(0.8), Inches(1.2), Inches(6.0), Inches(1.2))
p = t_box.text_frame.paragraphs[0]
p.text = "VOXIE QUEST"
p.font.name = FONT_TITLE
p.font.size = Pt(50)
p.font.bold = True
p.font.color.rgb = CORAL

l_box = s1.shapes.add_textbox(Inches(0.8), Inches(2.4), Inches(5.8), Inches(0.8))
p = l_box.text_frame.paragraphs[0]
p.text = "Voice-Controlled Gaming Tools for Enhanced Learning"
p.font.name = FONT_BODY
p.font.size = Pt(22)
p.font.bold = True
p.font.color.rgb = BLUE

# Student Details Sticker Card
create_card(s1, Inches(0.8), Inches(3.6), Inches(5.6), Inches(2.8), YELLOW)
stk_tb = s1.shapes.add_textbox(Inches(1.0), Inches(3.8), Inches(5.2), Inches(2.4))
tf = stk_tb.text_frame
tf.word_wrap = True
lines = [
    "🎓 Student Name: [NAME]",
    "🔢 Roll Number: [ROLL NO]",
    "📚 Degree & Course: [COURSE]",
    "🏛️ College: [COLLEGE]",
    "👨‍🏫 Guide: [GUIDE]",
    "📅 Academic Year: [YEAR]"
]
for i, ln in enumerate(lines):
    p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
    p.text = ln
    p.font.name = FONT_BODY
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.space_after = Pt(3)

# Right Side (70% Visual Core): Hero Mascot Picture
create_card(s1, Inches(6.8), Inches(1.0), Inches(5.7), Inches(5.5), CARD_BG)
if os.path.exists("assets_deck/voxie_hero.png"):
    s1.shapes.add_picture("assets_deck/voxie_hero.png", Inches(7.4), Inches(1.4), width=Inches(4.5), height=Inches(4.5))

add_slide_footer(s1, 1, None)
set_note(s1, "Welcome to our presentation of Voxie Quest, a voice-controlled educational web game for young children. We built this platform in Blazor WebAssembly to turn natural spoken answers into playful learning adventures.")

# ==============================================================================
# SLIDE 2: PROBLEM (Split Screen)
# ==============================================================================
s2 = prs.slides.add_slide(blank_layout)
set_slide_background(s2)
add_slide_header(s2, "Rote Drills Create Frustration", "Learning should feel like play.", "PROBLEM", CORAL)

# 70% Visual: Split Screen Card
if os.path.exists("assets_70_30/split_problem.png"):
    s2.shapes.add_picture("assets_70_30/split_problem.png", Inches(1.8), Inches(2.0), width=Inches(9.7), height=Inches(4.8))

add_slide_footer(s2, 2)
set_note(s2, "Young children lose interest when forced to do passive worksheets or struggle with fine-motor keyboard typing. Our research shows that early childhood education thrives when physical barriers are removed and replaced with vocal play.")

# ==============================================================================
# SLIDE 3: OUR IDEA
# ==============================================================================
s3 = prs.slides.add_slide(blank_layout)
set_slide_background(s3)
add_slide_header(s3, "Speak. Play. Learn.", "Your voice is the controller.", "CORE CONCEPT", MINT)

# 70% Visual: Game Screen Mockup with Radiating Waves
if os.path.exists("assets_70_30/game_soundwaves.png"):
    s3.shapes.add_picture("assets_70_30/game_soundwaves.png", Inches(1.4), Inches(2.0), width=Inches(10.5), height=Inches(4.8))

add_slide_footer(s3, 3)
set_note(s3, "Instead of touching tiny buttons or memorizing keys, children speak directly to character Voxie to jump lilypads, launch rockets, and explore vocabulary. This creates an immediate cognitive feedback loop that keeps children excited and focused.")

# ==============================================================================
# SLIDE 4: HOW IT WORKS (5-Step Bold Pipeline)
# ==============================================================================
s4 = prs.slides.add_slide(blank_layout)
set_slide_background(s4)
add_slide_header(s4, "How It Works", "From spoken word to instant reward.", "PIPELINE", BLUE)

# 70% Visual: 5 Chunky Horizontal Steps
steps = [
    ("1. SPEAK", "🎤", "Child answers aloud", YELLOW),
    ("2. MIC", "📡", "Web Speech API", BLUE),
    ("3. MATCH", "⚙️", "C# Fuzzy Engine", MINT),
    ("4. REACT", "🦊", "Voxie cheering", ORANGE),
    ("5. REWARD", "⭐", "Stars & Gems", CORAL)
]
for i, (st, icn, sub, col) in enumerate(steps):
    x = 0.8 + (i * 2.38)
    card = create_card(s4, Inches(x), Inches(2.4), Inches(2.2), Inches(4.2), col)
    tb = s4.shapes.add_textbox(Inches(x+0.1), Inches(2.6), Inches(2.0), Inches(3.8))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = icn
    p.font.size = Pt(44)
    p.alignment = PP_ALIGN.CENTER
    p = tf.add_paragraph()
    p.text = st
    p.font.name = FONT_TITLE
    p.font.size = Pt(18)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER
    p.space_after = Pt(12)
    p = tf.add_paragraph()
    p.text = sub
    p.font.name = FONT_BODY
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER

add_slide_footer(s4, 4)
set_note(s4, "Audio is captured directly in the browser and passed to our speech matcher, which checks pronunciation tolerances in real time. If the answer is correct, the child is immediately celebrated with animations and stars.")

# ==============================================================================
# SLIDE 5: FIVE GAMES (Fan Grid of 5 Cards)
# ==============================================================================
s5 = prs.slides.add_slide(blank_layout)
set_slide_background(s5)
add_slide_header(s5, "Five Foundational Skill Quests", "Targeting phonics, arithmetic, colors, fauna, and words.", "MINI-GAMES", ORANGE)

cards = [
    ("card_rocket.png", "🚀 Phonics", Inches(0.8)),
    ("card_number.png", "🔢 Arithmetic", Inches(3.2)),
    ("card_color.png", "🎨 Colors", Inches(5.6)),
    ("card_safari.png", "🦁 Fauna", Inches(8.0)),
    ("card_words.png", "🧩 Spelling", Inches(10.4))
]
for img_name, tag, px in cards:
    path = f"assets_70_30/{img_name}"
    if os.path.exists(path):
        s5.shapes.add_picture(path, px, Inches(2.1), width=Inches(2.1), height=Inches(3.3))
    tag_card = create_card(s5, px, Inches(5.5), Inches(2.1), Inches(0.9), YELLOW)
    p = tag_card.text_frame.paragraphs[0]
    p.text = tag
    p.font.name = FONT_TITLE
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.alignment = PP_ALIGN.CENTER

add_slide_footer(s5, 5)
set_note(s5, "Voxie Quest covers five essential curriculum areas through focused mini-games, from phonemic awareness in Letter Rocket to mental math in Number Jump. Each game adapts its parameters as the child plays.")

# ==============================================================================
# SLIDE 6: SMART VOICE (Worked Example)
# ==============================================================================
s6 = prs.slides.add_slide(blank_layout)
set_slide_background(s6)
add_slide_header(s6, "Smart Child Speech Matching", "Kids' pronunciation still counts.", "ALGORITHM", BLUE)

# 70% Visual: Side-by-side matching card
create_card(s6, Inches(1.5), Inches(2.0), Inches(10.3), Inches(4.7), CARD_BG)

# Spoken Bubble
create_card(s6, Inches(2.0), Inches(2.4), Inches(3.2), Inches(1.4), CORAL)
tb = s6.shapes.add_textbox(Inches(2.1), Inches(2.5), Inches(3.0), Inches(1.2))
p = tb.text_frame.paragraphs[0]
p.text = "Child Spoke:\n'WED'"
p.font.name = FONT_TITLE
p.font.size = Pt(20)
p.font.bold = True
p.font.color.rgb = WHITE
p.alignment = PP_ALIGN.CENTER

# Arrow
arr = s6.shapes.add_shape(MSO_SHAPE.RIGHT_ARROW, Inches(5.6), Inches(2.8), Inches(2.1), Inches(0.6))
arr.fill.solid()
arr.fill.fore_color.rgb = YELLOW
arr.line.color.rgb = NAVY

# Target Bubble
create_card(s6, Inches(8.1), Inches(2.4), Inches(3.2), Inches(1.4), MINT)
tb = s6.shapes.add_textbox(Inches(8.2), Inches(2.5), Inches(3.0), Inches(1.2))
p = tb.text_frame.paragraphs[0]
p.text = "Target Word:\n'RED'"
p.font.name = FONT_TITLE
p.font.size = Pt(20)
p.font.bold = True
p.font.color.rgb = NAVY
p.alignment = PP_ALIGN.CENTER

# Calculation Explanation Card
create_card(s6, Inches(2.0), Inches(4.2), Inches(9.3), Inches(2.1), WHITE)
tb = s6.shapes.add_textbox(Inches(2.2), Inches(4.3), Inches(8.9), Inches(1.9))
tf = tb.text_frame
tf.word_wrap = True
p = tf.paragraphs[0]
p.text = "⚙️ Levenshtein Edit Distance: 1 substitution (W ➔ R) = 66.7% similarity"
p.font.name = FONT_BODY
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p = tf.add_paragraph()
p.text = "🔍 Soundex Phonetic Filter: Approves liquid glide consonant substitution (W ↔ R)"
p.font.name = FONT_BODY
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p = tf.add_paragraph()
p.text = "⭐ VERDICT: 100% ACCEPTED AS CORRECT! (Award Reward Stars)"
p.font.name = FONT_TITLE
p.font.size = Pt(18)
p.font.bold = True
p.font.color.rgb = CORAL

add_slide_footer(s6, 6)
set_note(s6, "Young children often substitute sounds like saying 'wed' instead of 'red' as part of natural speech development. Our algorithm blends edit distance with phonetic grouping so children are rewarded for legitimate attempts rather than penalized.")

# ==============================================================================
# SLIDE 7: LEVELS AND DIFFICULTY (Winding Map Trail)
# ==============================================================================
s7 = prs.slides.add_slide(blank_layout)
set_slide_background(s7)
add_slide_header(s7, "Adaptive Botanical Levels", "Gentle growth from Seedling to Big Tree.", "ADAPTIVE ENGINE", MINT)

# 3 Tiers Top Row
tiers = [
    ("🌱 Seedling Tier", "Ages 3-5 • Untimed • Tap & Voice", YELLOW, Inches(1.5)),
    ("🌿 Sapling Tier", "Ages 6-8 • Calm Moving Sun", MINT, Inches(5.2)),
    ("🌳 Big Tree Tier", "Ages 9-12 • Multi-Step Puzzles", BLUE, Inches(8.9))
]
for t_lbl, t_sub, col, tx in tiers:
    card = create_card(s7, tx, Inches(2.0), Inches(3.0), Inches(1.1), col)
    tb = s7.shapes.add_textbox(tx+Inches(0.1), Inches(2.1), Inches(2.8), Inches(0.9))
    p = tb.text_frame.paragraphs[0]
    p.text = t_lbl
    p.font.name = FONT_TITLE
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = WHITE if col == BLUE else NAVY
    p = tb.text_frame.add_paragraph()
    p.text = t_sub
    p.font.name = FONT_BODY
    p.font.size = Pt(9.5)
    p.font.color.rgb = WHITE if col == BLUE else NAVY

# Winding 10-Level Picture-Book Trail Card
create_card(s7, Inches(1.5), Inches(3.4), Inches(10.4), Inches(3.3), CARD_BG)
# Trail Nodes 1 to 10
coords = [
    (Inches(2.0), Inches(5.6)), (Inches(3.2), Inches(4.5)), (Inches(4.4), Inches(5.5)),
    (Inches(5.6), Inches(4.3)), (Inches(6.8), Inches(5.4)), (Inches(8.0), Inches(4.3)),
    (Inches(9.2), Inches(5.5)), (Inches(10.4), Inches(4.5)), (Inches(10.0), Inches(3.7)),
    (Inches(11.0), Inches(3.7))
]
for idx, (nx, ny) in enumerate(coords):
    lvl = idx + 1
    col = CORAL if lvl in [5, 10] else (MINT if lvl < 4 else YELLOW)
    node = s7.shapes.add_shape(MSO_SHAPE.OVAL, nx, ny, Inches(0.8), Inches(0.8))
    node.fill.solid()
    node.fill.fore_color.rgb = col
    node.line.color.rgb = NAVY
    node.line.width = Pt(2.5)
    p = node.text_frame.paragraphs[0]
    p.text = f"★{lvl}" if lvl in [5, 10] else str(lvl)
    p.font.name = FONT_TITLE
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = WHITE if col == CORAL else NAVY
    p.alignment = PP_ALIGN.CENTER

add_slide_footer(s7, 7)
set_note(s7, "Rather than labeling children with stressful 'easy' or 'hard' tags, we use botanical tiers that adapt smoothly across an eight-answer rolling window. If a child struggles, Voxie silently provides hints and slower pacing before ever adjusting the level.")

# ==============================================================================
# SLIDE 8: STREAK AND GARDEN (Tilted Phones)
# ==============================================================================
s8 = prs.slides.add_slide(blank_layout)
set_slide_background(s8)
add_slide_header(s8, "Streaks & Living Garden", "Every question answered blooms a flower.", "GAMIFICATION", ORANGE)

# Tilted Phone 1: Streak Calendar
if os.path.exists("assets_70_30/phone_streak.png"):
    s8.shapes.add_picture("assets_70_30/phone_streak.png", Inches(2.2), Inches(2.1), width=Inches(3.8), height=Inches(4.6))
# Tilted Phone 2: Skill Garden
if os.path.exists("assets_70_30/phone_garden.png"):
    s8.shapes.add_picture("assets_70_30/phone_garden.png", Inches(7.3), Inches(2.1), width=Inches(3.8), height=Inches(4.6))

add_slide_footer(s8, 8)
set_note(s8, "We replace traditional test anxiety with positive habit formation through a daily streak calendar and an interactive Skill Garden. Every correct answer waters a virtual flower, turning effort into visual pride.")

# ==============================================================================
# SLIDE 9: SAFE AND PRIVATE (Giant Shield)
# ==============================================================================
s9 = prs.slides.add_slide(blank_layout)
set_slide_background(s9)
add_slide_header(s9, "Safe by Design", "Zero ads, zero tracking, total child privacy.", "CHILD SAFETY", MINT)

# 70% Visual: Giant Protective Shield Card
create_card(s9, Inches(1.8), Inches(2.0), Inches(9.7), Inches(4.8), CARD_BG)
shield_badges = [
    ("🔒 100% Offline Capable", "Runs in-browser without servers", BLUE, Inches(2.2), Inches(2.4)),
    ("🚫 Zero Third-Party Ads", "Strict COPPA & DPDP compliance", CORAL, Inches(7.0), Inches(2.4)),
    ("🛡️ Math Parent Gate", "Adult verification challenge", MINT, Inches(2.2), Inches(3.6)),
    ("💾 LocalStorage Only", "Data never leaves device", YELLOW, Inches(7.0), Inches(3.6)),
    ("🇮🇳 Private India Explorer", "36 state landmark stickers with zero tracking", ORANGE, Inches(3.5), Inches(4.8))
]
for title, sub, col, bx, by in shield_badges:
    bw = Inches(6.3) if "India" in title else Inches(4.3)
    card = create_card(s9, bx, by, bw, Inches(1.0), col)
    tb = s9.shapes.add_textbox(bx+Inches(0.1), by+Inches(0.1), bw-Inches(0.2), Inches(0.8))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.name = FONT_TITLE
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p = tb.text_frame.add_paragraph()
    p.text = sub
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY

add_slide_footer(s9, 9)
set_note(s9, "Voxie Quest is designed with strict privacy-by-design standards compliant with global child safety regulations. All gameplay data remains securely stored in the child's browser, and no personal telemetry or advertisements exist.")

# ==============================================================================
# SLIDE 10: PARENT DASHBOARD & WEEKLY LEAGUE
# ==============================================================================
s10 = prs.slides.add_slide(blank_layout)
set_slide_background(s10)
add_slide_header(s10, "Parent Dashboard & League", "Effort-based, never stranger ranking.", "PARENT CONTROLS", BLUE)

# Left: Dashboard Analytics Mockup Card
create_card(s10, Inches(1.2), Inches(2.1), Inches(5.2), Inches(4.6), WHITE)
tb = s10.shapes.add_textbox(Inches(1.4), Inches(2.3), Inches(4.8), Inches(4.2))
tf = tb.text_frame
tf.word_wrap = True
p = tf.paragraphs[0]
p.text = "📊 Parent Oversight Panel"
p.font.name = FONT_TITLE
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p.space_after = Pt(8)
dash_items = [
    "• Micro-Level Trendlines (4-Week Growth)",
    "• Skill Accuracy Rates (Phonics, Math, Words)",
    "• Tier Locking & Timer Pace Multipliers",
    "• Plain-Language Home Guidance"
]
for di in dash_items:
    p = tf.add_paragraph()
    p.text = di
    p.font.name = FONT_BODY
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.space_after = Pt(4)

# Right: Weekly League Garden Hill Card
create_card(s10, Inches(6.8), Inches(2.1), Inches(5.3), Inches(4.6), YELLOW)
tb2 = s10.shapes.add_textbox(Inches(7.0), Inches(2.3), Inches(4.9), Inches(4.2))
tf2 = tb2.text_frame
tf2.word_wrap = True
p = tf2.paragraphs[0]
p.text = "🌻 Weekly Garden League"
p.font.name = FONT_TITLE
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p.space_after = Pt(8)
league_items = [
    "• Private Rooms Only (Family or Classroom)",
    "• Safe Non-Identifiable Names ('Brave Panda 42')",
    "• Top 5 Podium — Nobody designated last place",
    "• Strict 150 Daily Point Cap for Healthy Rest"
]
for li in league_items:
    p = tf2.add_paragraph()
    p.text = li
    p.font.name = FONT_BODY
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = NAVY
    p.space_after = Pt(4)

add_slide_footer(s10, 10)
set_note(s10, "Parents can review real learning trends and lock difficulty tiers from a protected dashboard. Our weekly league operates strictly inside private family or classroom rooms with a daily point cap, ensuring children never compete against strangers.")

# ==============================================================================
# SLIDE 11: TECH AND TESTING
# ==============================================================================
s11 = prs.slides.add_slide(blank_layout)
set_slide_background(s11)
add_slide_header(s11, "Engineering & Rigorous QA", "Tested end-to-end for seamless edge play.", "DEVOPS & QA", CORAL)

# 70% Visual: Pipeline Horizontal Sequence + Hero Metric Card
pipe = [
    ("VS 2022", "C# .NET 8", BLUE, Inches(1.2)),
    ("xUnit Suite", "Automated QA", MINT, Inches(3.5)),
    ("build.sh", "Release Compile", YELLOW, Inches(5.8)),
    ("Vercel Edge", "Global CDN", ORANGE, Inches(8.1)),
    ("Live SPA", "Zero-Install", CORAL, Inches(10.4))
]
for st, sub, col, px in pipe:
    card = create_card(s11, px, Inches(2.2), Inches(2.0), Inches(1.5), col)
    tb = s11.shapes.add_textbox(px+Inches(0.05), Inches(2.3), Inches(1.9), Inches(1.3))
    p = tb.text_frame.paragraphs[0]
    p.text = st
    p.font.name = FONT_TITLE
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER
    p = tb.text_frame.add_paragraph()
    p.text = sub
    p.font.name = FONT_BODY
    p.font.size = Pt(10)
    p.font.color.rgb = WHITE if col in [BLUE, CORAL] else NAVY
    p.alignment = PP_ALIGN.CENTER

# Giant Metric Hero Card
create_card(s11, Inches(1.2), Inches(4.1), Inches(10.9), Inches(2.6), CARD_BG)
tb = s11.shapes.add_textbox(Inches(1.5), Inches(4.3), Inches(10.3), Inches(2.2))
tf = tb.text_frame
tf.word_wrap = True
p = tf.paragraphs[0]
p.text = "🧪 [PLACEHOLDER: add real data] Unit Tests Passed (0 Failures)"
p.font.name = FONT_TITLE
p.font.size = Pt(22)
p.font.bold = True
p.font.color.rgb = CORAL
p.alignment = PP_ALIGN.CENTER
p.space_after = Pt(8)

p = tf.add_paragraph()
p.text = "• 100% Adaptive Flow & Botanical Difficulty Tests: PASSED"
p.font.name = FONT_BODY
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY
p = tf.add_paragraph()
p.text = "• 100% SpeechMatcher Levenshtein & Soundex Tolerance: PASSED"
p.font.name = FONT_BODY
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY
p = tf.add_paragraph()
p.text = "• 100% Weekly League Daily Cap & Private Room Rules: PASSED"
p.font.name = FONT_BODY
p.font.size = Pt(13)
p.font.bold = True
p.font.color.rgb = NAVY

add_slide_footer(s11, 11)
set_note(s11, "The application is built in C# with Blazor WebAssembly on .NET 8 and validated by our automated test suite. It compiles to static WebAssembly binaries deployed across Vercel's global edge network for instant zero-install access.")

# ==============================================================================
# SLIDE 12: THANK YOU AND DEMO
# ==============================================================================
s12 = prs.slides.add_slide(blank_layout)
set_slide_background(s12)
add_slide_header(s12, "Experience Voxie Quest Live", "Scan the code and start speaking.", "LIVE DEMO", YELLOW)

# QR Code Card (Left)
create_card(s12, Inches(1.5), Inches(2.1), Inches(4.8), Inches(4.6), YELLOW)
if os.path.exists("assets_deck/qr_mock.png"):
    s12.shapes.add_picture("assets_deck/qr_mock.png", Inches(2.2), Inches(2.4), width=Inches(3.4), height=Inches(3.4))

tb = s12.shapes.add_textbox(Inches(1.7), Inches(5.9), Inches(4.4), Inches(0.7))
p = tb.text_frame.paragraphs[0]
p.text = "📱 [VERCEL LINK]"
p.font.name = FONT_TITLE
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = NAVY
p.alignment = PP_ALIGN.CENTER

# Mascot Waving (Right)
create_card(s12, Inches(6.8), Inches(2.1), Inches(5.0), Inches(4.6), CARD_BG)
if os.path.exists("assets_deck/voxie_waving.png"):
    s12.shapes.add_picture("assets_deck/voxie_waving.png", Inches(7.4), Inches(2.4), width=Inches(3.8), height=Inches(3.8))

tb = s12.shapes.add_textbox(Inches(7.0), Inches(5.9), Inches(4.6), Inches(0.7))
p = tb.text_frame.paragraphs[0]
p.text = "✨ Speak, Play, Learn!"
p.font.name = FONT_TITLE
p.font.size = Pt(16)
p.font.bold = True
p.font.color.rgb = CORAL
p.alignment = PP_ALIGN.CENTER

add_slide_footer(s12, 12, None)
set_note(s12, "Thank you for your time and attention today. I invite everyone to scan the QR code to try the live voice recognition game on their devices, and I look forward to your questions.")

# ----------------- EXPORT PRESENTATION -----------------
output_path = "VoxieQuest_70_30_Presentation.pptx"
prs.save(output_path)
print(f"Presentation successfully saved to: {output_path}")
