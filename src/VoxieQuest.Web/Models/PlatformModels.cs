using System.Text.Json.Serialization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// Developmental age bands tailored for neurological, motor, and cognitive capabilities.
/// </summary>
public enum AgeBand
{
    /// <summary>Ages 3-5: Tap, drag, voice; no reading required; sessions 5-8 minutes.</summary>
    Ages3To5 = 0,

    /// <summary>Ages 6-8: Simple reading; short goals; sessions 10-15 minutes.</summary>
    Ages6To8 = 1,

    /// <summary>Ages 9-12: Strategy, stories, light collaboration; sessions 15-25 minutes.</summary>
    Ages9To12 = 2,

    /// <summary>Ages 13-15: Systems thinking, collaboration, creation; sessions 20-30 minutes.</summary>
    Ages13To15 = 3,

    /// <summary>Ages 16-18: Real-world simulations, portfolio mastery; sessions 25-40 minutes.</summary>
    Ages16To18 = 4
}

/// <summary>
/// The 6 foundational developmental outcome areas of the platform.
/// </summary>
public enum OutcomeCategory
{
    Cognitive = 0,
    Academic = 1,
    Social = 2,
    Emotional = 3,
    Creativity = 4,
    LifeSkills = 5
}

/// <summary>
/// Growth stages in the living Skill Garden (skills over scores).
/// </summary>
public enum GrowthStage
{
    /// <summary>First encounter / discovery.</summary>
    Seedling = 0,

    /// <summary>Active exploration and variable testing.</summary>
    Budding = 1,

    /// <summary>Fluent mastery and confident application.</summary>
    Blooming = 2,

    /// <summary>Cross-domain transfer and synthesis.</summary>
    Perennial = 3
}

/// <summary>
/// Represents a living skill branch in the botanical progress model.
/// </summary>
public class SkillNode
{
    public string SkillId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OutcomeCategory Category { get; set; } = OutcomeCategory.Academic;
    public GrowthStage Stage { get; set; } = GrowthStage.Seedling;
    public int NourishmentPoints { get; set; } = 0;
    public int ChallengesCompleted { get; set; } = 0;
    public DateTime LastNourishedUtc { get; set; } = DateTime.UtcNow;
    public List<string> QualitativeObservations { get; set; } = new();

    /// <summary>
    /// Gets the icon associated with the growth stage.
    /// </summary>
    [JsonIgnore]
    public string StageEmoji => Stage switch
    {
        GrowthStage.Seedling => "🌱",
        GrowthStage.Budding => "🌿",
        GrowthStage.Blooming => "🌸",
        GrowthStage.Perennial => "🌳",
        _ => "🌱"
    };

    /// <summary>
    /// Advances growth stage based on nourishment and effort.
    /// </summary>
    public void AddNourishment(int points, string? observation = null)
    {
        NourishmentPoints += Math.Max(1, points);
        ChallengesCompleted++;
        LastNourishedUtc = DateTime.UtcNow;

        if (observation != null && !QualitativeObservations.Contains(observation))
        {
            QualitativeObservations.Add(observation);
            if (QualitativeObservations.Count > 5)
            {
                QualitativeObservations.RemoveAt(0);
            }
        }

        Stage = NourishmentPoints switch
        {
            >= 300 => GrowthStage.Perennial,
            >= 150 => GrowthStage.Blooming,
            >= 50 => GrowthStage.Budding,
            _ => GrowthStage.Seedling
        };
    }
}

/// <summary>
/// Comprehensive Skill Garden state tracking across all 6 outcome categories.
/// </summary>
public class SkillGardenState
{
    public Dictionary<string, SkillNode> Skills { get; set; } = new();

    public SkillGardenState()
    {
        InitializeDefaultSkills();
    }

    private void InitializeDefaultSkills()
    {
        AddDefaultSkill("cog_memory_recall", "Memory & Acoustic Recall", OutcomeCategory.Cognitive);
        AddDefaultSkill("acad_phonics_letters", "Phonics & Letter Sounds", OutcomeCategory.Academic);
        AddDefaultSkill("acad_math_balance", "Maths & Lever Balance", OutcomeCategory.Academic);
        AddDefaultSkill("soc_coop_optics", "Teamwork & Shared Light", OutcomeCategory.Social);
        AddDefaultSkill("emot_breath_calm", "Emotional Breath Regulation", OutcomeCategory.Emotional);
        AddDefaultSkill("creat_fable_loom", "Storytelling & Motives", OutcomeCategory.Creativity);
        AddDefaultSkill("life_watershed_sim", "Watershed Flood Planning", OutcomeCategory.LifeSkills);
    }

    private void AddDefaultSkill(string id, string name, OutcomeCategory cat)
    {
        if (!Skills.ContainsKey(id))
        {
            Skills[id] = new SkillNode
            {
                SkillId = id,
                DisplayName = name,
                Category = cat,
                Stage = GrowthStage.Seedling,
                NourishmentPoints = 15
            };
        }
    }

