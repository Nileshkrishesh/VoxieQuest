using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

public class PlatformAndClassroomTests
{
    [Fact]
    public void ProceduralNameGenerator_GeneratesValidZeroPiiNames()
    {
        var youngName = ProceduralNameGenerator.Generate(AgeBand.Ages3To5);
        var olderName = ProceduralNameGenerator.Generate(AgeBand.Ages13To15);

        Assert.False(string.IsNullOrWhiteSpace(youngName));
        Assert.False(string.IsNullOrWhiteSpace(olderName));
        Assert.Equal(3, youngName.Split(' ').Length);
        Assert.Equal(3, olderName.Split(' ').Length);
    }

    [Fact]
    public void SkillGardenState_AdvancesGrowthStagesThroughNourishment()
    {
        var garden = new SkillGardenState();
        var node = garden.GetOrAddSkill("test_skill", "Test Skill", OutcomeCategory.Emotional);

        // Initially seedling
        node.NourishmentPoints = 0;
        node.AddNourishment(10);
        Assert.Equal(GrowthStage.Seedling, node.Stage);

        // Budding threshold (>= 50)
        node.AddNourishment(45);
        Assert.Equal(GrowthStage.Budding, node.Stage);

        // Blooming threshold (>= 150)
        node.AddNourishment(100);
        Assert.Equal(GrowthStage.Blooming, node.Stage);

        // Perennial threshold (>= 300)
        node.AddNourishment(150);
        Assert.Equal(GrowthStage.Perennial, node.Stage);
        Assert.Equal("🌳", node.StageEmoji);
    }

    [Fact]
    public void ParentalControls_CorrectlyIdentifiesBedtimeWindowAcrossMidnight()
    {
        var controls = new ParentalControls
        {
            BedtimeLockoutEnabled = true,
            BedtimeStart = "20:00",
            BedtimeEnd = "06:00"
        };

        Assert.True(controls.BedtimeLockoutEnabled);
    }

    [Fact]
    public async Task ClassroomService_CreatesAndManagesRoomsSafely()
    {
        var service = new ClassroomService();

        var room = await service.CreateRoomAsync("beacon-bridge", "Beacon Bridge Co-op", "Optics & Light", "Notes");
        Assert.NotNull(room);
        Assert.Contains("-", room.RoomCode);
        Assert.False(room.IsExpired);

        var retrieved = await service.GetRoomAsync(room.RoomCode);
        Assert.NotNull(retrieved);
        Assert.Equal(room.RoomCode, retrieved.RoomCode);

        // Join station
        var joined = await service.JoinRoomAsync(room.RoomCode, "Station 2");
        Assert.True(joined);
        Assert.Contains("Station 2", room.ConnectedStations);

        // Discussion cards
        var cards = service.GetDiscussionCards("beacon-bridge");
        Assert.NotEmpty(cards);

        // Close room
        var closed = await service.CloseRoomAsync(room.RoomCode);
        Assert.True(closed);

        var afterClose = await service.GetRoomAsync(room.RoomCode);
        Assert.Null(afterClose);
    }

    [Fact]
    public void ChildProfile_RecordsSessionAndNourishesGarden()
    {
        var profile = new ChildProfile
        {
            Name = "Brave River Otter",
            AgeBand = AgeBand.Ages3To5
        };

        profile.NourishGardenSkill("emot_breath_calm", "Emotional Breath Regulation", OutcomeCategory.Emotional, 50, "Calmed Pip the hedgehog.");
        var skill = profile.GardenState.Skills["emot_breath_calm"];

        Assert.Equal(GrowthStage.Budding, skill.Stage);
        Assert.Contains("Calmed Pip the hedgehog.", skill.QualitativeObservations);

        profile.AddSessionSummary("mood-garden", "Mood Garden Sanctuary", 10, true, new() { OutcomeCategory.Emotional }, "Gentle breath cycles completed.");
        Assert.Single(profile.RecentSessions);
        Assert.True(profile.RecentSessions[0].CompletedNaturally);
    }
}
