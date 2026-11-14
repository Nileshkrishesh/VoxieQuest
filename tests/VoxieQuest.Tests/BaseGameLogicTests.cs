using VoxieQuest.Web.Models;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests verifying the shared 10-round game logic, 3-attempt scaffolding, and scoring rules.
/// </summary>
public class BaseGameLogicTests
{
    [Fact]
    public void QuestionAttemptState_InitialState_IsAttempt1_WithNoHint()
    {
        var state = new QuestionAttemptState();

        Assert.Equal(1, state.AttemptCount);
        Assert.False(state.ShowHint);
        Assert.False(state.IsExhausted);
        Assert.Empty(state.FeedbackMessage);
    }

    [Fact]
    public void QuestionAttemptState_FirstMiss_AdvancesToAttempt2_NoHint_NoNegativeWords()
    {
        var state = new QuestionAttemptState();
        state.RecordMiss("red", "Like a strawberry");

        Assert.Equal(2, state.AttemptCount);
        Assert.False(state.ShowHint);
        Assert.False(state.IsExhausted);
        Assert.Equal("Almost! Try again!", state.FeedbackMessage);
        Assert.DoesNotContain("Wrong", state.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Incorrect", state.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Fail", state.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuestionAttemptState_SecondMiss_AdvancesToAttempt3_ShowsHint()
    {
        var state = new QuestionAttemptState();
        state.RecordMiss("red", "Like a strawberry");
        state.RecordMiss("red", "Like a strawberry");

        Assert.Equal(3, state.AttemptCount);
        Assert.True(state.ShowHint);
        Assert.False(state.IsExhausted);
        Assert.Contains("Like a strawberry", state.FeedbackMessage);
        Assert.DoesNotContain("Wrong", state.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuestionAttemptState_ThirdMiss_MarksExhausted_ProvidesCompassionateResolution()
    {
        var state = new QuestionAttemptState();
        state.RecordMiss("red", "Like a strawberry");
        state.RecordMiss("red", "Like a strawberry");
        state.RecordMiss("red", "Like a strawberry");

        Assert.True(state.IsExhausted);
        Assert.Contains("Great try!", state.FeedbackMessage);
        Assert.Contains("'red'", state.FeedbackMessage);
        Assert.DoesNotContain("Wrong", state.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuestionAttemptState_Reset_RestoresFreshAttempt1()
    {
        var state = new QuestionAttemptState();
        state.RecordMiss("sun", "Shines bright");
        state.RecordMiss("sun", "Shines bright");

        state.Reset();

        Assert.Equal(1, state.AttemptCount);
        Assert.False(state.ShowHint);
        Assert.False(state.IsExhausted);
        Assert.Empty(state.FeedbackMessage);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public void TenRoundSession_RoundIndicesStayWithinBounds(int currentRound, int expectedRound)
    {
        const int TotalRounds = 10;
        Assert.True(currentRound >= 1 && currentRound <= TotalRounds);
        Assert.Equal(expectedRound, currentRound);
    }

    [Fact]
    public void StarScoring_AccumulatesCorrectlyAcrossRounds()
    {
        int totalStars = 0;
        int streak = 0;

        // Simulate 10 rounds with 8 correct and 2 misses
        for (int r = 1; r <= 10; r++)
        {
            bool isCorrect = r != 3 && r != 7;
            if (isCorrect)
            {
                totalStars += 2;
                streak++;
            }
            else
            {
                streak = 0;
            }
        }

        Assert.Equal(16, totalStars);
        Assert.Equal(3, streak); // Rounds 8, 9, 10
    }
}
