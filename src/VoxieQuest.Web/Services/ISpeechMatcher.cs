using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Evaluates verbal transcripts from children using pure C# phonetic and string proximity algorithms.
/// Highly tolerant to childhood speech developments, liquid gliding, fronting, and lisps.
/// </summary>
public interface ISpeechMatcher
{
    /// <summary>
    /// Evaluates if the spoken transcript matches the expected challenge answer.
    /// </summary>
    /// <param name="spokenText">Raw text received from speech recognition.</param>
    /// <param name="question">Target question containing accepted answers and hints.</param>
    /// <returns>Detailed match evaluation result.</returns>
    MatchEvaluation Evaluate(string spokenText, QuestionItem question);

    /// <summary>
    /// Evaluates multiple candidate transcripts against the question item, returning the first successful match or best failure.
    /// </summary>
    /// <param name="candidates">Candidate transcript strings.</param>
    /// <param name="question">Target question containing accepted answers and hints.</param>
    /// <returns>Detailed match evaluation result.</returns>
    MatchEvaluation Evaluate(IEnumerable<string> candidates, QuestionItem question);

    /// <summary>
    /// Direct evaluation between a spoken phrase and target word with optional accepted variations.
    /// </summary>
    /// <param name="spokenText">Spoken text.</param>
    /// <param name="target">Target word.</param>
    /// <param name="acceptedAlternates">Optional list of accepted alternate answers.</param>
    /// <returns>Detailed match evaluation result.</returns>
    MatchEvaluation Evaluate(string spokenText, string target, IEnumerable<string>? acceptedAlternates = null);

    /// <summary>
    /// Evaluates multiple candidate transcripts against target word and accepted alternates.
    /// </summary>
    /// <param name="candidates">Candidate transcript strings.</param>
    /// <param name="target">Target word.</param>
    /// <param name="acceptedAlternates">Optional list of accepted alternate answers.</param>
    /// <returns>Detailed match evaluation result.</returns>
    MatchEvaluation Evaluate(IEnumerable<string> candidates, string target, IEnumerable<string>? acceptedAlternates = null);

    /// <summary>
    /// Generates the standard 4-character Soundex code for a word.
    /// </summary>
    /// <param name="word">Input word.</param>
    /// <returns>Soundex string code (e.g. R300).</returns>
    string ComputeSoundex(string word);

    /// <summary>
    /// Generates simplified Metaphone phonetic key for a word.
    /// </summary>
    /// <param name="word">Input word.</param>
    /// <returns>Metaphone key.</returns>
    string ComputeMetaphone(string word);

    /// <summary>
    /// Computes Levenshtein edit distance between two strings.
    /// </summary>
    int ComputeLevenshteinDistance(string s, string t);
}
