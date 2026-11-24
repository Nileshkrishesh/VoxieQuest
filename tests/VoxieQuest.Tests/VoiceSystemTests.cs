using Microsoft.JSInterop;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the Voxie Quest voice capture pipeline, state machine,
/// VoiceManager resilience, and multi-alternative speech handling.
/// </summary>
public class VoiceSystemTests
{
    private class FakeJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (typeof(TValue) == typeof(bool))
            {
                return ValueTask.FromResult((TValue)(object)true);
            }
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    private readonly FakeJSRuntime _fakeJsRuntime;
    private readonly BrowserSpeechVoiceService _browserService;
    private readonly OfflineVoiceService _offlineService;
    private readonly VoiceManager _voiceManager;

    public VoiceSystemTests()
    {
        _fakeJsRuntime = new FakeJSRuntime();
        _browserService = new BrowserSpeechVoiceService(_fakeJsRuntime);
        _offlineService = new OfflineVoiceService(_fakeJsRuntime);
        _voiceManager = new VoiceManager(_browserService, _offlineService);
    }

    [Fact]
    public void MicState_InitialState_ShouldBeIdle()
    {
        Assert.Equal(MicState.Idle, _browserService.CurrentState);
        Assert.Equal(MicState.Idle, _voiceManager.CurrentState);
    }

    [Theory]
    [InlineData("requesting_permission", MicState.RequestingPermission)]
    [InlineData("listening", MicState.Listening)]
    [InlineData("processing", MicState.Processing)]
    [InlineData("success", MicState.Success)]
    [InlineData("no_speech", MicState.NoSpeech)]
    [InlineData("permission_denied", MicState.PermissionDenied)]
    [InlineData("unsupported", MicState.Unsupported)]
    [InlineData("error", MicState.Error)]
    [InlineData("idle", MicState.Idle)]
    public void OnJsVoiceStateChanged_ShouldUpdateCurrentStateAndTriggerEvent(string jsState, MicState expectedState)
    {
        MicState? receivedState = null;
        _browserService.OnStateChanged += s => receivedState = s;

        _browserService.OnJsVoiceStateChanged(jsState);

        Assert.Equal(expectedState, _browserService.CurrentState);
        Assert.Equal(expectedState, receivedState);
    }

    [Fact]
    public void VoiceManager_StateChangedEvent_ShouldBubbleUpFromActiveService()
    {
        MicState? bubbledState = null;
        _voiceManager.OnStateChanged += s => bubbledState = s;

        _browserService.OnJsVoiceStateChanged("listening");

        Assert.Equal(MicState.Listening, _voiceManager.CurrentState);
        Assert.Equal(MicState.Listening, bubbledState);
    }

    [Fact]
    public void VoiceManager_WhenInPracticeMode_CurrentStateShouldBeIdle()
    {
        _voiceManager.SetEngine(VoiceEngineType.PracticeManual);

        Assert.Equal(MicState.Idle, _voiceManager.CurrentState);
    }

    [Fact]
    public async Task VoiceManager_StartListening_WhenInPracticeMode_ShouldSwitchBackToBrowserSpeech()
    {
        _voiceManager.SetEngine(VoiceEngineType.PracticeManual);
        Assert.Equal(VoiceEngineType.PracticeManual, _voiceManager.CurrentEngine);

        // When user asks to listen, it should automatically reactivate the browser speech engine
        await _voiceManager.StartListeningAsync();

        Assert.Equal(VoiceEngineType.BrowserWebSpeech, _voiceManager.CurrentEngine);
    }

    [Fact]
    public void VoiceManager_OnError_ShouldNotTrapEngineInPracticeMode()
    {
        // Transient permission rejection should NOT switch the engine permanently
        _browserService.OnJsVoiceStateChanged("permission_denied");

        Assert.Equal(VoiceEngineType.BrowserWebSpeech, _voiceManager.CurrentEngine);
        Assert.Equal(MicState.PermissionDenied, _voiceManager.CurrentState);
    }

    [Fact]
    public void OnJsVolumeChanged_ShouldUpdateVolumeAndFireEvent()
    {
        float reportedVol = 0.0f;
        _voiceManager.OnVolumeChanged += v => reportedVol = v;

        _browserService.OnJsVolumeChanged(0.65f);

        Assert.Equal(0.65f, _voiceManager.CurrentVolume, 2);
        Assert.Equal(0.65f, reportedVol, 2);
    }

    [Fact]
    public void OnJsSpeechResultWithAlternatives_ShouldParseAndDeliverResult()
    {
        SpeechResult? received = null;
        _voiceManager.OnResult += r => received = r;

        _browserService.OnJsSpeechResultWithAlternatives("see", "see|C|sea", 0.95, true);

        Assert.NotNull(received);
        Assert.Equal("see", received.Transcript);
        Assert.True(received.IsFinal);
        Assert.Contains("see", received.Alternatives);
        Assert.Contains("C", received.Alternatives);
        Assert.Contains("sea", received.Alternatives);
    }

    [Fact]
    public void SpeechMatcher_WithMultiAlternatives_ShouldMatchIfAnyCandidateIsCorrect()
    {
        var matcher = new SpeechMatcher();
        var candidates = new List<string> { "sea", "see", "C" };
        var question = new QuestionItem
        {
            TargetAnswer = "C",
            Prompt = "Say the letter C"
        };

        var match = matcher.Evaluate(candidates, question);

        Assert.True(match.IsMatch);
        Assert.Equal("C", match.MatchedTarget, ignoreCase: true);
    }

    [Fact]
    public void QuestionAttemptState_ScaffoldingFlow_ShouldBeChildFriendly()
    {
        var attempt = new QuestionAttemptState();
        Assert.Equal(1, attempt.AttemptCount);
        Assert.False(attempt.ShowHint);
        Assert.False(attempt.IsExhausted);

        // 1st miss: encouraging nudge without revealing hint
        attempt.RecordMiss("frog", "It hops in ponds!");
        Assert.Equal(2, attempt.AttemptCount);
        Assert.False(attempt.ShowHint);
        Assert.False(attempt.IsExhausted);
        Assert.Contains("Almost", attempt.FeedbackMessage);

        // 2nd miss: reveals hint
        attempt.RecordMiss("frog", "It hops in ponds!");
        Assert.Equal(3, attempt.AttemptCount);
        Assert.True(attempt.ShowHint);
        Assert.False(attempt.IsExhausted);
        Assert.Contains("It hops in ponds!", attempt.FeedbackMessage);

        // 3rd miss: compassionate resolution, never says wrong
        attempt.RecordMiss("frog", "It hops in ponds!");
        Assert.Equal(4, attempt.AttemptCount);
        Assert.True(attempt.IsExhausted);
        Assert.DoesNotContain("wrong", attempt.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frog", attempt.FeedbackMessage);
    }
}
