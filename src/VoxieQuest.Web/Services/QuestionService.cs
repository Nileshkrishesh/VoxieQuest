using System.Net.Http.Json;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Loads and caches questions from JSON files in wwwroot/data.
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<SkillCategory, List<QuestionItem>> _cache = new();
    private readonly Dictionary<string, List<QuestionItem>> _fileCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client to load static assets.</param>
    public QuestionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuestionItem>> GetQuestionsAsync(SkillCategory category, DifficultyLevel? difficulty = null)
    {
        await EnsureLoadedAsync(category);

        if (!_cache.TryGetValue(category, out var list) || list.Count == 0)
        {
            return Array.Empty<QuestionItem>();
        }

        if (difficulty.HasValue)
        {
            var filtered = list.Where(q => q.Difficulty == difficulty.Value).ToList();
            return filtered.Count > 0 ? filtered : list;
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<QuestionItem?> GetNextQuestionAsync(SkillCategory category, DifficultyLevel difficulty, string? currentQuestionId = null)
    {
        var questions = await GetQuestionsAsync(category, difficulty);
        if (questions.Count == 0) return null;

        var candidates = questions.Where(q => q.Id != currentQuestionId).ToList();
        if (candidates.Count == 0) candidates = questions.ToList();

        return candidates[_random.Next(candidates.Count)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuestionItem>> LoadCustomQuestionsAsync(string dataFilePath)
    {
        if (string.IsNullOrWhiteSpace(dataFilePath)) return Array.Empty<QuestionItem>();

        var normalizedPath = dataFilePath.Trim().TrimStart('/');
        if (_fileCache.TryGetValue(normalizedPath, out var cached) && cached.Count > 0)
        {
            return cached;
        }

        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<QuestionItem>>(normalizedPath);
            if (items != null && items.Count > 0)
            {
                _fileCache[normalizedPath] = items;
                return items;
            }
        }
        catch
        {
            // Fallback or handle load error gracefully
        }

        return Array.Empty<QuestionItem>();
    }

    private async Task EnsureLoadedAsync(SkillCategory category)
    {
        if (_cache.ContainsKey(category)) return;

        var fileName = category switch
        {
            SkillCategory.Phonics => "data/phonics.json",
            SkillCategory.Vocabulary => "data/words.json",
            SkillCategory.Numbers => "data/numbers.json",
            SkillCategory.Storytelling => "data/stories.json",
            _ => "data/phonics.json"
        };

        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<QuestionItem>>(fileName);
            if (items != null && items.Count > 0)
            {
                _cache[category] = items;
                return;
            }
        }
        catch
        {
            // Fallback questions populated if file fetch fails
        }

        _cache[category] = GetFallbackQuestions(category);
    }

    private static List<QuestionItem> GetFallbackQuestions(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Phonics => new List<QuestionItem>
            {
                new()
                {
                    Id = "p_cat",
                    Category = SkillCategory.Phonics,
                    Difficulty = DifficultyLevel.Easy,
                    Prompt = "What says meow and rhymes with BAT?",
                    TargetAnswer = "cat",
                    AcceptedAnswers = new() { "cat", "kitty", "a cat" },
                    Hint = "C - A - T",
                    PhoneticSpelling = "K - AA - T",
                    IconSvg = "cat",
                    RewardStars = 1
                },
                new()
                {
                    Id = "p_dog",
                    Category = SkillCategory.Phonics,
                    Difficulty = DifficultyLevel.Easy,
                    Prompt = "What says woof and rhymes with LOG?",
                    TargetAnswer = "dog",
                    AcceptedAnswers = new() { "dog", "puppy", "a dog" },
                    Hint = "D - O - G",
                    PhoneticSpelling = "D - AW - G",
                    IconSvg = "dog",
                    RewardStars = 1
                }
            },
            SkillCategory.Vocabulary => new List<QuestionItem>
            {
                new()
                {
                    Id = "v_apple",
                    Category = SkillCategory.Vocabulary,
                    Difficulty = DifficultyLevel.Easy,
                    Prompt = "Say the name of this sweet, crunchy red fruit!",
                    TargetAnswer = "apple",
                    AcceptedAnswers = new() { "apple", "an apple", "red apple" },
                    Hint = "Starts with letter A",
                    PhoneticSpelling = "AE - P - AH - L",
                    IconSvg = "apple",
                    RewardStars = 1
                }
            },
            SkillCategory.Numbers => new List<QuestionItem>
            {
                new()
                {
                    Id = "n_three",
                    Category = SkillCategory.Numbers,
                    Difficulty = DifficultyLevel.Easy,
                    Prompt = "How many stars do you see? ⭐ ⭐ ⭐",
                    TargetAnswer = "three",
                    AcceptedAnswers = new() { "three", "3", "three stars" },
                    Hint = "Count 1, 2...",
                    PhoneticSpelling = "TH - R - IY",
                    IconSvg = "stars",
                    RewardStars = 1
                }
            },
            SkillCategory.Storytelling => new List<QuestionItem>
            {
                new()
                {
                    Id = "s_magic_gate",
                    Category = SkillCategory.Storytelling,
                    Difficulty = DifficultyLevel.Easy,
                    Prompt = "The magic castle gate asks: Do you say OPEN or CLOSE?",
                    TargetAnswer = "open",
                    AcceptedAnswers = new() { "open", "please open", "open gate" },
                    Hint = "Say OPEN to step inside!",
                    PhoneticSpelling = "OW - P - AH - N",
                    IconSvg = "gate",
                    RewardStars = 2
                }
            },
            _ => new List<QuestionItem>()
        };
    }
}
