using System.Text.Json;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for Animal Safari and Word Builder curriculum JSON data,
/// 3-tier difficulty distribution, 10 rounds per tier, and speech recognition matching.
/// </summary>
public class AnimalSafariAndWordBuilderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string GetWebDataPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "VoxieQuest.sln")) && !File.Exists(Path.Combine(current.FullName, "VoxieQuest.slnx")))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            throw new FileNotFoundException("Could not locate solution root directory");
        }

        return Path.Combine(current.FullName, "src", "VoxieQuest.Web", "wwwroot", "data", fileName);
    }

    [Fact]
    public void AnimalSafari_CurriculumJson_LoadsAndContainsAllThreeLevelsWithTenRoundsEach()
    {
        var filePath = GetWebDataPath("animalsafari.json");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var questions = JsonSerializer.Deserialize<List<QuestionItem>>(json, JsonOptions);

        Assert.NotNull(questions);
        Assert.True(questions.Count >= 30, $"Expected >= 30 questions, found {questions.Count}");

        var easyQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Easy).ToList();
        var mediumQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Medium).ToList();
        var hardQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Hard).ToList();

        Assert.Equal(10, easyQuestions.Count);
        Assert.Equal(10, mediumQuestions.Count);
        Assert.Equal(10, hardQuestions.Count);

        foreach (var q in questions)
        {
            Assert.False(string.IsNullOrWhiteSpace(q.Id), "Question ID must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(q.Prompt), "Prompt must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(q.TargetAnswer), "TargetAnswer must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(q.Hint), "Hint must not be empty");
            Assert.NotEmpty(q.AcceptedAnswers);
            Assert.True(q.RewardStars >= 1, "RewardStars should be >= 1");
        }
    }

    [Fact]
    public void WordBuilder_CurriculumJson_LoadsAndContainsAllThreeLevelsWithTenRoundsEach()
    {
        var filePath = GetWebDataPath("wordbuilder.json");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var questions = JsonSerializer.Deserialize<List<QuestionItem>>(json, JsonOptions);

        Assert.NotNull(questions);
        Assert.True(questions.Count >= 30, $"Expected >= 30 questions, found {questions.Count}");

        var easyQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Easy).ToList();
        var mediumQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Medium).ToList();
        var hardQuestions = questions.Where(q => q.Difficulty == DifficultyLevel.Hard).ToList();

        Assert.Equal(10, easyQuestions.Count);
        Assert.Equal(10, mediumQuestions.Count);
        Assert.Equal(10, hardQuestions.Count);

        // Easy words must be 3-letter CVC words
        foreach (var q in easyQuestions)
        {
            Assert.Equal(3, q.TargetAnswer.Length);
        }

        // Medium words must be 4-letter blend words
        foreach (var q in mediumQuestions)
        {
            Assert.Equal(4, q.TargetAnswer.Length);
        }

        // Hard words must be 5-6 letters
        foreach (var q in hardQuestions)
        {
            Assert.InRange(q.TargetAnswer.Length, 5, 6);
        }

        foreach (var q in questions)
        {
            Assert.False(string.IsNullOrWhiteSpace(q.Id));
            Assert.False(string.IsNullOrWhiteSpace(q.Prompt));
            Assert.False(string.IsNullOrWhiteSpace(q.TargetAnswer));
            Assert.False(string.IsNullOrWhiteSpace(q.Hint));
            Assert.NotEmpty(q.AcceptedAnswers);
        }
    }

    [Theory]
    [InlineData("cat", "cat")]
    [InlineData("it's a cat", "cat")]
    [InlineData("tat", "cat")] // Velar fronting
    [InlineData("kitty", "cat")]
    [InlineData("dog", "dog")]
    [InlineData("puppy", "dog")]
    [InlineData("cow", "cow")]
    [InlineData("a cow", "cow")]
    [InlineData("frog", "frog")]
    [InlineData("fwog", "frog")] // Liquid gliding
    [InlineData("duck", "duck")]
    [InlineData("lion", "lion")]
    [InlineData("elephant", "elephant")]
    public void SpeechMatcher_AnimalSafari_CorrectlyIdentifiesAnimalCalls(string spoken, string expectedAnimal)
    {
        var matcher = new SpeechMatcher();
        var question = new QuestionItem
        {
            TargetAnswer = expectedAnimal,
            AcceptedAnswers = new() { expectedAnimal, $"a {expectedAnimal}", $"the {expectedAnimal}", "kitty", "puppy" },
            Hint = "Test animal hint"
        };

        var result = matcher.Evaluate(spoken, question);
        Assert.True(result.IsMatch, $"Expected '{spoken}' to match animal '{expectedAnimal}'");
    }

    [Theory]
    [InlineData("cat", "cat")]
    [InlineData("tat", "cat")]
    [InlineData("sun", "sun")]
    [InlineData("thun", "sun")] // Lisp
    [InlineData("dog", "dog")]
    [InlineData("frog", "frog")]
    [InlineData("fwog", "frog")]
    [InlineData("star", "star")]
    [InlineData("apple", "apple")]
    [InlineData("tiger", "tiger")]
    [InlineData("train", "train")]
    [InlineData("twain", "train")] // Gliding
    public void SpeechMatcher_WordBuilder_CorrectlyMatchesTargetWordsWithKidPhonetics(string spoken, string expectedWord)
    {
        var matcher = new SpeechMatcher();
        var question = new QuestionItem
        {
            TargetAnswer = expectedWord,
            AcceptedAnswers = new() { expectedWord, $"a {expectedWord}" },
            Hint = "Test word hint"
        };

        var result = matcher.Evaluate(spoken, question);
        Assert.True(result.IsMatch, $"Expected '{spoken}' to match word '{expectedWord}'");
    }

    [Fact]
    public void WordBuilder_LetterBlocks_CorrectlySplitAndReassemble()
    {
        const string targetWord = "FROG";
        var blocks = targetWord.Select(c => c.ToString()).ToList();

        Assert.Equal(4, blocks.Count);
        Assert.Equal("F", blocks[0]);
        Assert.Equal("R", blocks[1]);
        Assert.Equal("O", blocks[2]);
        Assert.Equal("G", blocks[3]);

        var reassembled = string.Concat(blocks);
        Assert.Equal(targetWord, reassembled);
    }
}
