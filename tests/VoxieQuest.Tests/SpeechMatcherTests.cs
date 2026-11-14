using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for <see cref="SpeechMatcher"/> covering childhood phonological variations,
/// carrier phrase stripping, synonyms, numbers, Soundex, Metaphone, and false-positive rejection.
/// </summary>
public class SpeechMatcherTests
{
    private readonly SpeechMatcher _matcher;

    public SpeechMatcherTests()
    {
        _matcher = new SpeechMatcher();
    }

    [Theory]
    [InlineData("wed", "red")]              // Liquid gliding /r/ -> [w]
    [InlineData("wabbit", "rabbit")]        // Liquid gliding /r/ -> [w]
    [InlineData("wun", "run")]              // Liquid gliding /r/ -> [w]
    [InlineData("bwue", "blue")]            // Liquid gliding blend /l/ -> [w]
    [InlineData("lellow", "yellow")]        // Developmental substitution
    [InlineData("yewwow", "yellow")]        // Gliding variation
    [InlineData("wellow", "yellow")]        // Gliding variation
    [InlineData("tat", "cat")]              // Velar fronting /k/ -> [t]
    [InlineData("fith", "fish")]            // Interdental lisp /sh/ -> [th]
    [InlineData("free", "three")]           // Fricative fronting /th/ -> [f]
    [InlineData("teef", "teeth")]           // Fricative fronting /th/ -> [f]
    public void Evaluate_KidMispronunciations_ShouldMatch(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected '{spoken}' to match target '{target}'. Reason: {eval.MatchReason}");
        Assert.True(eval.Confidence >= 0.70, $"Expected confidence >= 0.70, got {eval.Confidence}");
    }

    [Theory]
    [InlineData("it's red", "red")]
    [InlineData("it is red", "red")]
    [InlineData("red color", "red")]
    [InlineData("it's a red apple", "red apple")]
    [InlineData("i see a cat", "cat")]
    [InlineData("i think it's a dog", "dog")]
    [InlineData("open please", "open")]
    [InlineData("the answer is sunny", "sunny")]
    [InlineData("a frog", "frog")]
    public void Evaluate_CarrierPhrasesAndFillers_ShouldStripAndMatch(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected phrase '{spoken}' to match target '{target}'. Reason: {eval.MatchReason}");
    }

    [Theory]
    [InlineData("three", "3")]
    [InlineData("3", "three")]
    [InlineData("five", "5")]
    [InlineData("ten", "10")]
    [InlineData("eight", "8")]
    public void Evaluate_NumberWordsAndDigits_ShouldMatchEquivalently(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected number '{spoken}' to match '{target}'. Reason: {eval.MatchReason}");
    }

    [Fact]
    public void Evaluate_AcceptedSynonyms_ShouldMatch()
    {
        var question = new QuestionItem
        {
            TargetAnswer = "cat",
            AcceptedAnswers = new() { "kitty", "a cat", "kitten" }
        };

        var eval = _matcher.Evaluate("kitty", question);

        Assert.True(eval.IsMatch);
        Assert.Equal("kitty", eval.MatchedTarget);
    }

    [Theory]
    [InlineData("banana", "red")]
    [InlineData("dog", "cat")]
    [InlineData("blue", "red")]
    [InlineData("elephant", "ant")]
    [InlineData("seven", "three")]
    [InlineData("airplane", "frog")]
    public void Evaluate_UnrelatedWords_MustNotMatch(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.False(eval.IsMatch, $"Expected '{spoken}' NOT to match '{target}', but it matched via {eval.MatchReason}");
    }

    [Fact]
    public void ComputeSoundex_ProducesExpectedCodes()
    {
        var code1 = _matcher.ComputeSoundex("Robert");
        var code2 = _matcher.ComputeSoundex("Rupert");

        Assert.Equal("R163", code1);
        Assert.Equal("R163", code2);
    }

    [Fact]
    public void ComputeMetaphone_EncodesSilentLettersCorrectly()
    {
        var knight = _matcher.ComputeMetaphone("knight");
        var night = _matcher.ComputeMetaphone("night");

        Assert.Equal("NT", knight);
        Assert.Equal("NT", night);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("cat", "bat", 1)]
    [InlineData("frog", "frog", 0)]
    [InlineData("apple", "bpple", 1)]
    public void ComputeLevenshteinDistance_CalculatesCorrectEditDistances(string s, string t, int expectedDistance)
    {
        var dist = _matcher.ComputeLevenshteinDistance(s, t);
        Assert.Equal(expectedDistance, dist);
    }
}
