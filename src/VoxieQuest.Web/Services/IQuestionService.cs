using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service that loads and serves educational curriculum challenges from static JSON files.
/// Enables adding new questions in wwwroot/data without recompiling C# code.
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Retrieves all questions for a specific skill realm, optionally filtered by difficulty.
    /// </summary>
    /// <param name="category">Educational category.</param>
    /// <param name="difficulty">Optional difficulty filter.</param>
    /// <returns>Collection of matching questions.</returns>
    Task<IReadOnlyList<QuestionItem>> GetQuestionsAsync(SkillCategory category, DifficultyLevel? difficulty = null);

    /// <summary>
    /// Selects the next question for a game session, prioritizing the current adaptive difficulty.
    /// </summary>
    /// <param name="category">Educational category.</param>
    /// <param name="difficulty">Target difficulty level.</param>
    /// <param name="currentQuestionId">Optional ID of the currently displayed question to avoid immediate repeats.</param>
    /// <returns>Next challenge question item.</returns>
    Task<QuestionItem?> GetNextQuestionAsync(SkillCategory category, DifficultyLevel difficulty, string? currentQuestionId = null);

    /// <summary>
    /// Loads questions from a specific static JSON curriculum file (e.g., "data/animalsafari.json").
    /// </summary>
    /// <param name="dataFilePath">Relative path to the JSON file inside wwwroot.</param>
    /// <returns>Collection of questions from that file.</returns>
    Task<IReadOnlyList<QuestionItem>> LoadCustomQuestionsAsync(string dataFilePath);
}
