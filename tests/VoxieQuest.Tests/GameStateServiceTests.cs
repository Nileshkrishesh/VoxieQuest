using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the <see cref="GameStateService"/> lifecycle transitions.
/// </summary>
public class GameStateServiceTests
{
    private readonly GameStateService _service;

    public GameStateServiceTests()
    {
        _service = new GameStateService();
    }

    [Fact]
    public void InitialState_StartsInMenu_WithZeroScore()
    {
        Assert.Equal(GameModeState.Menu, _service.CurrentState);
        Assert.Equal(0, _service.SessionScore);
        Assert.Equal(0, _service.SessionStreak);
        Assert.Null(_service.CurrentQuestion);
    }

    [Fact]
    public void StartGame_TransitionsToPlaying_AndInitializesQuestion()
    {
        var question = new QuestionItem
        {
            Id = "test_q1",
            Category = SkillCategory.Phonics,
            Prompt = "Say Cat!",
            TargetAnswer = "cat"
        };

        _service.StartGame(SkillCategory.Phonics, question);

        Assert.Equal(GameModeState.Playing, _service.CurrentState);
        Assert.Equal(SkillCategory.Phonics, _service.ActiveCategory);
        Assert.NotNull(_service.CurrentQuestion);
        Assert.Equal("test_q1", _service.CurrentQuestion.Id);
        Assert.Equal(0, _service.SessionScore);
    }

    [Fact]
    public void PauseAndResume_TransitionsCorrectly()
    {
        var question = new QuestionItem { Id = "q", TargetAnswer = "a" };
        _service.StartGame(SkillCategory.Vocabulary, question);

        _service.PauseGame();
        Assert.Equal(GameModeState.Paused, _service.CurrentState);

        _service.ResumeGame();
        Assert.Equal(GameModeState.Playing, _service.CurrentState);
    }

    [Fact]
    public void RecordAnswer_Correct_IncrementsScoreAndStreak()
    {
        _service.StartGame(SkillCategory.Numbers, new QuestionItem());

        _service.RecordAnswer(true, starsEarned: 2);
        Assert.Equal(2, _service.SessionScore);
        Assert.Equal(1, _service.SessionStreak);

        _service.RecordAnswer(true, starsEarned: 1);
        Assert.Equal(3, _service.SessionScore);
        Assert.Equal(2, _service.SessionStreak);
    }

    [Fact]
    public void RecordAnswer_Miss_ResetsStreak()
    {
        _service.StartGame(SkillCategory.Numbers, new QuestionItem());
        _service.RecordAnswer(true, 1);
        _service.RecordAnswer(true, 1);
        Assert.Equal(2, _service.SessionStreak);

        _service.RecordAnswer(false);
        Assert.Equal(2, _service.SessionScore); // Score preserved
        Assert.Equal(0, _service.SessionStreak); // Streak reset
    }

    [Fact]
    public void FinishGameWithReward_TransitionsToReward()
    {
        _service.StartGame(SkillCategory.Storytelling, new QuestionItem());
        _service.FinishGameWithReward();

        Assert.Equal(GameModeState.Reward, _service.CurrentState);
    }

    [Fact]
    public void ReturnToMenu_ResetsCurrentSession()
    {
        _service.StartGame(SkillCategory.Phonics, new QuestionItem());
        _service.RecordAnswer(true, 5);

        _service.ReturnToMenu();

        Assert.Equal(GameModeState.Menu, _service.CurrentState);
        Assert.Equal(0, _service.SessionScore);
        Assert.Equal(0, _service.SessionStreak);
        Assert.Null(_service.CurrentQuestion);
    }
}
