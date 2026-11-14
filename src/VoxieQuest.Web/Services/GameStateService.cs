using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements game lifecycle coordination across states: Menu, Playing, Reward, and Paused.
/// </summary>
public class GameStateService : IGameStateService
{
    private GameModeState _currentState = GameModeState.Menu;
    private SkillCategory _activeCategory = SkillCategory.Phonics;
    private QuestionItem? _currentQuestion;
    private int _sessionScore = 0;
    private int _sessionStreak = 0;

    /// <inheritdoc />
    public GameModeState CurrentState => _currentState;

    /// <inheritdoc />
    public SkillCategory ActiveCategory => _activeCategory;

    /// <inheritdoc />
    public QuestionItem? CurrentQuestion => _currentQuestion;

    /// <inheritdoc />
    public int SessionScore => _sessionScore;

    /// <inheritdoc />
    public int SessionStreak => _sessionStreak;

    /// <inheritdoc />
    public event Action<GameModeState>? OnStateChanged;

    /// <inheritdoc />
    public event Action<int>? OnScoreChanged;

    /// <inheritdoc />
    public event Action<QuestionItem?>? OnQuestionChanged;

    /// <inheritdoc />
    public void SetState(GameModeState state)
    {
        if (_currentState != state)
        {
            _currentState = state;
            OnStateChanged?.Invoke(_currentState);
        }
    }

    /// <inheritdoc />
    public void StartGame(SkillCategory category, QuestionItem initialQuestion)
    {
        _activeCategory = category;
        _currentQuestion = initialQuestion;
        _sessionScore = 0;
        _sessionStreak = 0;

        _currentState = GameModeState.Playing;
        OnStateChanged?.Invoke(_currentState);
        OnQuestionChanged?.Invoke(_currentQuestion);
        OnScoreChanged?.Invoke(_sessionScore);
    }

    /// <inheritdoc />
    public void PauseGame()
    {
        if (_currentState == GameModeState.Playing)
        {
            SetState(GameModeState.Paused);
        }
    }

    /// <inheritdoc />
    public void ResumeGame()
    {
        if (_currentState == GameModeState.Paused)
        {
            SetState(GameModeState.Playing);
        }
    }

    /// <inheritdoc />
    public void RecordAnswer(bool isCorrect, int starsEarned = 1)
    {
        if (isCorrect)
        {
            _sessionScore += Math.Max(1, starsEarned);
            _sessionStreak++;
        }
        else
        {
            _sessionStreak = 0;
        }

        OnScoreChanged?.Invoke(_sessionScore);
    }

    /// <inheritdoc />
    public void AdvanceQuestion(QuestionItem nextQuestion)
    {
        _currentQuestion = nextQuestion;
        OnQuestionChanged?.Invoke(_currentQuestion);
    }

    /// <inheritdoc />
    public void FinishGameWithReward()
    {
        SetState(GameModeState.Reward);
    }

    /// <inheritdoc />
    public void ReturnToMenu()
    {
        _currentQuestion = null;
        _sessionScore = 0;
        _sessionStreak = 0;
        SetState(GameModeState.Menu);
        OnQuestionChanged?.Invoke(null);
    }
}
