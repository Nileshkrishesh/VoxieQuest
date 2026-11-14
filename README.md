# 🦊 VOXIE QUEST — Speak, Play, Learn!

> **Voice-Controlled Educational Web Gaming Tools for Enhanced Learning in the Skill Ecosystem**  
> Designed for children aged 5–10 to explore phonics, math, colors, animals, and spelling with their natural voice.

[![.NET 8](https://img.shields.io/badge/.NET-8.0%20WASM-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly%20Standalone-512BD4?logo=blazor)](https://blazor.net/)
[![Tests](https://img.shields.io/badge/xUnit-113%20Passed-brightgreen)](https://xunit.net/)
[![Visual Studio](https://img.shields.io/badge/IDE-Visual%20Studio%202022-purple?logo=visualstudio)](https://visualstudio.microsoft.com/)
[![COPPA Child-Safe](https://img.shields.io/badge/Child--Safe-Zero%20Ads%20%7C%20Zero%20Tracking-06D6A0)](#child-safe-guarantee)

---

## 📖 Table of Contents
1. [Overview & Tech Stack](#overview--tech-stack)
2. [Game Realms & Features](#game-realms--features)
3. [Visual Studio 2022 Quickstart (Ctrl+F5)](#visual-studio-2022-quickstart-ctrlf5)
4. [Running Unit Tests in Test Explorer](#running-unit-tests-in-test-explorer)
5. [Publishing & Free Hosting (GitHub Pages & Netlify)](#publishing--free-hosting-github-pages--netlify)
6. [Curated Free Asset & Sound Resources](#curated-free-asset--sound-resources)
7. [Assumptions & Known Browser Limits](#assumptions--known-browser-limits)
8. [Child-Safe Guarantee](#child-safe-guarantee)

---

## 🛠️ Overview & Tech Stack

**Voxie Quest** is built with a pure **Blazor WebAssembly standalone (.NET 8)** architecture. All game state machines, scoring algorithms, phonological fuzzy matching, adaptive difficulty systems, and session timers run in pure C# compiled to WebAssembly.

- **C# for 100% of Game Logic:** No Unity, no Node.js build tools, no npm, and no third-party game engine dependencies.
- **Thin JavaScript Interop:** Confined strictly to browser native APIs (`SpeechRecognition`, Web Audio API `AudioContext` and `AnalyserNode`, and safe `localStorage`).
- **Storybook Aesthetics:** Hand-drawn children's picture book look with thick rounded outlines (`border: 3.5px solid #2B2D42`), warm pastel design tokens (Sunny Yellow `#FFD166`, Sky Blue `#4EA8DE`, Coral `#EF476F`, Mint `#06D6A0`, Soft Orange `#F78C6B`), paper texture, and local offline fonts (*Fredoka* and *Nunito*).
- **Phonological Tolerance Engine (`SpeechMatcher.cs`):** Pure C# fuzzy speech evaluator using Levenshtein distance, Soundex, Metaphone, carrier phrase stripping, and childhood phonology transforms (liquid gliding, velar fronting, frontal lisping, and fricative stopping).
- **Compassionate Feedback:** 3 attempts per question with hints revealed on the 2nd miss. **Never shows "Wrong" or "Incorrect"**, only *"Almost! Try again!"* and positive reinforcement.

---

## 🎮 Game Realms & Features

### 1. 🚀 Letter Rocket (`/game/letter-rocket`)
- **Category:** Letters & Phonics (`SkillCategory.Letters`)
- **Mechanic:** Say the letter or phoneme displayed in the rocket's porthole.
- **Animation:** Liquid rocket thrusters ignite with an animated SVG flame blast-off into space.

### 2. 🐸 Number Jump (`/game/number-jump`)
- **Category:** Arithmetic & Counting (`SkillCategory.Numbers`)
- **Mechanic:** Speak the sum to simple additions (e.g. `2 + 1 = 3`).
- **Animation:** An animated frog hops across buoyant, numbered floating lily pads.

### 3. 🎨 Color Splash (`/game/color-splash`)
- **Category:** Color Identification (`SkillCategory.Colors`)
- **Mechanic:** Speak the color name shown on the palette.
- **Animation:** A smiling paint bucket pours and splashes vibrant paint onto black-and-white picture outlines.

### 4. 🦁 Animal Safari (`/game/animal-safari`)
- **Category:** Auditory Animal Calls (`SkillCategory.Animals`)
- **Mechanic:** Hear an animal sound (procedurally synthesized or MP3), speak the animal name.
- **Animation:** Animal leaps out of rustling jungle foliage and hops into a caravan cart trailing Voxie's expedition jeep.

### 5. 🧱 Word Builder (`/game/word-builder`)
- **Category:** Spelling & Blends (`SkillCategory.Spelling`)
- **Mechanic:** View floating 3D toy letter blocks (e.g. `[C]` `[A]` `[T]`), speak the word.
- **Animation:** Blocks magnetically slide together (`blocks-snapping`) and transform into an illustrated picture card.

### 6. 🌸 Skill Garden (`/garden`)
- Features **5 visual garden plants** (Letters Sunflower, Numbers Tulip, Rainbow Rose, Safari Vine, Word Tree).
- Plants progress through **5 growth stages** (Seed $\to$ Sprout $\to$ Stem $\to$ Bud $\to$ Radiant Blossom) based on accuracy and stars.
- Interactive **Watering Can** action showers animated SVG droplets with sparkle chimes.

### 7. 📒 Sticker Book (`/stickers`) & 🏆 Trophy Shelf (`/trophies`)
- **Sticker Book:** 24 collectible stickers across 4 album sheets (**Safari**, **Cosmic**, **Garden**, **Champions**) with tactile corner-peel animations.
- **Trophy Shelf:** 3D wooden bookcase presenting 8 prestigious awards with celebration fanfare modals.

### 8. 🛡️ Parent Gate & Parent Dashboard (`/parent`)
- **Parent Gate:** Child-resistant verification requiring a **3-second hold** on an SVG progress ring or answering an adult math challenge.
- **Parent Dashboard:** Standalone SVG horizontal mastery charts, daily session time bar charts, weak area learning recommendations, and session limit controls.

### 9. 🌙 Screen Time Rest Break ("Voxie is Sleepy")
- Configurable session limit (15m, 20m, 30m, 45m; default 20m).
- Friendly rest break overlay showing sleepy Voxie in a nightcap with floating "Zzz" bubbles.

### 10. ♿ Accessibility Controls
- **Dyslexia-Friendly Font:** Increased letter tracking (`0.06em`), word spacing, and heavy baselines.
- **High-Contrast Colorblind Mode:** 4px high-contrast outlines and clear text indicators.
- **Speech Speed Control:** Adjusts TTS prompts from 0.6x to 1.3x.
- **Volume & Mute:** Independent sliders for master sound effects and voice prompts.

---

## 💻 Visual Studio 2022 Quickstart (Ctrl+F5)

You can build, run, and debug Voxie Quest entirely inside **Visual Studio 2022** without command-line dependencies:

### Step 1: Open the Solution
1. Launch **Visual Studio 2022** (v17.8 or newer with the **ASP.NET and web development** workload).
2. Select **Open a project or solution**.
3. Browse to the project folder:
   ```
   C:\Users\Nilesh gupta\.gemini\antigravity\scratch\VoxieQuest\VoxieQuest.sln
   ```
4. Click **Open**.

### Step 2: Set Startup Project
1. In the **Solution Explorer** pane (right side), locate `src/VoxieQuest.Web`.
2. Right-click `VoxieQuest.Web` and select **Set as Startup Project**.
   *(The project name will turn **bold**).*

### Step 3: Run the Application
1. In the top toolbar, ensure the launch target is set to **VoxieQuest.Web** or **https** / **http**.
2. Press **`Ctrl + F5`** (*Debug > Start Without Debugging*).
   - Visual Studio automatically restores NuGet packages, compiles the Blazor WASM binary, launches the local DevServer, and opens your default browser (Google Chrome or Microsoft Edge).

### Step 4: Allow Microphone Access in Chrome / Edge
1. When the game loads for the first time, your browser will display a permission prompt:
   ```
   "localhost:PORT wants to use your microphone" -> Click [Allow]
   ```
2. If accidentally dismissed or blocked:
   - Click the **Tune / Lock icon** (`🔒` or `🎚️`) on the left side of Chrome's address bar.
   - Set **Microphone** to **Allow**.
   - Reload the page (`F5`).
3. Click the **🎙️ Mic Calibration** pill in Voxie Quest's header to test speech detection.

---

## 🧪 Running Unit Tests in Test Explorer

Voxie Quest includes **113 xUnit tests** covering adaptive difficulty, speech matching, childhood phonology, curriculum JSON schema, session timers, and garden growth.

### In Visual Studio 2022 GUI:
1. Open the Test Explorer window:
   - Menu: **Test > Test Explorer** (or press **`Ctrl + E, T`**).
2. In the Test Explorer toolbar, click the **Run All Tests In View** button (green play icon `▶▶`, or press **`Ctrl + R, A`**).
3. Test Explorer will discover and execute all 113 tests in `VoxieQuest.Tests`.
4. All tests will display a green checkmark (`✔ 113 Passed`).

### Via Command Line (.NET CLI):
```bash
dotnet test VoxieQuest.sln
```

---

## 🚀 Publishing & Free Hosting (GitHub Pages & Netlify)

Because Voxie Quest is a standalone static WebAssembly application with zero backend databases or server-side code, it can be hosted for free on **GitHub Pages**, **Netlify**, **Cloudflare Pages**, or **Vercel**.

### Step 1: Publish to a Local Folder in Visual Studio
1. In Visual Studio, right-click `VoxieQuest.Web` in Solution Explorer $\to$ select **Publish...**
2. In the Publish wizard:
   - **Target:** `Folder` $\to$ click **Next**.
   - **Specific Target:** `Folder` $\to$ click **Next**.
   - **Folder Location:** Choose an output folder (e.g. `bin\Release\net8.0\publish\`) $\to$ click **Finish**.
3. Click the **Publish** button.
4. The output static files are generated in the `publish\wwwroot\` folder:
   - `index.html`, `_framework/`, `css/`, `js/`, `fonts/`, `data/`, `favicon.png`.

---

### Option A: Free Hosting on GitHub Pages

GitHub Pages serves static sites from a repository branch.

#### 1. Adjust Base Href in `index.html`
In GitHub Pages, repositories are typically hosted at `https://<username>.github.io/<repository-name>/`.
Open `publish/wwwroot/index.html` and change:
```html
<!-- Default localhost base: -->
<base href="/" />

<!-- Change to your GitHub repository name: -->
<base href="/VoxieQuest/" />
```

#### 2. Add `.nojekyll` File
GitHub Pages uses Jekyll by default, which ignores folders starting with an underscore (such as `_framework/` where Blazor WASM binaries live).
Create an empty file named `.nojekyll` inside `publish/wwwroot/`:
```bash
# In PowerShell:
New-Item -ItemType File "publish/wwwroot/.nojekyll"
```

#### 3. Single Page App (SPA) Routing Trick (`404.html`)
To prevent 404 errors when a user refreshes on routes like `/games` or `/garden`, copy `index.html` as `404.html`:
```bash
# In PowerShell:
Copy-Item "publish/wwwroot/index.html" "publish/wwwroot/404.html"
```

#### 4. Push to `gh-pages` Branch
Push the contents of `publish/wwwroot/` to your `gh-pages` branch. Under GitHub repository **Settings > Pages**, select the `gh-pages` branch as source.

---

### Option B: Free Hosting on Netlify

Netlify provides instant drag-and-drop or Git-based hosting for Blazor WebAssembly.

#### 1. Add Single Page App Redirects File (`_redirects`)
Create a plain text file named `_redirects` inside `wwwroot/`:
```text
/*    /index.html   200
```

#### 2. Deploy
- **Drag-and-Drop:** Log into [Netlify](https://app.netlify.com/), go to **Sites**, and drag the `publish/wwwroot` folder into the upload dropzone.
- **Git Continuous Deployment:**
  - Build command: `dotnet publish -c Release`
  - Publish directory: `src/VoxieQuest.Web/bin/Release/net8.0/publish/wwwroot`

---

## 🎨 Curated Free Asset & Sound Resources

All visual assets and audio in Voxie Quest are designed to adhere to a warm, hand-drawn children's storybook aesthetic. Here are recommended free (CC0 / OFL / Royalty-Free) sources for expanding the art and audio:

### 1. Typography & Hand-Drawn Fonts (Open Font License)
- **[Fredoka](https://fonts.google.com/specimen/Fredoka)** *(Included locally in `wwwroot/fonts/`)*: Warm, rounded geometric sans-serif designed for children's literature.
- **[Nunito](https://fonts.google.com/specimen/Nunito)** *(Included locally in `wwwroot/fonts/`)*: Soft, well-balanced rounded terminal display typeface.
- **[OpenDyslexic](https://opendyslexic.org/)**: Open-source typeface with weighted bottoms designed to mitigate dyslexia symptoms.
- **[Sniglet](https://fonts.google.com/specimen/Sniglet)**: Playful rounded headline font suitable for storybooks.

### 2. Hand-Drawn Vector & SVG Illustrations (CC0 & Permissive)
- **[Kenney Game Assets](https://kenney.nl/assets)**: Over 40,000 CC0 public domain game assets, including cartoon animals, space rockets, and UI blocks.
- **[OpenGameArt.org](https://opengameart.org/)**: Community-driven 2D hand-drawn sprites, cartoon creatures, and picture book backgrounds.
- **[SVGRepo](https://www.svgrepo.com/)**: Clean, scalable SVG icons and illustrations with MIT / CC0 licensing.
- **[unDraw](https://undraw.co/)**: Open-source vector illustrations easily customizable with Voxie Quest's color tokens.

### 3. Sound Effects & Child-Friendly Audio (CC0 / Creative Commons)
- **Web Audio API Procedural Synthesis:** Built into `wwwroot/js/voxieInterop.js` — zero-file procedural synthesizer generating wooden clicks, bubble pops, joyful chimes, animal calls, and brass fanfares.
- **[Freesound.org](https://freesound.org/)**: High-quality CC0 samples of animal sounds (farm animals, birds, zoo calls) and playful toy percussion.
- **[Sonniss GDC Game Audio Archives](https://sonniss.com/gameaudioarchive)**: Annual commercial-grade royalty-free sound effects packs.
- **[Kenney Audio Packs](https://kenney.nl/assets/category:Audio)**: Whimsical interface clicks, positive chimes, fanfare melodies, and cartoon boings.

---

## 🔍 Assumptions & Known Browser Limits

### 1. Web Speech API Support
- **Best Browsers:** Google Chrome, Microsoft Edge, and Apple Safari (desktop and iOS) provide full, native hardware-accelerated Web Speech API support.
- **Mozilla Firefox:** Firefox disables the Web Speech API by default. If using Firefox, enable `media.webspeech.recognition.enable` in `about:config`, or use the built-in Practice Mode fallback buttons.
- **Secure Context Required:** Modern browsers require `https://` or `localhost` / `127.0.0.1` to access speech recognition and the microphone.

### 2. Microphone Permissions & Autoplay Audio Policies
- Browsers enforce an autoplay security policy: Web Audio `AudioContext` starts in a suspended state until the user interacts with the page (e.g. clicking *"Play Adventure"*, *"Water Plants"*, or tapping the microphone). Voxie Quest handles this by resuming audio on the first user interaction.
- The child must approve the one-time browser microphone prompt.

### 3. Storage Limits & Privacy
- Progress is stored in browser `localStorage`.
- Storage quota for `localStorage` across modern browsers is approximately **5MB to 10MB**, which is more than enough for thousands of child profiles and records.
- Clearing browser cache/cookies will reset local profiles unless exported.

### 4. Vosk Offline Recognition Fallback
- An offline Vosk-WASM fallback bridge is prepared in `wwwroot/js/voskInterop.js`. Loading full offline neural models requires downloading model weights (`~40MB`), so native browser Web Speech is prioritized for fast loading.

---

## 🛡️ Child-Safe Guarantee

Voxie Quest was engineered strictly under **COPPA** (Children's Online Privacy Protection Act) best practices:
- **Zero Third-Party Advertising:** No banner ads, interstitial popups, or sponsored links.
- **Zero Analytics SDKs & Telemetry:** No Google Analytics, Meta Pixel, tracking pixels, or remote telemetry servers.
- **Zero Account Creation:** No passwords, email addresses, phone numbers, or social logins collected.
- **Zero External Navigation:** Children cannot be directed outside the application boundaries.
- **Local-Only Persistence:** Child profiles, star counts, and accuracy metrics never leave the user's private browser storage.

---

*Voxie Quest — Created with ❤️ for young learners everywhere.*
