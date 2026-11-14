using System.Text.Json.Serialization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// Learner activity band for states on the Voxie Across India map.
/// </summary>
public enum IndiaActivityBand
{
    /// <summary>Fewer than 20 learners or newly added state.</summary>
    JustStarting = 0,

    /// <summary>20 to 199 learners actively exploring quests.</summary>
    Growing = 1,

    /// <summary>200+ learners actively exploring quests.</summary>
    Busy = 2
}

/// <summary>
/// Representation of an Indian State or Union Territory for the picture-book map.
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
}

/// <summary>
/// Aggregated, privacy-thresholded state player total received from the map API.
/// </summary>
public class IndiaStateData
{
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;

    /// <summary>
    /// Rounded count text (e.g. "Just starting here!", "50+", "100+", "500+", "1,000+").
    /// Exact count is NEVER exposed to prevent micro-targeting or identification.
    /// </summary>
    public string DisplayCount { get; set; } = "Just starting here!";

    /// <summary>Activity band for map coloring.</summary>
    public IndiaActivityBand ActivityBand { get; set; } = IndiaActivityBand.JustStarting;

    /// <summary>Indicates whether this is the child's opted-in state (displays a golden star ⭐).</summary>
    public bool IsChildState { get; set; } = false;
}

/// <summary>
/// Response payload from GET /api/map.
/// </summary>
public class IndiaMapResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("statesWithActiveLearners")]
    public int StatesWithActiveLearners { get; set; } = 0;

    [JsonPropertyName("states")]
    public Dictionary<string, IndiaStateApiItem> States { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("isOfflineFallback")]
    public bool IsOfflineFallback { get; set; } = false;
}

public class IndiaStateApiItem
{
    [JsonPropertyName("countText")]
    public string CountText { get; set; } = "Just starting here!";

    [JsonPropertyName("band")]
    public string Band { get; set; } = "JustStarting";
}

/// <summary>
/// Strictly restricted payload sent to POST /api/presence.
/// Only stateCode, ageBand, and anonId are ever serialized.
/// NO profile ID, NO child name, NO GPS, NO IP, NO cookies.
/// </summary>
public class IndiaPresencePayload
{
    [JsonPropertyName("stateCode")]
    public string StateCode { get; set; } = string.Empty;

    [JsonPropertyName("ageBand")]
    public string AgeBand { get; set; } = string.Empty;

    [JsonPropertyName("anonId")]
    public string AnonId { get; set; } = string.Empty;
}

/// <summary>
/// Local persistent consent preferences managed exclusively by parents behind the parent gate.
/// </summary>
public class IndiaPresenceConsent
{
    /// <summary>Whether parent has opted in to counting the child on the map. Defaults to FALSE.</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>Selected 2-letter state code (e.g. "PB", "MH", "KA").</summary>
    public string? StateCode { get; set; }

    /// <summary>Child's developmental age band ("3-5", "6-8", "9-12", "13+").</summary>
    public string? AgeBand { get; set; }

    /// <summary>
    /// Random anonymous install ID (UUID) generated once locally.
    /// Completely unlinked from any user profile, school, or personal identity.
    /// </summary>
    public string AnonymousInstallId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Whether child has waved to India to earn the special sticker.</summary>
    public bool HasWaved { get; set; } = false;

    /// <summary>Last time presence was synced to the API (at most once every 24 hours).</summary>
    public DateTime? LastSyncedUtc { get; set; }
}

