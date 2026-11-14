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
}
