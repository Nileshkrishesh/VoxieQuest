using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing the real-time game flow and loop states (Menu, Playing, Reward, Paused).
/// </summary>
public interface IGameStateService
{
    /// <summary>
    /// Gets the current state of the game loop.
    /// </summary>
    GameModeState CurrentState { get; }

    /// <summary>
    /// Gets the current active educational realm category.
    /// </summary>
    SkillCategory ActiveCategory { get; }

    /// <summary>
    /// Gets the question currently presented to the child.
    /// </summary>
    QuestionItem? CurrentQuestion { get; }

    /// <summary>
    /// Gets the number of stars/points accumulated in the current session.
    /// </summary>
    int SessionScore { get; }

    /// <summary>
    /// Gets the current consecutive correct streak in this session.
    /// </summary>
    int SessionStreak { get; }

    /// <summary>
    /// Event triggered when the game state transitions (Menu, Playing, Reward, Paused).
    /// </summary>
    event Action<GameModeState>? OnStateChanged;

    /// <summary>
    /// Event triggered when the session score or streak updates.
    /// </summary>
    event Action<int>? OnScoreChanged;

    /// <summary>
    /// Event triggered when a new question becomes active.
    /// </summary>
    event Action<QuestionItem?>? OnQuestionChanged;

    /// <summary>
    /// Transitions directly to a specified game state.
    /// </summary>
    /// <param name="state">Target state.</param>
    void SetState(GameModeState state);

    /// <summary>
    /// Starts an active game session with an initial question.
    /// </summary>
    /// <param name="category">Educational category.</param>
    /// <param name="initialQuestion">First challenge item.</param>
    void StartGame(SkillCategory category, QuestionItem initialQuestion);

    /// <summary>
    /// Temporarily pauses gameplay and suspends voice listening.
    /// </summary>
    void PauseGame();

    /// <summary>
    /// Resumes active gameplay from a paused state.
    /// </summary>
    void ResumeGame();

    /// <summary>
    /// Records the evaluation outcome of the active challenge.
    /// </summary>
    /// <param name="isCorrect">True if correct.</param>
    /// <param name="starsEarned">Stars earned.</param>
    void RecordAnswer(bool isCorrect, int starsEarned = 1);

    /// <summary>
    /// Sets the next question in the active session.
    /// </summary>
    /// <param name="nextQuestion">Next challenge item.</param>
    void AdvanceQuestion(QuestionItem nextQuestion);

    /// <summary>
    /// Transitions from active play into the celebratory reward screen.
    /// </summary>
    void FinishGameWithReward();

    /// <summary>
    /// Resets the session and returns to the main menu.
    /// </summary>
    void ReturnToMenu();
}
