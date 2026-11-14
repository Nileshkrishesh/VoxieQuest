using Microsoft.AspNetCore.Components;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;

namespace VoxieQuest.Web.Components;

/// <summary>
/// Abstract base component providing unified round logic (10 rounds, 3 difficulty tiers,
/// 3-attempt scaffolding, scoring, minimal text with spoken prompts, and reward flow)
/// across all Voxie Quest mini-games.
/// </summary>
public abstract class BaseGameComponent : ComponentBase, IAsyncDisposable
{
    /// <summary>Total rounds per standard mini-game play session.</summary>
    public const int TotalRounds = 10;

    [Inject] protected VoiceManager VoiceManager { get; set; } = default!;
    [Inject] protected ISpeechMatcher SpeechMatcher { get; set; } = default!;
    [Inject] protected IAudioService AudioService { get; set; } = default!;
    [Inject] protected IProgressService ProgressService { get; set; } = default!;
    [Inject] protected IAdaptiveDifficultyService DifficultyService { get; set; } = default!;
    [Inject] protected IQuestionService QuestionService { get; set; } = default!;
    [Inject] protected IStreakService StreakService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets the current round number (1 to 10).</summary>
    protected int CurrentRound { get; private set; } = 1;

    /// <summary>Gets total stars accumulated in this 10-round game session.</summary>
    protected int SessionStars { get; private set; } = 0;

    /// <summary>Gets the current consecutive correct streak in this game session.</summary>
    protected int SessionStreak { get; private set; } = 0;

    /// <summary>Gets the active difficulty level (Easy, Medium, Hard).</summary>
    protected DifficultyLevel Difficulty => DifficultyService.CurrentDifficulty;

    /// <summary>Gets the attempt state (up to 3 attempts, hint after 2nd miss).</summary>
    protected QuestionAttemptState AttemptState { get; } = new();

    /// <summary>Gets the visual expressive mood for Voxie the Fox.</summary>
    protected Mascot.MascotState MascotState { get; set; } = Mascot.MascotState.Idle;

    /// <summary>Gets whether the 10-round completion reward modal is visible.</summary>
    protected bool IsRewardScreenOpen { get; private set; } = false;

    /// <summary>Gets whether a celebratory star particle burst is currently radiating.</summary>
    protected bool ShowStarBurst { get; set; } = false;

    /// <summary>Gets the current active challenge item.</summary>
    protected GameChallenge? ActiveChallenge { get; private set; }

    /// <summary>Gets the real-time microphone volume level (0.0 to 1.0).</summary>
    protected float CurrentVolume { get; private set; } = 0.0f;

    /// <summary>Gets the most recent raw transcript from the child.</summary>
    protected string LastTranscript { get; set; } = string.Empty;

    /// <summary>Gets whether the most recent answer was judged correct.</summary>
    protected bool LastAnswerSuccess { get; set; } = false;

    /// <summary>Gets whether speech or action animation is actively evaluating.</summary>
    protected bool IsBusyAnimating { get; set; } = false;

    /// <summary>Gets the skill category represented by this mini-game.</summary>
    protected abstract SkillCategory GameCategory { get; }

    /// <summary>Gets the child-friendly title of this mini-game.</summary>
    protected abstract string GameTitle { get; }

    /// <summary>Generates or retrieves a challenge appropriate for the given round and difficulty level.</summary>
    protected abstract Task<GameChallenge> GenerateChallengeAsync(int round, DifficultyLevel difficulty);

    /// <summary>Hook executed when the child provides a correct answer, before advancing to next round.</summary>
    protected virtual Task OnChallengeSolvedAsync(GameChallenge challenge) => Task.CompletedTask;

    /// <summary>Hook executed when an attempt is missed.</summary>
    protected virtual Task OnAttemptMissedAsync(GameChallenge challenge, int attemptCount) => Task.CompletedTask;

    protected override async Task OnInitializedAsync()
    {
        VoiceManager.OnResult += HandleVoiceResult;
        VoiceManager.OnVolumeChanged += HandleVolumeChanged;
        VoiceManager.OnError += HandleVoiceError;

        await StartGameSessionAsync();
    }