    public SkillNode GetOrAddSkill(string skillId, string displayName, OutcomeCategory category)
    {
        if (!Skills.TryGetValue(skillId, out var node))
        {
            node = new SkillNode
            {
                SkillId = skillId,
                DisplayName = displayName,
                Category = category
            };
            Skills[skillId] = node;
        }
        return node;
    }

    [JsonIgnore]
    public int HealthyBloomingCount => Skills.Values.Count(s => s.Stage >= GrowthStage.Blooming);
}

/// <summary>
/// Summary of an individual play experience for parent reflection.
/// </summary>
public class PlaySessionSummary
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string GameId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public int DurationMinutes { get; set; } = 8;
    public bool CompletedNaturally { get; set; } = true;
    public List<OutcomeCategory> OutcomesTrained { get; set; } = new();
    public string NarrativeHighlight { get; set; } = string.Empty;
}

/// <summary>
/// Allowed co-play modes for child protection.
/// </summary>
public enum CoPlayMode
{
    Disabled = 0,
    RoomCodesOnly = 1,
    ApprovedPeersOnly = 2
}

/// <summary>
/// Parental safety and well-being constraints stored locally.
/// </summary>
public class ParentalControls
{
    public int DailyLimitMinutes { get; set; } = 30;
    public int SingleSessionMaxMinutes { get; set; } = 15;
    public bool BedtimeLockoutEnabled { get; set; } = true;
    public string BedtimeStart { get; set; } = "19:30";
    public string BedtimeEnd { get; set; } = "07:00";
    public CoPlayMode AllowedCoPlay { get; set; } = CoPlayMode.RoomCodesOnly;
    public List<string> ApprovedPeerRoomCodes { get; set; } = new() { "OAK-742", "RIVER-104" };

    [JsonIgnore]
    public bool IsCurrentlyBedtime
    {
        get
        {
            if (!BedtimeLockoutEnabled) return false;
            if (TimeOnly.TryParse(BedtimeStart, out var start) && TimeOnly.TryParse(BedtimeEnd, out var end))
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                if (start > end)
                {
                    // Spans midnight, e.g. 19:30 to 07:00
                    return now >= start || now < end;
                }
                return now >= start && now < end;
            }
            return false;
        }
    }
}

/// <summary>
/// Classroom session pod for zero-login group learning.
/// </summary>
public class ClassroomRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public string TargetGameId { get; set; } = "beacon-bridge";
    public string TargetGameTitle { get; set; } = "Beacon Bridge Co-op";
    public string ScenarioName { get; set; } = "Optics & Light Refraction";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(60);
    public List<string> ConnectedStations { get; set; } = new();
    public string TeacherNotes { get; set; } = "Pair students up to rotate prisms and align beacons.";

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}

/// <summary>
/// Procedural generator for zero-PII kid handles.
/// </summary>
public static class ProceduralNameGenerator
{
    private static readonly string[] AdjectivesYoung = { "Brave", "Curious", "Sunny", "Gentle", "Happy", "Kind", "Swift", "Cozy" };
    private static readonly string[] ElementsYoung = { "River", "Meadow", "Sun", "Cloud", "Forest", "Pebble", "Leaf", "Rainbow" };
    private static readonly string[] AnimalsYoung = { "Otter", "Badger", "Fox", "Bear", "Fawn", "Owl", "Bunny", "Hedgehog" };

    private static readonly string[] AdjectivesOlder = { "Stalwart", "Radiant", "Kinetic", "Agile", "Quantum", "Solar", "Cosmic", "Astral" };
    private static readonly string[] ElementsOlder = { "Nebula", "Summit", "Horizon", "Canyon", "Glacier", "Vortex", "Pinnacle", "Atlas" };
    private static readonly string[] AnimalsOlder = { "Falcon", "Osprey", "Lynx", "Wolf", "Bison", "Heron", "Condor", "Manta" };

    public static string Generate(AgeBand ageBand)
    {
        var rng = new Random();
        if (ageBand <= AgeBand.Ages6To8)
        {
            var adj = AdjectivesYoung[rng.Next(AdjectivesYoung.Length)];
            var elem = ElementsYoung[rng.Next(ElementsYoung.Length)];
            var animal = AnimalsYoung[rng.Next(AnimalsYoung.Length)];
            return $"{adj} {elem} {animal}";
        }
        else
        {
            var adj = AdjectivesOlder[rng.Next(AdjectivesOlder.Length)];
            var elem = ElementsOlder[rng.Next(ElementsOlder.Length)];
            var animal = AnimalsOlder[rng.Next(AnimalsOlder.Length)];
            return $"{adj} {elem} {animal}";
        }
    }
}
