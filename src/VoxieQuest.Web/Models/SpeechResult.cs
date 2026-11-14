namespace VoxieQuest.Web.Models;

/// <summary>
/// Encapsulates the transcribed result received from the Web Speech API.
/// </summary>
public class SpeechResult
{
    /// <summary>
    /// Gets or sets the recognized speech text transcript.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets candidate alternative transcripts reported by the speech recognition engine.
    /// </summary>
    public List<string> Alternatives { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score between 0.0 and 1.0 reported by the speech engine.
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether this is a final transcript or an interim result.
    /// </summary>
    public bool IsFinal { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this transcription matched the active question's expected answer.
    /// </summary>
    public bool IsMatch { get; set; } = false;

    /// <summary>
    /// Gets all unique candidate transcripts in priority order (Transcript followed by Alternatives).
    /// </summary>
    public IEnumerable<string> GetAllCandidates()
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(Transcript))
        {
            list.Add(Transcript.Trim());
        }
        if (Alternatives != null)
        {
            foreach (var alt in Alternatives)
            {
                if (!string.IsNullOrWhiteSpace(alt) && !list.Contains(alt.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(alt.Trim());
                }
            }
        }
        return list;
    }
}
