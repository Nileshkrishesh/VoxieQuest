using System.Text.Json.Serialization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// Detailed exploration metadata for an Indian State or Union Territory.
/// Fully static: each state features a cultural fun fact, landmark sticker,
/// and an interactive learning mini-game riddle.
/// </summary>
public class IndiaStateDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Capital { get; set; } = string.Empty;
    public bool IsUnionTerritory { get; set; } = false;
    public double LabelX { get; set; }
    public double LabelY { get; set; }
    public string SvgPath { get; set; } = string.Empty;

    // Static Exploration & Landmark Sticker Details
    public string LandmarkIcon { get; set; } = "⭐";
    public string LandmarkName { get; set; } = "Explorer Badge";
    public string FunFact { get; set; } = string.Empty;
    public string MiniGameQuestion { get; set; } = string.Empty;
    public List<string> MiniGameChoices { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string CelebrationCheer { get; set; } = string.Empty;
}

/// <summary>
/// Child's personal India Explorer Sticker Passport stored locally in browser storage.
/// Zero backend, zero personal tracking, 100% private.
/// </summary>
public class IndiaPassportProgress
{
    /// <summary>Set of unlocked state codes (e.g. ["PB", "RJ", "MH"]).</summary>
    public HashSet<string> UnlockedStateCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Last state explored by the child.</summary>
    public string? LastExploredStateCode { get; set; }

    /// <summary>Timestamp of last unlocked sticker.</summary>
    public DateTime? LastUnlockedUtc { get; set; }

    /// <summary>Number of collected stickers out of 36.</summary>
    [JsonIgnore]
    public int CollectedCount => UnlockedStateCodes.Count;

    /// <summary>Total states and UTs in India (36).</summary>
    [JsonIgnore]
    public int TotalStatesCount => IndiaStateRegistry.States.Count;

    /// <summary>Percentage of India explored so far.</summary>
    [JsonIgnore]
    public double ExplorationPercentage => Math.Round((double)CollectedCount / TotalStatesCount * 100.0, 1);
}