    /// <summary>
    /// Resets scores and begins round 1 of 10.
    /// </summary>
    protected async Task StartGameSessionAsync()
    {
        CurrentRound = 1;
        SessionStars = 0;
        SessionStreak = 0;
        IsRewardScreenOpen = false;
        LastTranscript = string.Empty;
        LastAnswerSuccess = false;

        await LoadRoundAsync();
    }

    /// <summary>
    /// Loads challenge data for the active round and speaks instructions via Voxie.
    /// </summary>
    protected async Task LoadRoundAsync()
    {
        IsBusyAnimating = false;
        LastTranscript = string.Empty;
        LastAnswerSuccess = false;
        MascotState = Mascot.MascotState.Idle;
        AttemptState.Reset();

        ActiveChallenge = await GenerateChallengeAsync(CurrentRound, Difficulty);

        if (ActiveChallenge != null)
        {
            // Voxie vocalizes instruction clearly with minimal text
            await VoiceManager.SpeakAsync(ActiveChallenge.Prompt);

            // Re-arm microphone if in Auto-Listen mode
            if (VoiceManager.Mode == VoiceMode.AutoListen && VoiceManager.CurrentEngine != VoiceEngineType.PracticeManual)
            {
                await Task.Delay(1100);
                await VoiceManager.StartListeningAsync();
                MascotState = Mascot.MascotState.Listening;
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// Evaluates verbal transcripts received from speech recognition or manual practice buttons.
    /// </summary>
    protected async Task ProcessTranscriptAsync(string transcript, bool isFinal)
    {
        if (ActiveChallenge == null || IsBusyAnimating || string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        LastTranscript = transcript;
        MascotState = Mascot.MascotState.Thinking;
        StateHasChanged();

        var questionStub = new QuestionItem
        {
            TargetAnswer = ActiveChallenge.TargetAnswer,
            AcceptedAnswers = ActiveChallenge.AcceptedAnswers,
            Hint = ActiveChallenge.Hint
        };

        var match = SpeechMatcher.Evaluate(transcript, questionStub);

        if (match.IsMatch)
        {
            // SUCCESS
            IsBusyAnimating = true;
            LastAnswerSuccess = true;
            MascotState = Mascot.MascotState.Happy;
            ShowStarBurst = true;

            await VoiceManager.StopListeningAsync();
            await AudioService.PlaySoundAsync(SoundEffect.SuccessChime);

            var starsEarned = Math.Max(1, ActiveChallenge.RewardStars);
            SessionStars += starsEarned;
            SessionStreak++;

            await ProgressService.RecordChallengeAttemptAsync(GameCategory, true, starsEarned);
            DifficultyService.RecordAttempt(true);
            await StreakService.RecordRoundPlayedAsync(starsEarned);

            // Trigger game-specific visual animation (e.g. Rocket blasts off, Frog hops, Paint splashes)
            await OnChallengeSolvedAsync(ActiveChallenge);

            StateHasChanged();
            await Task.Delay(1800);

            ShowStarBurst = false;

            if (CurrentRound >= TotalRounds)
            {
                await FinishGameWithRewardAsync();
            }
            else
            {
                CurrentRound++;
                await LoadRoundAsync();
            }
        }
        else if (isFinal)
        {
            // MISS (1st, 2nd, or 3rd Attempt) - Never show "Wrong"!
            LastAnswerSuccess = false;
            SessionStreak = 0;
            AttemptState.RecordMiss(ActiveChallenge.TargetAnswer, ActiveChallenge.Hint);

            await AudioService.PlaySoundAsync(SoundEffect.EncourageChime);
            await ProgressService.RecordChallengeAttemptAsync(GameCategory, false);
            DifficultyService.RecordAttempt(false);

            await OnAttemptMissedAsync(ActiveChallenge, AttemptState.AttemptCount);

            if (AttemptState.IsExhausted)
            {
                // All 3 attempts exhausted: gentle supportive completion
                await StreakService.RecordRoundPlayedAsync(0);
                IsBusyAnimating = true;
                MascotState = Mascot.MascotState.Happy;
                await VoiceManager.SpeakAsync($"Great try! The answer is '{ActiveChallenge.TargetAnswer}'. Let's try the next one!");
                await Task.Delay(2000);

                if (CurrentRound >= TotalRounds)
                {
                    await FinishGameWithRewardAsync();
                }
                else
                {
                    CurrentRound++;
                    await LoadRoundAsync();
                }
            }
            else
            {
                // Sad then cheerful reassuring recovery
                MascotState = Mascot.MascotState.SadThenCheer;
                await VoiceManager.SpeakAsync(AttemptState.FeedbackMessage);

                if (VoiceManager.Mode == VoiceMode.AutoListen && VoiceManager.CurrentEngine != VoiceEngineType.PracticeManual)
                {
                    await Task.Delay(1200);
                    await VoiceManager.StartListeningAsync();
                    MascotState = Mascot.MascotState.Listening;
                }
            }

            StateHasChanged();
        }
    }

    /// <summary>
    /// Concludes the 10-round session and opens the celebration reward screen.
    /// </summary>
    protected async Task FinishGameWithRewardAsync()
    {
        IsRewardScreenOpen = true;
        MascotState = Mascot.MascotState.Happy;
        await VoiceManager.StopListeningAsync();
        await AudioService.PlaySoundAsync(SoundEffect.Fanfare);
        await VoiceManager.SpeakAsync($"Hooray! You finished all ten rounds and collected {SessionStars} stars!");
        StateHasChanged();
    }

    /// <summary>
    /// Toggles microphone listening state.
    /// </summary>
    protected async Task ToggleListeningAsync()
    {
        if (VoiceManager.IsListening)
        {
            await VoiceManager.StopListeningAsync();
            MascotState = Mascot.MascotState.Idle;
        }
        else
        {
            await AudioService.PlaySoundAsync(SoundEffect.Pop);
            MascotState = Mascot.MascotState.Listening;
            await VoiceManager.StartListeningAsync();
        }
    }

    /// <summary>
    /// Receives a selected or typed answer from Practice Mode on-screen buttons or keyboard.
    /// </summary>
    protected async Task SubmitManualAnswerAsync(string answer)
    {
        await ProcessTranscriptAsync(answer, isFinal: true);
    }

    /// <summary>
    /// Repeats the current challenge prompt aloud via Voxie.
    /// </summary>
    protected async Task RepeatPromptAloudAsync()
    {
        if (ActiveChallenge != null)
        {
            await AudioService.PlaySoundAsync(SoundEffect.Click);
            await VoiceManager.SpeakAsync(ActiveChallenge.Prompt);
        }
    }

    /// <summary>
    /// Skips to the next round without penalty.
    /// </summary>
    protected async Task SkipRoundAsync()
    {
        await AudioService.PlaySoundAsync(SoundEffect.Click);
        if (CurrentRound >= TotalRounds)
        {
            await FinishGameWithRewardAsync();
        }
        else
        {
            CurrentRound++;
            await LoadRoundAsync();
        }
    }

    /// <summary>
    /// Returns the child safely to the overworld map.
    /// </summary>
    protected async Task ReturnToMapAsync()
    {
        await AudioService.PlaySoundAsync(SoundEffect.Click);
        await VoiceManager.StopListeningAsync();
        Navigation.NavigateTo("games");
    }

    private void HandleVoiceResult(SpeechResult res)
    {
        if (!IsBusyAnimating && !string.IsNullOrWhiteSpace(res.Transcript))
        {
            InvokeAsync(() => ProcessTranscriptAsync(res.Transcript, res.IsFinal));
        }
    }

    private void HandleVolumeChanged(float vol)
    {
        CurrentVolume = vol;
        InvokeAsync(StateHasChanged);
    }

    private void HandleVoiceError(string err)
    {
        InvokeAsync(() =>
        {
            MascotState = Mascot.MascotState.Idle;
            StateHasChanged();
        });
    }

    public virtual async ValueTask DisposeAsync()
    {
        VoiceManager.OnResult -= HandleVoiceResult;
        VoiceManager.OnVolumeChanged -= HandleVolumeChanged;
        VoiceManager.OnError -= HandleVoiceError;
        await VoiceManager.StopListeningAsync();
    }
}
