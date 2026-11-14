using System.Text;
using System.Text.RegularExpressions;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Pure C# Speech Matching engine with Levenshtein edit distance, Soundex, Metaphone,
/// carrier phrase stripping, and childhood phonological tolerance.
/// </summary>
public class SpeechMatcher : ISpeechMatcher
{
    private static readonly string[] CarrierPrefixes = new[]
    {
        "its a ", "its an ", "its the ", "its ",
        "it's a ", "it's an ", "it's the ", "it's ",
        "it is a ", "it is an ", "it is the ", "it is ",
        "this is a ", "this is an ", "this is the ", "this is ",
        "i think its a ", "i think its ", "i think it's a ", "i think it's ", "i think ",
        "i see a ", "i see an ", "i see the ", "i see ",
        "my answer is a ", "my answer is an ", "my answer is the ", "my answer is ",
        "the answer is a ", "the answer is an ", "the answer is the ", "the answer is ",
        "the letter is ", "the letter ", "letter ",
        "the number is ", "the number ", "number ",
        "the color is ", "the color ", "color ",
        "the word is ", "the word ", "word ",
        "the animal is ", "the animal ", "animal ",
        "i choose ", "i picked ", "i pick ",
        "i say ", "say ",
        "a ", "an ", "the "
    };

    private static readonly string[] CarrierSuffixes = new[]
    {
        " color", " please", " stars", " flowers", " word", " fruit", " animal", " letter", " number"
    };

