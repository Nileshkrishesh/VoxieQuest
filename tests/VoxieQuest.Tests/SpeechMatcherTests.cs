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
    [InlineData("the letter b", "b")]
    [InlineData("letter c", "c")]
    [InlineData("the number 4", "4")]
    [InlineData("number two", "2")]
    [InlineData("the animal is a lion", "lion")]
    [InlineData("the word is cat", "cat")]
    [InlineData("the color red", "red")]
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
    [InlineData("to", "2")]
    [InlineData("too", "2")]
    [InlineData("for", "4")]
    [InlineData("fore", "4")]
    [InlineData("ate", "8")]
    [InlineData("won", "1")]
    [InlineData("free", "3")]
    [InlineData("to", "two")]
    [InlineData("for", "four")]
    public void Evaluate_NumberWordsAndDigits_ShouldMatchEquivalently(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected number '{spoken}' to match '{target}'. Reason: {eval.MatchReason}");
    }

    [Theory]
    [InlineData("see", "c")]
    [InlineData("sea", "c")]
    [InlineData("bee", "b")]
    [InlineData("be", "b")]
    [InlineData("tea", "t")]
    [InlineData("tee", "t")]
    [InlineData("why", "y")]
    [InlineData("you", "u")]
    [InlineData("aitch", "h")]
    [InlineData("jay", "j")]
    [InlineData("kay", "k")]
    [InlineData("eye", "i")]
    [InlineData("are", "r")]
    [InlineData("cue", "q")]
    [InlineData("vee", "v")]
    [InlineData("zee", "z")]
    [InlineData("zed", "z")]
    public void Evaluate_LetterPhonetics_ShouldMatch(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected letter phonetic '{spoken}' to match '{target}'. Reason: {eval.MatchReason}");
    }

    [Theory]
    [InlineData("read", "red")]
    [InlineData("blew", "blue")]
    public void Evaluate_ColorHomophones_ShouldMatch(string spoken, string target)
    {
        var eval = _matcher.Evaluate(spoken, target);

        Assert.True(eval.IsMatch, $"Expected homophone '{spoken}' to match '{target}'. Reason: {eval.MatchReason}");
    }

    [Fact]
    public void Evaluate_CandidatesList_ShouldMatchIfAnyCandidateMatches()
    {
        var candidates = new List<string> { "unrelated noise", "to", "too" };
        var eval = _matcher.Evaluate(candidates, "2");

        Assert.True(eval.IsMatch);
        Assert.Equal("2", eval.MatchedTarget);
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
