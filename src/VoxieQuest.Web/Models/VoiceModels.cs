namespace VoxieQuest.Web.Models;

/// <summary>
/// Operational hardware and logical state of the microphone and speech recognition pipeline.
/// </summary>
public enum MicState
{
    /// <summary>Waiting for user interaction.</summary>
    Idle = 0,

    /// <summary>Backwards-compatible alias for Idle.</summary>
    Ready = 0,

    /// <summary>Prompting browser/OS for microphone permission.</summary>
    RequestingPermission = 1,

    /// <summary>Microphone is actively capturing audio and listening.</summary>
    Listening = 2,

    /// <summary>Acoustic speech detected, actively converting speech into text.</summary>
    Processing = 3,

    /// <summary>Backwards-compatible alias for Processing.</summary>
    Thinking = 3,

    /// <summary>Spoken words were successfully recognized and matched.</summary>
    Success = 4,

    /// <summary>Backwards-compatible alias for Success.</summary>
    Done = 4,

    /// <summary>Microphone is active but no speech was detected after timeout.</summary>
    NoSpeech = 5,

    /// <summary>User has blocked or denied microphone access in the browser.</summary>
    PermissionDenied = 6,

    /// <summary>Browser does not support the speech recognition engine.</summary>
    Unsupported = 7,

    /// <summary>An actual microphone or recognition error occurred.</summary>
    Error = 8
}

/// <summary>
/// Operational mode for listening to the child's voice.
/// </summary>
public enum VoiceMode
{
    /// <summary>Microphone stays open automatically during questions.</summary>
    AutoListen,

    /// <summary>Child or parent holds/taps button to speak.</summary>
    PushToTalk,

    /// <summary>Microphone is bypassed in favor of large on-screen answer cards and keyboard.</summary>
    PracticeButtons
}

/// <summary>
/// Speech recognition backend engine currently providing voice transcription.
/// </summary>
public enum VoiceEngineType
{
    /// <summary>Native Web Speech API (SpeechRecognition / webkitSpeechRecognition).</summary>
    BrowserWebSpeech,

    /// <summary>Offline WebAssembly speech recognition engine (Vosk-browser).</summary>
    OfflineVosk,

    /// <summary>Manual practice mode using touch/click buttons and keyboard inputs.</summary>
    PracticeManual
}

/// <summary>
/// Detailed evaluation outcome produced by the pure C# SpeechMatcher.
/// </summary>
public class MatchEvaluation
{
    /// <summary>Gets whether the spoken text successfully matched the expected target.</summary>
    public bool IsMatch { get; set; }

    /// <summary>Gets the confidence rating between 0.0 and 1.0.</summary>
    public double Confidence { get; set; }

    /// <summary>Gets the matched target word or phrase.</summary>
    public string MatchedTarget { get; set; } = string.Empty;

    /// <summary>Gets the normalized, cleaned spoken text input.</summary>
    public string NormalizedSpoken { get; set; } = string.Empty;

    /// <summary>Gets the rule or algorithm that satisfied the match.</summary>
    public string MatchReason { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful match evaluation.
    /// </summary>
    public static MatchEvaluation Success(string target, string spoken, string reason, double confidence = 1.0) =>
        new()
        {
            IsMatch = true,
            MatchedTarget = target,
            NormalizedSpoken = spoken,
            MatchReason = reason,
            Confidence = confidence
        };

    /// <summary>
    /// Creates an unsuccessful match evaluation.
    /// </summary>
    public static MatchEvaluation Fail(string spoken, string reason = "NoMatch") =>
        new()
        {
            IsMatch = false,
            NormalizedSpoken = spoken,
            MatchReason = reason,
            Confidence = 0.0
        };
}

/// <summary>
/// Manages the 3-attempt lifecycle for a question.
/// Never displays 'Wrong', only encouraging scaffolding ('Almost! Try again!').
/// </summary>
public class QuestionAttemptState
{
    /// <summary>Maximum attempts allowed per question before scaffolding completion.</summary>
    public const int MaxAttempts = 3;

    /// <summary>Gets the current attempt number (1, 2, or 3).</summary>
    public int AttemptCount { get; private set; } = 1;

    /// <summary>Gets whether the hint should be displayed (after 2nd miss).</summary>
    public bool ShowHint { get; private set; } = false;

    /// <summary>Gets whether all 3 attempts have been exhausted without penalty.</summary>
    public bool IsExhausted => AttemptCount > MaxAttempts;

    /// <summary>Gets the encouraging feedback text shown to the child.</summary>
    public string FeedbackMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Resets attempt state for a newly presented question.
    /// </summary>
    public void Reset()
    {
        AttemptCount = 1;
        ShowHint = false;
        FeedbackMessage = string.Empty;
    }

    /// <summary>
    /// Records an attempt miss and updates scaffolding feedback.
    /// </summary>
    /// <param name="targetAnswer">The expected target word.</param>
    /// <param name="hint">Optional hint text.</param>
    public void RecordMiss(string targetAnswer, string hint)
    {
        if (AttemptCount == 1)
        {
            // 1st miss: encouraging nudge, no hint yet
            FeedbackMessage = "Almost! Try again!";
            ShowHint = false;
            AttemptCount = 2;
        }
        else if (AttemptCount == 2)
        {
            // 2nd miss: hint revealed
            FeedbackMessage = string.IsNullOrWhiteSpace(hint) 
                ? "Almost! Listen closely and try again!" 
                : $"Almost! Here is a clue: {hint}";
            ShowHint = true;
            AttemptCount = 3;
        }
        else
        {
            // 3rd miss: compassionate resolution, never say "Wrong"
            FeedbackMessage = $"Great try! Let's say '{targetAnswer}' together!";
            ShowHint = true;
            AttemptCount = 4; // marks exhausted
        }
    }
}
