using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// In-memory ephemeral classroom service. Stores zero PII and auto-expires rooms.
/// </summary>
public class ClassroomService : IClassroomService
{
    private readonly List<ClassroomRoom> _rooms = new();
    private static readonly string[] TreeNames = { "OAK", "MAPLE", "PINE", "CEDAR", "BIRCH", "WILLOW", "RIVER", "SUN" };
    private readonly Random _rng = new();

    public IReadOnlyList<ClassroomRoom> ActiveRooms
    {
        get
        {
            // Purge expired rooms on read
            _rooms.RemoveAll(r => r.IsExpired);
            return _rooms.AsReadOnly();
        }
    }

    public Task<ClassroomRoom> CreateRoomAsync(string targetGameId, string targetGameTitle, string scenarioName, string teacherNotes)
    {
        var code = GenerateMnemonicCode();
        var room = new ClassroomRoom
        {
            RoomCode = code,
            TargetGameId = targetGameId,
            TargetGameTitle = targetGameTitle,
            ScenarioName = scenarioName,
            TeacherNotes = teacherNotes,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
            ConnectedStations = new() { "Station 1 (Leader)" }
        };

        _rooms.Add(room);
        return Task.FromResult(room);
    }

    public Task<ClassroomRoom?> GetRoomAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return Task.FromResult<ClassroomRoom?>(null);

        var normalized = roomCode.Trim().ToUpperInvariant();
        var room = _rooms.FirstOrDefault(r => r.RoomCode == normalized && !r.IsExpired);
        return Task.FromResult(room);
    }

    public Task<bool> JoinRoomAsync(string roomCode, string stationName)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return Task.FromResult(false);

        var normalized = roomCode.Trim().ToUpperInvariant();
        var room = _rooms.FirstOrDefault(r => r.RoomCode == normalized && !r.IsExpired);
        if (room != null)
        {
            if (!room.ConnectedStations.Contains(stationName))
            {
                room.ConnectedStations.Add(stationName);
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> CloseRoomAsync(string roomCode)
    {
        var normalized = roomCode.Trim().ToUpperInvariant();
        var count = _rooms.RemoveAll(r => r.RoomCode == normalized);
        return Task.FromResult(count > 0);
    }

    public IReadOnlyList<string> GetDiscussionCards(string gameId)
    {
        return gameId.ToLowerInvariant() switch
        {
            "mood-garden" => new[]
            {
                "What did it feel like in your chest when the breathing ring was expanding vs. contracting?",
                "When a cloud is rumbling and loud, what helps you feel calmer at home or school?",
                "If a friend was feeling like a storm cloud today, how could you offer them gentle sunbeams?"
            },
            "tumble-scale" => new[]
            {
                "When you placed a small weight further away from the center, why did it tilt faster than a heavy weight close in?",
                "How did breaking a fraction into smaller pieces help you balance the archipelago?",
                "What was the most surprising balanced combination your team discovered?"
            },
            "beacon-bridge" => new[]
            {
                "How did rotating your mirror change where your partner's beam landed?",
                "What strategies did you and your partner use to agree on the next move without talking out loud?",
                "What happened when the light hit the splitter prism?"
            },
            "fable-weaver" => new[]
            {
                "When your character made a selfish choice, what happened to the villagers in the next chapter?",
                "Did resolving the conflict feel more satisfying when the character apologized or when they worked together?",
                "How can understanding someone's secret motive help us resolve real disagreements?"
            },
            "hydrocity" => new[]
            {
                "Why did asphalt roads cause more basement flooding than the grass bioswales?",
                "How do retention ponds protect downstream houses during cloudbursts?",
                "What kind of water-friendly parks would you build in our own neighborhood?"
            },
            _ => new[]
            {
                "What was the most challenging part of today's quest, and how did you work through it?",
                "What strategy helped you the most when an experiment gave an unexpected result?",
                "What can you teach someone else about how this game works?"
            }
        };
    }

    private string GenerateMnemonicCode()
    {
        string prefix = TreeNames[_rng.Next(TreeNames.Length)];
        int number = _rng.Next(100, 999);
        string code = $"{prefix}-{number}";
        // Ensure uniqueness
        while (_rooms.Any(r => r.RoomCode == code))
        {
            prefix = TreeNames[_rng.Next(TreeNames.Length)];
            number = _rng.Next(100, 999);
            code = $"{prefix}-{number}";
        }
        return code;
    }
}