/// <summary>
/// Official registry of all 28 States and 8 Union Territories of India
/// with simplified, open-licensed SVG geometry for the picture-book map.
/// </summary>
public static class IndiaStateRegistry
{
    public static readonly Dictionary<string, IndiaStateDefinition> States = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JK"] = new() { Code = "JK", Name = "Jammu & Kashmir", Capital = "Srinagar/Jammu", IsUnionTerritory = true, LabelX = 330, LabelY = 180, SvgPath = "M 280 120 L 330 110 L 370 140 L 380 190 L 350 230 L 300 230 L 260 190 Z" },
        ["LA"] = new() { Code = "LA", Name = "Ladakh", Capital = "Leh", IsUnionTerritory = true, LabelX = 440, LabelY = 130, SvgPath = "M 370 140 L 450 70 L 520 120 L 490 200 L 420 220 L 380 190 Z" },
        ["HP"] = new() { Code = "HP", Name = "Himachal Pradesh", Capital = "Shimla", LabelX = 370, LabelY = 240, SvgPath = "M 350 230 L 420 220 L 430 260 L 390 290 L 340 260 Z" },
        ["PB"] = new() { Code = "PB", Name = "Punjab", Capital = "Chandigarh", LabelX = 300, LabelY = 260, SvgPath = "M 270 240 L 340 240 L 330 290 L 280 290 Z" },
        ["CH"] = new() { Code = "CH", Name = "Chandigarh", Capital = "Chandigarh", IsUnionTerritory = true, LabelX = 345, LabelY = 265, SvgPath = "M 335 260 L 350 260 L 350 275 L 335 275 Z" },
        ["UT"] = new() { Code = "UT", Name = "Uttarakhand", Capital = "Dehradun", LabelX = 425, LabelY = 280, SvgPath = "M 390 260 L 450 250 L 470 300 L 415 320 Z" },
        ["HR"] = new() { Code = "HR", Name = "Haryana", Capital = "Chandigarh", LabelX = 330, LabelY = 310, SvgPath = "M 300 285 L 355 285 L 360 340 L 310 335 Z" },
        ["DL"] = new() { Code = "DL", Name = "Delhi", Capital = "New Delhi", IsUnionTerritory = true, LabelX = 355, LabelY = 320, SvgPath = "M 348 312 L 362 312 L 362 326 L 348 326 Z" },
        ["RJ"] = new() { Code = "RJ", Name = "Rajasthan", Capital = "Jaipur", LabelX = 240, LabelY = 370, SvgPath = "M 230 280 L 310 300 L 320 400 L 260 460 L 170 410 L 170 340 Z" },
        ["UP"] = new() { Code = "UP", Name = "Uttar Pradesh", Capital = "Lucknow", LabelX = 450, LabelY = 370, SvgPath = "M 360 320 L 470 300 L 580 370 L 530 450 L 430 430 L 370 370 Z" },
        ["BR"] = new() { Code = "BR", Name = "Bihar", Capital = "Patna", LabelX = 600, LabelY = 400, SvgPath = "M 560 370 L 670 380 L 660 450 L 560 440 Z" },
        ["SK"] = new() { Code = "SK", Name = "Sikkim", Capital = "Gangtok", LabelX = 690, LabelY = 345, SvgPath = "M 675 330 L 705 330 L 705 365 L 675 365 Z" },
        ["WB"] = new() { Code = "WB", Name = "West Bengal", Capital = "Kolkata", LabelX = 680, LabelY = 470, SvgPath = "M 670 370 L 705 370 L 690 450 L 720 520 L 670 550 L 650 460 Z" },
        ["JH"] = new() { Code = "JH", Name = "Jharkhand", Capital = "Ranchi", LabelX = 590, LabelY = 475, SvgPath = "M 560 440 L 650 450 L 640 520 L 560 510 Z" },
        ["OD"] = new() { Code = "OD", Name = "Odisha", Capital = "Bhubaneswar", LabelX = 590, LabelY = 560, SvgPath = "M 550 510 L 640 520 L 630 620 L 540 610 Z" },
        ["MP"] = new() { Code = "MP", Name = "Madhya Pradesh", Capital = "Bhopal", LabelX = 380, LabelY = 470, SvgPath = "M 300 420 L 430 420 L 530 460 L 490 540 L 340 540 L 290 470 Z" },
        ["GJ"] = new() { Code = "GJ", Name = "Gujarat", Capital = "Gandhinagar", LabelX = 170, LabelY = 490, SvgPath = "M 130 440 L 240 450 L 250 540 L 190 570 L 100 520 Z" },
        ["MH"] = new() { Code = "MH", Name = "Maharashtra", Capital = "Mumbai", LabelX = 300, LabelY = 600, SvgPath = "M 220 550 L 350 540 L 460 560 L 420 670 L 280 660 L 220 590 Z" },
        ["CT"] = new() { Code = "CT", Name = "Chhattisgarh", Capital = "Raipur", LabelX = 490, LabelY = 530, SvgPath = "M 480 470 L 540 490 L 530 630 L 470 610 Z" },
        ["TG"] = new() { Code = "TG", Name = "Telangana", Capital = "Hyderabad", LabelX = 400, LabelY = 670, SvgPath = "M 380 620 L 470 620 L 450 710 L 370 690 Z" },
        ["AP"] = new() { Code = "AP", Name = "Andhra Pradesh", Capital = "Amaravati", LabelX = 430, LabelY = 750, SvgPath = "M 450 630 L 540 620 L 480 810 L 410 800 L 440 710 Z" },
        ["KA"] = new() { Code = "KA", Name = "Karnataka", Capital = "Bengaluru", LabelX = 300, LabelY = 740, SvgPath = "M 270 660 L 370 670 L 380 800 L 300 840 L 260 730 Z" },
        ["GA"] = new() { Code = "GA", Name = "Goa", Capital = "Panaji", LabelX = 250, LabelY = 715, SvgPath = "M 245 705 L 265 705 L 260 730 L 240 725 Z" },
        ["KL"] = new() { Code = "KL", Name = "Kerala", Capital = "Thiruvananthapuram", LabelX = 310, LabelY = 890, SvgPath = "M 290 830 L 330 830 L 320 950 L 285 910 Z" },
        ["TN"] = new() { Code = "TN", Name = "Tamil Nadu", Capital = "Chennai", LabelX = 370, LabelY = 870, SvgPath = "M 330 800 L 420 800 L 400 940 L 325 930 Z" },
        ["PY"] = new() { Code = "PY", Name = "Puducherry", Capital = "Puducherry", IsUnionTerritory = true, LabelX = 425, LabelY = 825, SvgPath = "M 418 820 L 432 820 L 432 834 L 418 834 Z" },
        ["AS"] = new() { Code = "AS", Name = "Assam", Capital = "Dispur", LabelX = 770, LabelY = 390, SvgPath = "M 715 365 L 810 350 L 830 400 L 740 430 Z" },
        ["AR"] = new() { Code = "AR", Name = "Arunachal Pradesh", Capital = "Itanagar", LabelX = 840, LabelY = 320, SvgPath = "M 770 320 L 880 300 L 910 360 L 820 360 Z" },
        ["NL"] = new() { Code = "NL", Name = "Nagaland", Capital = "Kohima", LabelX = 855, LabelY = 400, SvgPath = "M 830 380 L 870 380 L 860 430 L 825 420 Z" },
        ["MN"] = new() { Code = "MN", Name = "Manipur", Capital = "Imphal", LabelX = 840, LabelY = 450, SvgPath = "M 825 430 L 865 430 L 855 480 L 820 475 Z" },
        ["MZ"] = new() { Code = "MZ", Name = "Mizoram", Capital = "Aizawl", LabelX = 810, LabelY = 505, SvgPath = "M 800 480 L 835 480 L 825 545 L 795 530 Z" },
        ["TR"] = new() { Code = "TR", Name = "Tripura", Capital = "Agartala", LabelX = 770, LabelY = 480, SvgPath = "M 755 460 L 785 460 L 780 505 L 750 495 Z" },
        ["ML"] = new() { Code = "ML", Name = "Meghalaya", Capital = "Shillong", LabelX = 745, LabelY = 420, SvgPath = "M 715 410 L 780 410 L 775 440 L 715 435 Z" },
        ["AN"] = new() { Code = "AN", Name = "Andaman & Nicobar", Capital = "Port Blair", IsUnionTerritory = true, LabelX = 780, LabelY = 780, SvgPath = "M 770 730 L 790 730 L 790 850 L 770 850 Z" },
        ["LD"] = new() { Code = "LD", Name = "Lakshadweep", Capital = "Kavaratti", IsUnionTerritory = true, LabelX = 220, LabelY = 860, SvgPath = "M 210 840 L 230 840 L 230 890 L 210 890 Z" },
        ["DH"] = new() { Code = "DH", Name = "Daman & Diu and DNH", Capital = "Daman", IsUnionTerritory = true, LabelX = 210, LabelY = 560, SvgPath = "M 205 550 L 220 550 L 220 565 L 205 565 Z" }
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
