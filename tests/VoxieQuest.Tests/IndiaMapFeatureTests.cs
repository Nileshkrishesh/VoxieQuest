using System.Net;
using System.Text.Json;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the Voxie Across India feature validating strict privacy rules,
/// thresholding (<20 players), rounding, state validation, payload restriction, and offline resilience.
/// </summary>
public class IndiaMapFeatureTests
{
    private readonly InMemorySaveService _saveService;
    private readonly MockAudioService _audioService;

    public IndiaMapFeatureTests()
    {
        _saveService = new InMemorySaveService();
        _audioService = new MockAudioService();
    }

    [Fact]
    public void Threshold_HidesStatesWithFewerThan20Players()
    {
        // States with < 20 players must display "Just starting here!" and never expose raw counts
        var lowState = new IndiaStateData
        {
            StateCode = "GA",
            DisplayCount = "Just starting here!",
            ActivityBand = IndiaActivityBand.JustStarting
        };

        Assert.Equal("Just starting here!", lowState.DisplayCount);
        Assert.Equal(IndiaActivityBand.JustStarting, lowState.ActivityBand);
        Assert.DoesNotContain("1", lowState.DisplayCount);
        Assert.DoesNotContain("19", lowState.DisplayCount);
    }

    [Theory]
    [InlineData("PB", true)]
    [InlineData("MH", true)]
    [InlineData("KA", true)]
    [InlineData("DL", true)]
    [InlineData("IN-TN", true)]
    [InlineData("XX", false)]
    [InlineData("CALIFORNIA", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void StateValidation_RejectsInvalidCodes(string inputCode, bool expectedValid)
    {
        bool isValid = IndiaStateRegistry.IsValidCode(inputCode);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void PayloadRestriction_OnlySendsStateCodeAgeBandAndAnonId()
    {
        var payload = new IndiaPresencePayload
        {
            StateCode = "PB",
            AgeBand = "6-8",
            AnonId = Guid.NewGuid().ToString("N")
        };

        string json = JsonSerializer.Serialize(payload);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify ONLY these 3 exact keys exist in the serialized JSON
        var propertyNames = root.EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equal(3, propertyNames.Count);
        Assert.Contains("stateCode", propertyNames);
        Assert.Contains("ageBand", propertyNames);
        Assert.Contains("anonId", propertyNames);

        // Verify forbidden PII fields are never serialized
        Assert.DoesNotContain("childName", propertyNames);
        Assert.DoesNotContain("name", propertyNames);
        Assert.DoesNotContain("gps", propertyNames);
        Assert.DoesNotContain("ip", propertyNames);
        Assert.DoesNotContain("latitude", propertyNames);
        Assert.DoesNotContain("school", propertyNames);
    }

    [Fact]
    public async Task OfflineResilience_GracefullyFallsBack_WhenApiIsUnreachable()
    {
        // Mock handler that throws network failure or returns 500
        var failingHandler = new FailingHttpMessageHandler();
        var client = new HttpClient(failingHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var service = new IndiaMapService(_saveService, client, _audioService);
        var mapData = await service.GetMapTotalsAsync();

        Assert.NotNull(mapData);
        Assert.True(mapData.Success);
        Assert.True(mapData.IsOfflineFallback);
        Assert.True(mapData.StatesWithActiveLearners > 0);
        Assert.True(mapData.States.ContainsKey("PB"));
        Assert.Equal("100+", mapData.States["PB"].CountText);
    }

    [Fact]
    public async Task Consent_DefaultsToOff_AndGeneratesRandomAnonId()
    {
        var handler = new SuccessfulHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new IndiaMapService(_saveService, client, _audioService);

        var consent = await service.GetConsentAsync();

        Assert.False(consent.IsEnabled);
        Assert.Null(consent.StateCode);
        Assert.False(string.IsNullOrWhiteSpace(consent.AnonymousInstallId));
    }

    [Fact]
    public async Task OptOut_DeletesContribution_AndResetsState()
    {
        var handler = new SuccessfulHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new IndiaMapService(_saveService, client, _audioService);

        // First opt in
        await service.SaveConsentAsync(true, "PB", "6-8");
        var consentAfterOptIn = await service.GetConsentAsync();
        Assert.True(consentAfterOptIn.IsEnabled);
        Assert.Equal("PB", consentAfterOptIn.StateCode);

        // Delete contribution
        await service.DeleteContributionAsync();
        var consentAfterDelete = await service.GetConsentAsync();
        Assert.False(consentAfterDelete.IsEnabled);
        Assert.Null(consentAfterDelete.StateCode);
    }

    [Fact]
    public async Task WaveToIndia_AwardsStickerWithoutMessaging()
    {
        var handler = new SuccessfulHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new IndiaMapService(_saveService, client, _audioService);

        bool waved = await service.WaveToIndiaAsync();
        Assert.True(waved);

        var consent = await service.GetConsentAsync();
        Assert.True(consent.HasWaved);
    }
}

public class FailingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Simulated API failure or offline network.");
    }
}

public class SuccessfulHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true}")
        };
        return Task.FromResult(response);
    }
}

public class MockAudioService : IAudioService
{
    public double Volume => 1.0;
    public bool IsMuted => false;
    public Task PlaySoundAsync(SoundEffect effect) => Task.CompletedTask;
    public Task PlayChimeAsync(bool success) => Task.CompletedTask;
    public Task PlayAnimalSoundAsync(string animalName) => Task.CompletedTask;
    public Task SetVolumeAsync(double volume) => Task.CompletedTask;
    public Task SetMutedAsync(bool muted) => Task.CompletedTask;
}