    private static readonly Dictionary<string, string> NumberWordsToDigits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = "0", ["one"] = "1", ["won"] = "1",
        ["two"] = "2", ["to"] = "2", ["too"] = "2",
        ["three"] = "3", ["free"] = "3", ["tree"] = "3",
        ["four"] = "4", ["for"] = "4", ["fore"] = "4",
        ["five"] = "5", ["six"] = "6", ["seven"] = "7",
        ["eight"] = "8", ["ate"] = "8",
        ["nine"] = "9", ["ten"] = "10",
        ["eleven"] = "11", ["twelve"] = "12", ["thirteen"] = "13",
        ["fourteen"] = "14", ["fifteen"] = "15", ["twenty"] = "20"
    };

    private static readonly Dictionary<string, string> DigitsToNumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0"] = "zero", ["1"] = "one", ["2"] = "two", ["3"] = "three", ["4"] = "four",
        ["5"] = "five", ["6"] = "six", ["7"] = "seven", ["8"] = "eight", ["9"] = "nine",
        ["10"] = "ten", ["11"] = "eleven", ["12"] = "twelve", ["13"] = "thirteen",
        ["14"] = "fourteen", ["15"] = "fifteen", ["20"] = "twenty"
    };

    private static readonly Dictionary<string, string[]> LetterPhonetics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new[] { "a", "ay", "eh", "hey" },
        ["b"] = new[] { "b", "be", "bee" },
        ["c"] = new[] { "c", "see", "sea", "si" },
        ["d"] = new[] { "d", "dee" },
        ["e"] = new[] { "e", "ee" },
        ["f"] = new[] { "f", "ef", "eff" },
        ["g"] = new[] { "g", "gee", "ji" },
        ["h"] = new[] { "h", "aitch", "each", "etch", "he" },
        ["i"] = new[] { "i", "eye", "aye", "ai" },
        ["j"] = new[] { "j", "jay" },
        ["k"] = new[] { "k", "kay" },
        ["l"] = new[] { "l", "el", "ell" },
        ["m"] = new[] { "m", "em" },
        ["n"] = new[] { "n", "en", "in" },
        ["o"] = new[] { "o", "oh" },
        ["p"] = new[] { "p", "pea", "pee" },
        ["q"] = new[] { "q", "cue", "queue" },
        ["r"] = new[] { "r", "are", "our" },
        ["s"] = new[] { "s", "es", "ess" },
        ["t"] = new[] { "t", "tea", "tee" },
        ["u"] = new[] { "u", "you", "yu" },
        ["v"] = new[] { "v", "vee", "we" },
        ["w"] = new[] { "w", "double u", "double you" },
        ["x"] = new[] { "x", "ex", "ecs" },
        ["y"] = new[] { "y", "why", "wai" },
        ["z"] = new[] { "z", "zee", "zed" }
    };

    private static readonly Dictionary<string, string> CommonHomophones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["read"] = "red",
        ["blew"] = "blue",
        ["won"] = "one",
        ["to"] = "two",
        ["too"] = "two",
        ["for"] = "four",
        ["fore"] = "four",
        ["ate"] = "eight",
        ["sun"] = "son",
        ["sea"] = "see"
    };

    /// <inheritdoc />
    public MatchEvaluation Evaluate(string spokenText, QuestionItem question)
    {
        if (question == null) return MatchEvaluation.Fail(spokenText, "NullQuestion");
        return Evaluate(spokenText, question.TargetAnswer, question.AcceptedAnswers);
    }

    /// <inheritdoc />
    public MatchEvaluation Evaluate(IEnumerable<string> candidates, QuestionItem question)
    {
        if (question == null) return MatchEvaluation.Fail(string.Empty, "NullQuestion");
        return Evaluate(candidates, question.TargetAnswer, question.AcceptedAnswers);
    }

    /// <inheritdoc />
    public MatchEvaluation Evaluate(IEnumerable<string> candidates, string target, IEnumerable<string>? acceptedAlternates = null)
    {
        if (candidates == null)
        {
            return MatchEvaluation.Fail(string.Empty, "NullCandidates");
        }

        MatchEvaluation? bestFail = null;
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var evaluation = Evaluate(candidate, target, acceptedAlternates);
            if (evaluation.IsMatch)
            {
                return evaluation;
            }
            bestFail ??= evaluation;
        }

        return bestFail ?? MatchEvaluation.Fail(string.Empty, "NoCandidateProvided");
    }

    /// <inheritdoc />
    public MatchEvaluation Evaluate(string spokenText, string target, IEnumerable<string>? acceptedAlternates = null)
    {
        if (string.IsNullOrWhiteSpace(spokenText) || string.IsNullOrWhiteSpace(target))
        {
            return MatchEvaluation.Fail(spokenText ?? string.Empty, "EmptyInput");
        }

        var cleanSpoken = NormalizeString(spokenText);
        var cleanTarget = NormalizeString(target);

        // 1. Exact match on raw clean string
        if (cleanSpoken == cleanTarget)
        {
            return MatchEvaluation.Success(cleanTarget, cleanSpoken, "ExactMatch", 1.0);
        }

        // 2. Strip carrier phrases (e.g. "it's a red apple" -> "red apple", "it's red" -> "red")
        var strippedSpoken = StripCarrierPhrases(cleanSpoken);
        if (strippedSpoken == cleanTarget)
        {
            return MatchEvaluation.Success(cleanTarget, cleanSpoken, "CarrierStrippedExact", 1.0);
        }

        // 3. Accepted variations check
        var alternates = new List<string> { cleanTarget };
        if (acceptedAlternates != null)
        {
            foreach (var alt in acceptedAlternates)
            {
                var normAlt = NormalizeString(alt);
                if (!string.IsNullOrEmpty(normAlt) && !alternates.Contains(normAlt))
                {
                    alternates.Add(normAlt);
                }
            }
        }

        foreach (var alt in alternates)
        {
            if (cleanSpoken == alt || strippedSpoken == alt)
            {
                return MatchEvaluation.Success(alt, cleanSpoken, "AcceptedVariationMatch", 1.0);
            }

            // Word token or phrase containment with whole word boundaries (e.g. "it's a red apple" matches "red apple", but "elephant" does not match "ant")
            if (ContainsAsWholeWordOrPhrase(cleanSpoken, alt) || ContainsAsWholeWordOrPhrase(strippedSpoken, alt))
            {
                return MatchEvaluation.Success(alt, cleanSpoken, "PhraseContainment", 0.95);
            }
        }

        // 4. Number word vs digit equivalence
        if (TryMatchNumberEquivalence(strippedSpoken, cleanTarget, out var numMatched))
        {
            return MatchEvaluation.Success(numMatched, cleanSpoken, "NumberEquivalence", 1.0);
        }

        // 5. Letter Phonetics check (e.g. Target 'C', spoken 'see'; Target 'B', spoken 'bee')
        if (cleanTarget.Length == 1 && char.IsLetter(cleanTarget[0]))
        {
            if (LetterPhonetics.TryGetValue(cleanTarget, out var phonetics))
            {
                if (phonetics.Contains(cleanSpoken) || phonetics.Contains(strippedSpoken))
                {
                    return MatchEvaluation.Success(cleanTarget, cleanSpoken, "LetterPhoneticMatch", 1.0);
                }
            }
        }

        // 6. Common Homophones (e.g. "read" -> "red", "blew" -> "blue")
        if (CommonHomophones.TryGetValue(strippedSpoken, out var homoTarget) && (homoTarget == cleanTarget || alternates.Contains(homoTarget)))
        {
            return MatchEvaluation.Success(cleanTarget, cleanSpoken, "HomophoneMatch", 1.0);
        }

        // 7. Kid-Specific Developmental Phonology Match
        // Handles: "wed" for "red", "bwue" for "blue", "lellow" or "yewwow" for "yellow",
        // "tat" for "cat", "fith" for "fish", "free" for "three", "wabbit" for "rabbit"
        foreach (var alt in alternates)
        {
            if (IsKidPhonologicalMatch(strippedSpoken, alt, out var phonReason))
            {
                return MatchEvaluation.Success(alt, cleanSpoken, $"KidPhonology:{phonReason}", 0.92);
            }
        }

        // 8. Phonetic Metaphone & Soundex Matching
        var targetMetaphone = ComputeMetaphone(cleanTarget);
        var spokenMetaphone = ComputeMetaphone(strippedSpoken);

        if (!string.IsNullOrEmpty(targetMetaphone) && targetMetaphone == spokenMetaphone)
        {
            return MatchEvaluation.Success(cleanTarget, cleanSpoken, "MetaphoneMatch", 0.90);
        }

        var targetSoundex = ComputeSoundex(cleanTarget);
        var spokenSoundex = ComputeSoundex(strippedSpoken);

        if (!string.IsNullOrEmpty(targetSoundex) && targetSoundex == spokenSoundex)
        {
            return MatchEvaluation.Success(cleanTarget, cleanSpoken, "SoundexMatch", 0.88);
        }

        // 7. Forgiving Levenshtein Proximity
        foreach (var alt in alternates)
        {
            var dist = ComputeLevenshteinDistance(strippedSpoken, alt);
            var maxLen = Math.Max(strippedSpoken.Length, alt.Length);

            if (maxLen > 0)
            {
                var proximityRatio = 1.0 - ((double)dist / maxLen);

                // Allow 1 edit for words length 3-4, up to 2 edits for words length >= 5 with high proximity
                bool isClose = (alt.Length <= 4 && dist <= 1) || (alt.Length >= 5 && dist <= 2 && proximityRatio >= 0.70);

                if (isClose)
                {
                    return MatchEvaluation.Success(alt, cleanSpoken, "LevenshteinProximity", Math.Round(proximityRatio, 2));
                }
            }
        }

        return MatchEvaluation.Fail(cleanSpoken, "NoMatchFound");
    }

    /// <summary>
    /// Checks for developmental phonological substitutions common in children aged 5-10.
    /// </summary>
    private bool IsKidPhonologicalMatch(string spoken, string target, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(spoken) || string.IsNullOrWhiteSpace(target)) return false;

        // Specific Kid Speech Normalizations:
        // Rule A: Liquid Gliding (/r/ -> [w], /l/ -> [w] or [y])
        // Example: "wed" -> "red", "wabbit" -> "rabbit", "bwue" -> "blue", "yewwow" -> "yellow"
        var deGlidedSpoken = spoken;
        if (spoken.StartsWith("w") && target.StartsWith("r"))
        {
            deGlidedSpoken = "r" + spoken.Substring(1);
            if (deGlidedSpoken == target || ComputeLevenshteinDistance(deGlidedSpoken, target) <= 1)
            {
                reason = "Gliding_R_to_W";
                return true;
            }
        }

        // Handle "bwue" -> "blue" (w for l in blends)
        if (spoken.Contains("bw") && target.Contains("bl"))
        {
            if (spoken.Replace("bw", "bl") == target)
            {
                reason = "Gliding_L_blend_to_W";
                return true;
            }
        }

        // Handle "fwog" -> "frog", "twain" -> "train", "bwoom" -> "broom" (w for r in blends)
        if (spoken.Contains("w") && target.Contains("r"))
        {
            var deGlidedR = spoken.Replace("tw", "tr")
                                  .Replace("fw", "fr")
                                  .Replace("bw", "br")
                                  .Replace("cw", "cr")
                                  .Replace("gw", "gr");
            if (deGlidedR == target || ComputeLevenshteinDistance(deGlidedR, target) <= 1)
            {
                reason = "Gliding_R_blend_to_W";
                return true;
            }
        }

        // Handle "lellow" / "yewwow" -> "yellow"
        if ((spoken == "lellow" || spoken == "yewwow" || spoken == "wellow") && target == "yellow")
        {
            reason = "Gliding_YellowVariation";
            return true;
        }

        // Rule B: Fricative fronting & lisping (/th/ -> [f], /s/ -> [th], /sh/ -> [th])
        // Example: "free" -> "three", "fith" -> "fish", "thun" -> "sun", "teef" -> "teeth"
        if (spoken.StartsWith("th") && target.StartsWith("s"))
        {
            var deLisped = "s" + spoken.Substring(2);
            if (deLisped == target || ComputeLevenshteinDistance(deLisped, target) <= 1)
            {
                reason = "FrontalLisp_S_to_TH";
                return true;
            }
        }

        if (spoken.StartsWith("f") && target.StartsWith("th"))
        {
            var deFronted = "th" + spoken.Substring(1);
            if (deFronted == target || ComputeLevenshteinDistance(deFronted, target) <= 1)
            {
                reason = "FricativeFronting_TH_to_F";
                return true;
            }
        }

        if (spoken.EndsWith("th") && target.EndsWith("sh"))
        {
            var deLisped = spoken.Substring(0, spoken.Length - 2) + "sh";
            if (deLisped == target)
            {
                reason = "Lisp_SH_to_TH";
                return true;
            }
        }

        if (spoken.EndsWith("f") && target.EndsWith("th"))
        {
            var deFrontedEnd = spoken.Substring(0, spoken.Length - 1) + "th";
            if (deFrontedEnd == target)
            {
                reason = "FricativeFronting_End_TH_to_F";
                return true;
            }
        }

        // Rule C: Velar fronting (/k/ or /c/ -> [t], /g/ -> [d])
        // Example: "tat" -> "cat"
        if (spoken.StartsWith("t") && (target.StartsWith("c") || target.StartsWith("k")))
        {
            var deFrontedVelar = target[0] + spoken.Substring(1);
            if (deFrontedVelar == target)
            {
                reason = "VelarFronting_K_to_T";
                return true;
            }
        }

        // General Kid-Phonetic Transform key comparison
        var kidSpokenKey = ToKidPhoneticKey(spoken);
        var kidTargetKey = ToKidPhoneticKey(target);

        if (!string.IsNullOrEmpty(kidSpokenKey) && kidSpokenKey == kidTargetKey)
        {
            reason = "KidPhoneticEquivalence";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reduces child phonological variations to a canonical simplified acoustic key.
    /// </summary>
    private static string ToKidPhoneticKey(string word)
    {
        if (string.IsNullOrEmpty(word)) return string.Empty;
        var sb = new StringBuilder(word.ToLowerInvariant());

        // Replace common developmental substitutions
        sb.Replace("ph", "f")
          .Replace("th", "f")
          .Replace("sh", "s")
          .Replace("ck", "k")
          .Replace("qu", "k")
          .Replace("bw", "bl")
          .Replace("ww", "w")
          .Replace("ll", "l")
          .Replace("ee", "e")
          .Replace("oo", "u");

        // Normalize initial w/r interchange
        if (sb.Length > 0 && (sb[0] == 'w' || sb[0] == 'r'))
        {
            sb[0] = 'r';
        }

        return sb.ToString();
    }

    private static bool TryMatchNumberEquivalence(string spoken, string target, out string matched)
    {
        matched = string.Empty;

        // If target is digits, check spoken word
        if (NumberWordsToDigits.TryGetValue(spoken, out var digit) && digit == target)
        {
            matched = target;
            return true;
        }

        // If target is word, check spoken digit
        if (DigitsToNumberWords.TryGetValue(spoken, out var word) && word == target)
        {
            matched = target;
            return true;
        }

        // If both map to the same digit (e.g. spoken "to" -> "2", target "two" -> "2")
        if (NumberWordsToDigits.TryGetValue(spoken, out var spkDigit) &&
            NumberWordsToDigits.TryGetValue(target, out var tgtDigit) &&
            spkDigit == tgtDigit)
        {
            matched = target;
            return true;
        }

        return false;
    }

    private static string StripCarrierPhrases(string input)
    {
        var result = input;

        foreach (var prefix in CarrierPrefixes)
        {
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length).Trim();
                break;
            }
        }

        foreach (var suffix in CarrierSuffixes)
        {
            if (result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - suffix.Length).Trim();
                break;
            }
        }

        return result;
    }

    private static bool ContainsAsWholeWordOrPhrase(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return false;
        return Regex.IsMatch(source, $@"\b{Regex.Escape(target)}\b", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Strips punctuation, collapses whitespace, and trims lowercase.
    /// </summary>
    public static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var noApostrophe = input.Replace("'", "").Replace("’", "");
        var stripped = Regex.Replace(noApostrophe, @"[^\w\s]", " ");
        var collapsed = Regex.Replace(stripped, @"\s+", " ");
        return collapsed.ToLowerInvariant().Trim();
    }

    /// <inheritdoc />
    public string ComputeSoundex(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;

        var clean = Regex.Replace(word.ToUpperInvariant(), @"[^A-Z]", "");
        if (clean.Length == 0) return string.Empty;

        var result = new StringBuilder();
        result.Append(clean[0]);

        char previousCode = GetSoundexCode(clean[0]);

        for (var i = 1; i < clean.Length; i++)
        {
            char currentCode = GetSoundexCode(clean[i]);

            if (currentCode != '0' && currentCode != previousCode)
            {
                result.Append(currentCode);
                previousCode = currentCode;
            }
            else if (currentCode != '0')
            {
                previousCode = currentCode;
            }
            else
            {
                // '0' means vowel or H/W/Y - reset previousCode if it separated two identical codes
                previousCode = '0';
            }

            if (result.Length == 4) break;
        }

        while (result.Length < 4)
        {
            result.Append('0');
        }

        return result.ToString();
    }

    private static char GetSoundexCode(char c)
    {
        return c switch
        {
            'B' or 'F' or 'P' or 'V' => '1',
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
            'D' or 'T' => '3',
            'L' => '4',
            'M' or 'N' => '5',
            'R' => '6',
            _ => '0' // A, E, I, O, U, H, W, Y
        };
    }

    /// <inheritdoc />
    public string ComputeMetaphone(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;

        var clean = Regex.Replace(word.ToUpperInvariant(), @"[^A-Z]", "");
        if (clean.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        var i = 0;

        // Skip initial silent letters (KN, GN, PN, WR, PS)
        if (clean.Length >= 2)
        {
            var prefix2 = clean.Substring(0, 2);
            if (prefix2 == "KN" || prefix2 == "GN" || prefix2 == "PN" || prefix2 == "WR" || prefix2 == "PS")
            {
                i = 1;
            }
        }

        while (i < clean.Length)
        {
            char c = clean[i];

            // Don't encode duplicate consecutive consonants (except C)
            if (c != 'C' && i > 0 && clean[i - 1] == c)
            {
                i++;
                continue;
            }

            switch (c)
            {
                case 'A' or 'E' or 'I' or 'O' or 'U':
                    if (i == 0) sb.Append(c); // Vowels only preserved at start
                    break;
                case 'B':
                    if (!(i == clean.Length - 1 && i > 0 && clean[i - 1] == 'M'))
                        sb.Append('B');
                    break;
                case 'C':
                    if (i + 1 < clean.Length && clean[i + 1] == 'H') { sb.Append('X'); i++; }
                    else if (i + 1 < clean.Length && (clean[i + 1] == 'I' || clean[i + 1] == 'E' || clean[i + 1] == 'Y')) sb.Append('S');
                    else sb.Append('K');
                    break;
                case 'D':
                    if (i + 1 < clean.Length && clean[i + 1] == 'G') { sb.Append('J'); i++; }
                    else sb.Append('T');
                    break;
                case 'F': sb.Append('F'); break;
                case 'G':
                    if (i + 1 < clean.Length && clean[i + 1] == 'H') { i++; }
                    else if (i + 1 < clean.Length && (clean[i + 1] == 'I' || clean[i + 1] == 'E' || clean[i + 1] == 'Y')) sb.Append('J');
                    else sb.Append('K');
                    break;
                case 'H':
                    if (i + 1 < clean.Length && "AEIOU".Contains(clean[i + 1]) && (i == 0 || !"CSPTG".Contains(clean[i - 1]))) sb.Append('H');
                    break;
                case 'J': sb.Append('J'); break;
                case 'K': sb.Append('K'); break;
                case 'L': sb.Append('L'); break;
                case 'M': sb.Append('M'); break;
                case 'N': sb.Append('N'); break;
                case 'P':
                    if (i + 1 < clean.Length && clean[i + 1] == 'H') { sb.Append('F'); i++; }
                    else sb.Append('P');
                    break;
                case 'Q': sb.Append('K'); break;
                case 'R': sb.Append('R'); break;
                case 'S':
                    if (i + 1 < clean.Length && clean[i + 1] == 'H') { sb.Append('X'); i++; }
                    else sb.Append('S');
                    break;
                case 'T':
                    if (i + 1 < clean.Length && clean[i + 1] == 'H') { sb.Append('0'); i++; }
                    else sb.Append('T');
                    break;
                case 'V': sb.Append('F'); break;
                case 'W' or 'Y':
                    if (i + 1 < clean.Length && "AEIOU".Contains(clean[i + 1])) sb.Append(c);
                    break;
                case 'X': sb.Append("KS"); break;
                case 'Z': sb.Append('S'); break;
            }

            i++;
            if (sb.Length >= 6) break;
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public int ComputeLevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];

        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;

        for (var i = 1; i <= s.Length; i++)
        {
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s.Length, t.Length];
    }
}