/// <summary>
/// Full registry of all 28 States and 8 Union Territories with verified landmark stickers,
/// child-friendly fun facts, and exploratory mini-games.
/// </summary>
public static class IndiaStateRegistry
{
    public static readonly Dictionary<string, IndiaStateDefinition> States = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PB"] = new()
        {
            Code = "PB", Name = "Punjab", Capital = "Chandigarh",
            LabelX = 300, LabelY = 260,
            SvgPath = "M 270 240 L 340 240 L 330 290 L 280 290 Z",
            LandmarkIcon = "🛕", LandmarkName = "Golden Temple",
            FunFact = "The land of five sparkling rivers, joyful Bhangra dances, and the peaceful Golden Temple shining brightly in Amritsar!",
            MiniGameQuestion = "Which golden landmark shines peacefully over the sacred water in Amritsar, Punjab?",
            MiniGameChoices = new() { "🛕 Golden Temple", "🏰 Sandcastle", "⛵ Houseboat" },
            CorrectAnswer = "🛕 Golden Temple",
            CelebrationCheer = "Bole So Nihal! You earned the Golden Temple sticker for your passport! 🛕✨"
        },
        ["RJ"] = new()
        {
            Code = "RJ", Name = "Rajasthan", Capital = "Jaipur",
            LabelX = 240, LabelY = 370,
            SvgPath = "M 230 280 L 310 300 L 320 400 L 260 460 L 170 410 L 170 340 Z",
            LandmarkIcon = "🐪", LandmarkName = "Thar Desert Camel",
            FunFact = "The land of colourful turbans, majestic hilltop forts, and the vast golden sand dunes of the Thar Desert!",
            MiniGameQuestion = "Which friendly animal with a hump is celebrated as the 'Ship of the Desert' in Rajasthan?",
            MiniGameChoices = new() { "🐪 Desert Camel", "🐧 Penguin", "🐬 Dolphin" },
            CorrectAnswer = "🐪 Desert Camel",
            CelebrationCheer = "Padharo Mhare Desh! The desert camel is now in your sticker passport! 🐪🌟"
        },
        ["MH"] = new()
        {
            Code = "MH", Name = "Maharashtra", Capital = "Mumbai",
            LabelX = 300, LabelY = 600,
            SvgPath = "M 220 550 L 350 540 L 460 560 L 420 670 L 280 660 L 220 590 Z",
            LandmarkIcon = "🏛️", LandmarkName = "Gateway of India",
            FunFact = "Home of the grand Gateway of India on the Arabian Sea, lush Western Ghat waterfalls, and sweet Alphonso mangoes!",
            MiniGameQuestion = "Which grand stone archway welcomes ships by the sparkling sea in Mumbai?",
            MiniGameChoices = new() { "🏛️ Gateway of India", "🗼 Eiffel Tower", "🎪 Circus Tent" },
            CorrectAnswer = "🏛️ Gateway of India",
            CelebrationCheer = "Jai Maharashtra! You unlocked the Gateway of India sticker! 🏛️🌊"
        },
        ["UP"] = new()
        {
            Code = "UP", Name = "Uttar Pradesh", Capital = "Lucknow",
            LabelX = 450, LabelY = 370,
            SvgPath = "M 360 320 L 470 300 L 580 370 L 530 450 L 430 430 L 370 370 Z",
            LandmarkIcon = "🕌", LandmarkName = "Taj Mahal",
            FunFact = "Home of the world-famous white marble Taj Mahal, historic Lucknow palaces, and the sacred winding Ganges River!",
            MiniGameQuestion = "Which world-famous white marble wonder stands gracefully by the river in Agra?",
            MiniGameChoices = new() { "🕌 Taj Mahal", "🗿 Stone Statue", "🎡 Ferris Wheel" },
            CorrectAnswer = "🕌 Taj Mahal",
            CelebrationCheer = "Wah Taj! The world-famous Taj Mahal sticker is yours! 🕌✨"
        },
        ["WB"] = new()
        {
            Code = "WB", Name = "West Bengal", Capital = "Kolkata",
            LabelX = 680, LabelY = 470,
            SvgPath = "M 670 370 L 705 370 L 690 450 L 720 520 L 670 550 L 650 460 Z",
            LandmarkIcon = "🐯", LandmarkName = "Royal Bengal Tiger",
            FunFact = "The lush mangrove forests of the Sundarbans are home to the striped Royal Bengal Tiger and sweet spongy Rasgullas!",
            MiniGameQuestion = "Which powerful striped big cat swims and roams the Sundarbans mangrove forests of Bengal?",
            MiniGameChoices = new() { "🐯 Royal Bengal Tiger", "🐼 Giant Panda", "🦘 Kangaroo" },
            CorrectAnswer = "🐯 Royal Bengal Tiger",
            CelebrationCheer = "Shubho Bijoya! You spotted the Royal Bengal Tiger sticker! 🐯🌿"
        },
        ["GJ"] = new()
        {
            Code = "GJ", Name = "Gujarat", Capital = "Gandhinagar",
            LabelX = 170, LabelY = 490,
            SvgPath = "M 130 440 L 240 450 L 250 540 L 190 570 L 100 520 Z",
            LandmarkIcon = "🦁", LandmarkName = "Gir Asiatic Lion",
            FunFact = "The Gir Forest in Gujarat is the only sanctuary in all of Asia where wild Asiatic lions roam freely in nature!",
            MiniGameQuestion = "Which majestic roaring animal with a golden mane is protected in Gujarat's Gir National Park?",
            MiniGameChoices = new() { "🦁 Asiatic Lion", "🐻 Polar Bear", "🦥 Tree Sloth" },
            CorrectAnswer = "🦁 Asiatic Lion",
            CelebrationCheer = "Majama! The mighty Asiatic Lion sticker is unlocked! 🦁🎉"
        },
        ["KA"] = new()
        {
            Code = "KA", Name = "Karnataka", Capital = "Bengaluru",
            LabelX = 300, LabelY = 740,
            SvgPath = "M 270 660 L 370 670 L 380 800 L 300 840 L 260 730 Z",
            LandmarkIcon = "🛕", LandmarkName = "Hampi Stone Chariot",
            FunFact = "Famous for ancient stone chariots in Hampi, royal Mysore palaces, and green coffee hill stations in Coorg!",
            MiniGameQuestion = "What ancient stone chariot carved out of granite rock is famous in historic Hampi?",
            MiniGameChoices = new() { "🛕 Hampi Stone Chariot", "🚀 Space Shuttle", "🏎️ Race Car" },
            CorrectAnswer = "🛕 Hampi Stone Chariot",
            CelebrationCheer = "Namaskara! You collected the Hampi Stone Chariot sticker! 🛕☕"
        },
        ["TN"] = new()
        {
            Code = "TN", Name = "Tamil Nadu", Capital = "Chennai",
            LabelX = 370, LabelY = 870,
            SvgPath = "M 330 800 L 420 800 L 400 940 L 325 930 Z",
            LandmarkIcon = "🛕", LandmarkName = "Meenakshi Temple Gopuram",
            FunFact = "Renowned for soaring colourful temple towers (Gopurams), graceful Bharatanatyam dance, and Marina Beach breeze!",
            MiniGameQuestion = "What colourful soaring gateway towers decorated with thousands of painted figures are iconic in Tamil Nadu?",
            MiniGameChoices = new() { "🛕 Temple Gopuram", "🗼 Windmill", "⛺ Tent" },
            CorrectAnswer = "🛕 Temple Gopuram",
            CelebrationCheer = "Vanakkam! You won the colourful Temple Gopuram sticker! 🛕🌸"
        },
        ["KL"] = new()
        {
            Code = "KL", Name = "Kerala", Capital = "Thiruvananthapuram",
            LabelX = 310, LabelY = 890,
            SvgPath = "M 290 830 L 330 830 L 320 950 L 285 910 Z",
            LandmarkIcon = "⛵", LandmarkName = "Backwaters Houseboat",
            FunFact = "Known as 'God's Own Country' with peaceful palm-fringed canals, fragrant spice gardens, and traditional Kathakali masks!",
            MiniGameQuestion = "What traditional wooden boat glides peacefully along the coconut-fringed backwaters of Kerala?",
            MiniGameChoices = new() { "⛵ Backwaters Houseboat", "🚢 Cargo Ship", "🛶 Raft" },
            CorrectAnswer = "⛵ Backwaters Houseboat",
            CelebrationCheer = "Namaskaram! The Backwaters Houseboat sticker is added to your passport! ⛵🌴"
        },
        ["AS"] = new()
        {
            Code = "AS", Name = "Assam", Capital = "Dispur",
            LabelX = 770, LabelY = 390,
            SvgPath = "M 715 365 L 810 350 L 830 400 L 740 430 Z",
            LandmarkIcon = "🦏", LandmarkName = "One-Horned Rhino",
            FunFact = "Kaziranga National Park in Assam protects the world's greatest family of majestic armoured one-horned rhinos amidst lush green tea fields!",
            MiniGameQuestion = "Which armoured giant herbivore with a single horn on its snout loves the tall grasslands of Assam?",
            MiniGameChoices = new() { "🦏 One-Horned Rhino", "🦒 Tall Giraffe", "🦓 Zebra" },
            CorrectAnswer = "🦏 One-Horned Rhino",
            CelebrationCheer = "Bihu joy! You earned the rare One-Horned Rhino sticker! 🦏🍵"
        },
        ["JK"] = new()
        {
            Code = "JK", Name = "Jammu & Kashmir", Capital = "Srinagar/Jammu", IsUnionTerritory = true,
            LabelX = 330, LabelY = 180,
            SvgPath = "M 280 120 L 330 110 L 370 140 L 380 190 L 350 230 L 300 230 L 260 190 Z",
            LandmarkIcon = "🛶", LandmarkName = "Dal Lake Shikara",
            FunFact = "Surrounded by snow-capped Himalayan peaks, fragrant saffron meadows, and beautiful wooden Shikara boats on Dal Lake!",
            MiniGameQuestion = "What colourful roofed wooden boat floats gracefully across the waters of Dal Lake in Srinagar?",
            MiniGameChoices = new() { "🛶 Dal Lake Shikara", "🛸 Flying Saucer", "🚜 Tractor" },
            CorrectAnswer = "🛶 Dal Lake Shikara",
            CelebrationCheer = "Kashmir paradise! The Shikara boat sticker is in your passport! 🛶🏔️"
        },
        ["LA"] = new()
        {
            Code = "LA", Name = "Ladakh", Capital = "Leh", IsUnionTerritory = true,
            LabelX = 440, LabelY = 130,
            SvgPath = "M 370 140 L 450 70 L 520 120 L 490 200 L 420 220 L 380 190 Z",
            LandmarkIcon = "🏔️", LandmarkName = "Pangong Blue Lake",
            FunFact = "A breathtaking high-altitude mountain wonderland with sparkling blue Pangong Lake, fluttering prayer flags, and friendly snow leopards!",
            MiniGameQuestion = "Which high-altitude mountain lake in Ladakh is famous for shifting shades from turquoise to deep royal blue?",
            MiniGameChoices = new() { "🏔️ Pangong Blue Lake", "🏊 Backyard Pool", "🕳️ Well" },
            CorrectAnswer = "🏔️ Pangong Blue Lake",
            CelebrationCheer = "Julley! You collected the Pangong Blue Lake sticker! 🏔️✨"
        },
        ["HP"] = new()
        {
            Code = "HP", Name = "Himachal Pradesh", Capital = "Shimla",
            LabelX = 370, LabelY = 240,
            SvgPath = "M 350 230 L 420 220 L 430 260 L 390 290 L 340 260 Z",
            LandmarkIcon = "🍎", LandmarkName = "Himachal Apple Orchard",
            FunFact = "Famous for crisp sweet red apples, scenic mountain toy trains, and snow-dusted pine forests in the Himalayas!",
            MiniGameQuestion = "What juicy sweet red fruit grows in sunny hillside orchards across Himachal Pradesh?",
            MiniGameChoices = new() { "🍎 Crisp Apple", "🍌 Banana", "🍍 Pineapple" },
            CorrectAnswer = "🍎 Crisp Apple",
            CelebrationCheer = "Mountain fresh! You earned the Himachal Apple sticker! 🍎🌲"
        },
        ["UT"] = new()
        {
            Code = "UT", Name = "Uttarakhand", Capital = "Dehradun",
            LabelX = 425, LabelY = 280,
            SvgPath = "M 390 260 L 450 250 L 470 300 L 415 320 Z",
            LandmarkIcon = "🌸", LandmarkName = "Valley of Flowers",
            FunFact = "Known as Devbhoomi with the sacred Valley of Flowers, snowy Nanda Devi peak, and crystal-clear mountain streams!",
            MiniGameQuestion = "What natural wonderland high in the Uttarakhand mountains blooms with thousands of wild alpine blossoms?",
            MiniGameChoices = new() { "🌸 Valley of Flowers", "🌵 Sand Dunes", "🏢 Skyscrapers" },
            CorrectAnswer = "🌸 Valley of Flowers",
            CelebrationCheer = "Blossoms in bloom! You collected the Valley of Flowers sticker! 🌸🏔️"
        },
        ["HR"] = new()
        {
            Code = "HR", Name = "Haryana", Capital = "Chandigarh",
            LabelX = 330, LabelY = 310,
            SvgPath = "M 300 285 L 355 285 L 360 340 L 310 335 Z",
            LandmarkIcon = "🦚", LandmarkName = "Dancing Peacock",
            FunFact = "A rich agricultural heartland with golden mustard fields and the joyful national bird, the dancing peacock, spreading its iridescent feathers!",
            MiniGameQuestion = "Which magnificent dancing bird with a shimmering emerald-and-blue fan of feathers is cherished across Haryana?",
            MiniGameChoices = new() { "🦚 Dancing Peacock", "🦜 Little Parrot", "🐧 Penguin" },
            CorrectAnswer = "🦚 Dancing Peacock",
            CelebrationCheer = "Hooray! The majestic Peacock sticker is unlocked! 🦚🌾"
        },
        ["DL"] = new()
        {
            Code = "DL", Name = "Delhi", Capital = "New Delhi", IsUnionTerritory = true,
            LabelX = 355, LabelY = 320,
            SvgPath = "M 348 312 L 362 312 L 362 326 L 348 326 Z",
            LandmarkIcon = "🇮🇳", LandmarkName = "India Gate",
            FunFact = "The vibrant capital of India where history meets modernity, featuring the grand India Gate, Red Fort, and lush garden monuments!",
            MiniGameQuestion = "Which iconic sandstone triumphal archway stands proudly along the Kartavya Path in New Delhi?",
            MiniGameChoices = new() { "🇮🇳 India Gate", "🌉 Suspension Bridge", "🚪 Wooden Door" },
            CorrectAnswer = "🇮🇳 India Gate",
            CelebrationCheer = "Capital salute! The India Gate sticker shines in your passport! 🇮🇳⭐"
        },
        ["BR"] = new()
        {
            Code = "BR", Name = "Bihar", Capital = "Patna",
            LabelX = 600, LabelY = 400,
            SvgPath = "M 560 370 L 670 380 L 660 450 L 560 440 Z",
            LandmarkIcon = "🌳", LandmarkName = "Bodh Gaya Bodhi Tree",
            FunFact = "The historic cradle of learning home to ancient Nalanda University and the sacred Bodhi Tree where mindfulness and wisdom blossomed!",
            MiniGameQuestion = "Which sacred tree with heart-shaped leaves is revered in Bodh Gaya, Bihar as a symbol of enlightenment?",
            MiniGameChoices = new() { "🌳 Sacred Bodhi Tree", "🌴 Coconut Palm", "🎄 Pine Tree" },
            CorrectAnswer = "🌳 Sacred Bodhi Tree",
            CelebrationCheer = "Wisdom blooms! You earned the Sacred Bodhi Tree sticker! 🌳🕊️"
        },
        ["JH"] = new()
        {
            Code = "JH", Name = "Jharkhand", Capital = "Ranchi",
            LabelX = 590, LabelY = 475,
            SvgPath = "M 560 440 L 650 450 L 640 520 L 560 510 Z",
            LandmarkIcon = "🌊", LandmarkName = "Hundru Waterfalls",
            FunFact = "The land of forests and thunderous waterfalls, rich in minerals, vibrant tribal Sohrai wall paintings, and lush Sal woodlands!",
            MiniGameQuestion = "What thunderous cascade of river water plunges dramatically over steep rocks near Ranchi, Jharkhand?",
            MiniGameChoices = new() { "🌊 Hundru Waterfalls", "🚰 Kitchen Sink", "🛁 Bathtub" },
            CorrectAnswer = "🌊 Hundru Waterfalls",
            CelebrationCheer = "Waterfall thunder! You unlocked the Hundru Falls sticker! 🌊🍃"
        },
        ["OD"] = new()
        {
            Code = "OD", Name = "Odisha", Capital = "Bhubaneswar",
            LabelX = 590, LabelY = 560,
            SvgPath = "M 550 510 L 640 520 L 630 620 L 540 610 Z",
            LandmarkIcon = "☀️", LandmarkName = "Konark Sun Chariot Wheel",
            FunFact = "Famous for the giant stone chariot wheel of Konark Sun Temple, nesting Olive Ridley sea turtles, and Chilika Lake's gentle dolphins!",
            MiniGameQuestion = "What famous ancient stone carving shaped like a 24-spoke astronomical wheel is celebrated in Konark, Odisha?",
            MiniGameChoices = new() { "☀️ Konark Sun Wheel", "🚲 Bicycle Tire", "🍩 Sweet Doughnut" },
            CorrectAnswer = "☀️ Konark Sun Wheel",
            CelebrationCheer = "Sunbeam victory! The Konark Sun Wheel sticker is yours! ☀️🛕"
        },
        ["MP"] = new()
        {
            Code = "MP", Name = "Madhya Pradesh", Capital = "Bhopal",
            LabelX = 380, LabelY = 470,
            SvgPath = "M 300 420 L 430 420 L 530 460 L 490 540 L 340 540 L 290 470 Z",
            LandmarkIcon = "🛕", LandmarkName = "Sanchi Great Stupa",
            FunFact = "The heart of India, home to the Great Sanchi Stupa built by Emperor Ashoka, the ancient rock shelters of Bhimbetka, and tiger sanctuaries!",
            MiniGameQuestion = "What ancient domed Buddhist stone monument commissioned by Emperor Ashoka stands in Sanchi, Madhya Pradesh?",
            MiniGameChoices = new() { "🛕 Sanchi Great Stupa", "🎪 Big Circus", "🏭 Glass Factory" },
            CorrectAnswer = "🛕 Sanchi Great Stupa",
            CelebrationCheer = "Heart of India! You unlocked the Great Sanchi Stupa sticker! 🛕🐯"
        },
        ["CT"] = new()
        {
            Code = "CT", Name = "Chhattisgarh", Capital = "Raipur",
            LabelX = 490, LabelY = 530,
            SvgPath = "M 480 470 L 540 490 L 530 630 L 470 610 Z",
            LandmarkIcon = "🌊", LandmarkName = "Chitrakote Niagara Falls",
            FunFact = "Blessed with dense green teak forests and the horseshoe-shaped Chitrakote Falls, celebrated as the 'Niagara of India'!",
            MiniGameQuestion = "Which wide horseshoe-shaped waterfall in Bastar, Chhattisgarh is affectionately called the 'Niagara of India'?",
            MiniGameChoices = new() { "🌊 Chitrakote Falls", "⛲ Park Fountain", "🕳️ Deep Well" },
            CorrectAnswer = "🌊 Chitrakote Falls",
            CelebrationCheer = "Splash of adventure! You earned the Chitrakote Falls sticker! 🌊🌲"
        },
        ["TG"] = new()
        {
            Code = "TG", Name = "Telangana", Capital = "Hyderabad",
            LabelX = 400, LabelY = 670,
            SvgPath = "M 380 620 L 470 620 L 450 710 L 370 690 Z",
            LandmarkIcon = "🏛️", LandmarkName = "Charminar Minarets",
            FunFact = "Hyderabad is celebrated for the four soaring minarets of Charminar, shimmering natural pearls, and the historic Golconda Fort!",
            MiniGameQuestion = "What landmark monument built in 1591 in Hyderabad has four graceful minarets reaching up to the sky?",
            MiniGameChoices = new() { "🏛️ Charminar", "🗼 Clock Tower", "🏯 Treehouse" },
            CorrectAnswer = "🏛️ Charminar",
            CelebrationCheer = "Shandaar! You collected the Charminar Minarets sticker! 🏛️💎"
        },
        ["AP"] = new()
        {
            Code = "AP", Name = "Andhra Pradesh", Capital = "Amaravati",
            LabelX = 430, LabelY = 750,
            SvgPath = "M 450 630 L 540 620 L 480 810 L 410 800 L 440 710 Z",
            LandmarkIcon = "🛕", LandmarkName = "Tirumala Hills",
            FunFact = "The scenic coastal realm with the sacred seven hills of Tirumala, the classical rhythm of Kuchipudi dance, and Kondapalli wooden toys!",
            MiniGameQuestion = "What famous hilltop pilgrimage city nestled in the lush Seshachalam hills is located in Andhra Pradesh?",
            MiniGameChoices = new() { "🛕 Tirumala Hills", "🏖️ Desert Dunes", "❄️ Snow Glacier" },
            CorrectAnswer = "🛕 Tirumala Hills",
            CelebrationCheer = "Govinda! The Tirumala Hills sticker is in your passport! 🛕🌿"
        },
        ["GA"] = new()
        {
            Code = "GA", Name = "Goa", Capital = "Panaji",
            LabelX = 250, LabelY = 715,
            SvgPath = "M 245 705 L 265 705 L 260 730 L 240 725 Z",
            LandmarkIcon = "🏖️", LandmarkName = "Golden Palm Beach",
            FunFact = "Famous for golden sunlit shores along the Arabian Sea, heritage churches, peaceful spice farms, and bright blue kingfisher birds!",
            MiniGameQuestion = "What warm coastal scenery with soft sand and swaying coconut palms makes Goa famous worldwide?",
            MiniGameChoices = new() { "🏖️ Golden Palm Beach", "❄️ Frozen Tundra", "🌋 Volcano" },
            CorrectAnswer = "🏖️ Golden Palm Beach",
            CelebrationCheer = "Viva Goa! The Golden Beach sticker is now yours! 🏖️🥥"
        },
        ["SK"] = new()
        {
            Code = "SK", Name = "Sikkim", Capital = "Gangtok",
            LabelX = 690, LabelY = 345,
            SvgPath = "M 675 330 L 705 330 L 705 365 L 675 365 Z",
            LandmarkIcon = "🏔️", LandmarkName = "Mount Kangchenjunga",
            FunFact = "India's first fully organic state, guarded by the snowy heights of Mount Kangchenjunga and home to adorable red pandas!",
            MiniGameQuestion = "What third-highest mountain peak in the whole world stands guard above the rhododendron valleys of Sikkim?",
            MiniGameChoices = new() { "🏔️ Mount Kangchenjunga", "⛰️ Low Hill", "🗻 Mount Fuji" },
            CorrectAnswer = "🏔️ Mount Kangchenjunga",
            CelebrationCheer = "Himalayan wonder! You earned the Kangchenjunga sticker! 🏔️🐾"
        },
        ["AR"] = new()
        {
            Code = "AR", Name = "Arunachal Pradesh", Capital = "Itanagar",
            LabelX = 840, LabelY = 320,
            SvgPath = "M 770 320 L 880 300 L 910 360 L 820 360 Z",
            LandmarkIcon = "🌄", LandmarkName = "Dawn-Lit Sunrise",
            FunFact = "Known as the 'Land of the Dawn-Lit Mountains' because it is the very first corner of India to greet the rising morning sun!",
            MiniGameQuestion = "Why is Arunachal Pradesh celebrated across India as the 'Land of the Dawn-Lit Mountains'?",
            MiniGameChoices = new() { "🌄 First Sunrise in India", "🌙 Only Moonlight", "⭐ Never Any Light" },
            CorrectAnswer = "🌄 First Sunrise in India",
            CelebrationCheer = "Morning glory! The Dawn-Lit Sunrise sticker is in your passport! 🌄🏔️"
        },
        ["ML"] = new()
        {
            Code = "ML", Name = "Meghalaya", Capital = "Shillong",
            LabelX = 745, LabelY = 420,
            SvgPath = "M 715 410 L 780 410 L 775 440 L 715 435 Z",
            LandmarkIcon = "🌿", LandmarkName = "Living Root Bridges",
            FunFact = "The abode of clouds, famous for magical living root bridges handcrafted over centuries from growing rubber fig tree roots across rivers!",
            MiniGameQuestion = "What unique bridges in Meghalaya are grown naturally from the living roots of rubber fig trees?",
            MiniGameChoices = new() { "🌿 Living Root Bridges", "🔩 Steel Cable Bridge", "🧱 Concrete Wall" },
            CorrectAnswer = "🌿 Living Root Bridges",
            CelebrationCheer = "Abode of Clouds! The Living Root Bridge sticker is unlocked! 🌿☁️"
        },
        ["NL"] = new()
        {
            Code = "NL", Name = "Nagaland", Capital = "Kohima",
            LabelX = 855, LabelY = 400,
            SvgPath = "M 830 380 L 870 380 L 860 430 L 825 420 Z",
            LandmarkIcon = "🪶", LandmarkName = "Great Hornbill",
            FunFact = "Renowned for the vibrant Hornbill Festival celebrating unity, traditional warrior dances, and emerald green Dzukou Valley!",
            MiniGameQuestion = "Which large colourful forest bird with a grand curved beak is celebrated at Nagaland's famous festival of festivals?",
            MiniGameChoices = new() { "🪶 Great Hornbill", "🦆 Duck", "🦉 Owl" },
            CorrectAnswer = "🪶 Great Hornbill",
            CelebrationCheer = "Nagaland festival cheer! You collected the Hornbill sticker! 🪶✨"
        },
        ["MN"] = new()
        {
            Code = "MN", Name = "Manipur", Capital = "Imphal",
            LabelX = 840, LabelY = 450,
            SvgPath = "M 825 430 L 865 430 L 855 480 L 820 475 Z",
            LandmarkIcon = "🦌", LandmarkName = "Dancing Sangai Deer",
            FunFact = "Home of Loktak Lake with floating islands of vegetation (phumdis) and the graceful, endangered dancing Sangai deer found nowhere else on Earth!",
            MiniGameQuestion = "Which rare, graceful deer that balances on the floating reed islands of Loktak Lake is unique to Manipur?",
            MiniGameChoices = new() { "🦌 Dancing Sangai Deer", "🦒 Tall Giraffe", "🐫 Desert Camel" },
            CorrectAnswer = "🦌 Dancing Sangai Deer",
            CelebrationCheer = "Jewel of India! The dancing Sangai Deer sticker is yours! 🦌🌸"
        },
        ["MZ"] = new()
        {
            Code = "MZ", Name = "Mizoram", Capital = "Aizawl",
            LabelX = 810, LabelY = 505,
            SvgPath = "M 800 480 L 835 480 L 825 545 L 795 530 Z",
            LandmarkIcon = "🎋", LandmarkName = "Cheraw Bamboo Dance",
            FunFact = "A peaceful hill state where dancers leap gracefully between rhythmic clacking bamboo poles in the world-famous Cheraw dance!",
            MiniGameQuestion = "What energetic traditional folk dance in Mizoram is performed by stepping rhythmically between pairs of bamboo poles?",
            MiniGameChoices = new() { "🎋 Cheraw Bamboo Dance", "🩰 Ballet", "💃 Tango" },
            CorrectAnswer = "🎋 Cheraw Bamboo Dance",
            CelebrationCheer = "Chibai! The Cheraw Bamboo Dance sticker is in your passport! 🎋🎶"
        },
        ["TR"] = new()
        {
            Code = "TR", Name = "Tripura", Capital = "Agartala",
            LabelX = 770, LabelY = 480,
            SvgPath = "M 755 460 L 785 460 L 780 505 L 750 495 Z",
            LandmarkIcon = "🏰", LandmarkName = "Neermahal Water Palace",
            FunFact = "Home to the fairytale Neermahal Palace rising right out of Rudrasagar Lake, and the mysterious stone face carvings of Unakoti!",
            MiniGameQuestion = "What magnificent royal summer palace in Tripura is built right in the middle of Rudrasagar Lake?",
            MiniGameChoices = new() { "🏰 Neermahal Water Palace", "⛺ Tent in the Woods", "🏚️ Old Shed" },
            CorrectAnswer = "🏰 Neermahal Water Palace",
            CelebrationCheer = "Lake palace magic! You unlocked the Neermahal Palace sticker! 🏰💧"
        },
        ["AN"] = new()
        {
            Code = "AN", Name = "Andaman & Nicobar", Capital = "Port Blair", IsUnionTerritory = true,
            LabelX = 780, LabelY = 780,
            SvgPath = "M 770 730 L 790 730 L 790 850 L 770 850 Z",
            LandmarkIcon = "🐢", LandmarkName = "Sea Turtle & Coral Reef",
            FunFact = "Tropical islands surrounded by crystal turquoise waters, living coral reefs, gentle dugongs, and nesting sea turtles on white beaches!",
            MiniGameQuestion = "Which gentle ocean creature with a flippered shell swims through the coral reefs of the Andaman Islands?",
            MiniGameChoices = new() { "🐢 Sea Turtle", "🦈 Shark", "🐙 Octopus" },
            CorrectAnswer = "🐢 Sea Turtle",
            CelebrationCheer = "Island breeze! The Andaman Sea Turtle sticker is yours! 🐢🌊"
        },
        ["CH"] = new()
        {
            Code = "CH", Name = "Chandigarh", Capital = "Chandigarh", IsUnionTerritory = true,
            LabelX = 345, LabelY = 265,
            SvgPath = "M 335 260 L 350 260 L 350 275 L 335 275 Z",
            LandmarkIcon = "🗿", LandmarkName = "Nek Chand Rock Garden",
            FunFact = "The City Beautiful, featuring the world-renowned Rock Garden created entirely out of recycled ceramic pots, bangles, and industrial scrap!",
            MiniGameQuestion = "What whimsical wonderland in Chandigarh was built entirely out of recycled home and ceramic materials?",
            MiniGameChoices = new() { "🗿 Nek Chand Rock Garden", "🏭 Steel Foundry", "🏬 Department Store" },
            CorrectAnswer = "🗿 Nek Chand Rock Garden",
            CelebrationCheer = "City Beautiful! You collected the Rock Garden sticker! 🗿🌿"
        },
        ["DH"] = new()
        {
            Code = "DH", Name = "Dadra & Nagar Haveli and Daman & Diu", Capital = "Daman", IsUnionTerritory = true,
            LabelX = 210, LabelY = 560,
            SvgPath = "M 205 550 L 220 550 L 220 565 L 205 565 Z",
            LandmarkIcon = "🏰", LandmarkName = "Diu Sea Fortress",
            FunFact = "Coastal territories featuring sea-facing stone lighthouses, Portuguese forts surrounded by waves, and quiet beaches on the Gulf of Khambhat!",
            MiniGameQuestion = "Which historic stone fortress rises majestically right at the edge of the Arabian Sea in Diu?",
            MiniGameChoices = new() { "🏰 Diu Sea Fortress", "🛸 Flying Saucer", "🛖 Straw Hut" },
            CorrectAnswer = "🏰 Diu Sea Fortress",
            CelebrationCheer = "Coastal heritage! You collected the Diu Sea Fortress sticker! 🏰⛵"
        },
        ["LD"] = new()
        {
            Code = "LD", Name = "Lakshadweep", Capital = "Kavaratti", IsUnionTerritory = true,
            LabelX = 220, LabelY = 860,
            SvgPath = "M 210 840 L 230 840 L 230 890 L 210 890 Z",
            LandmarkIcon = "🐠", LandmarkName = "Coral Atolls & Clownfish",
            FunFact = "Thirty-six pristine coral atolls with bright clownfish darting through sea anemones, crystal lagoons, and peaceful palm shorelines!",
            MiniGameQuestion = "What colourful living marine colonies create the clear turquoise lagoons of Lakshadweep?",
            MiniGameChoices = new() { "🐠 Coral Reefs & Clownfish", "🧊 Iceberg", "🏭 Factory" },
            CorrectAnswer = "🐠 Coral Reefs & Clownfish",
            CelebrationCheer = "Coral paradise! The Coral Reef sticker is in your passport! 🐠🐚"
        },
        ["PY"] = new()
        {
            Code = "PY", Name = "Puducherry", Capital = "Puducherry", IsUnionTerritory = true,
            LabelX = 425, LabelY = 825,
            SvgPath = "M 418 820 L 432 820 L 432 834 L 418 834 Z",
            LandmarkIcon = "🏛️", LandmarkName = "Auroville Matrimandir",
            FunFact = "A seaside town with mustard-yellow French heritage villas, palm-lined promenades, and the golden sphere of Matrimandir in Auroville!",
            MiniGameQuestion = "What magnificent golden sphere dedicated to peace and quiet reflection stands at the center of Auroville, Puducherry?",
            MiniGameChoices = new() { "🏛️ Matrimandir Golden Dome", "⚽ Soccer Ball", "🍩 Giant Donut" },
            CorrectAnswer = "🏛️ Matrimandir Golden Dome",
            CelebrationCheer = "Peace & wonder! The Matrimandir Golden Dome sticker is yours! 🏛️✨"
        }
    };

    public static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var clean = code.Trim().ToUpperInvariant();
        if (clean.StartsWith("IN-")) clean = clean[3..];
        return States.ContainsKey(clean);
    }

    public static string NormalizeCode(string code)
    {
        var clean = code.Trim().ToUpperInvariant();
        if (clean.StartsWith("IN-")) clean = clean[3..];
        return clean;
    }
}
