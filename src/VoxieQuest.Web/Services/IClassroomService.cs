using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing ephemeral classroom session pods with zero PII and room codes.
/// </summary>
public interface IClassroomService
{
    /// <summary>
    /// Gets all active in-memory classroom rooms created during this session.
    /// </summary>
    IReadOnlyList<ClassroomRoom> ActiveRooms { get; }

    /// <summary>
    /// Generates a new classroom room pod with a mnemonic code (e.g. OAK-742).
    /// </summary>
    Task<ClassroomRoom> CreateRoomAsync(string targetGameId, string targetGameTitle, string scenarioName, string teacherNotes);

    /// <summary>
    /// Finds an active classroom room by its 6-character room code.
    /// </summary>
    Task<ClassroomRoom?> GetRoomAsync(string roomCode);

    /// <summary>
    /// Joins a room pod as a student station.
    /// </summary>
    Task<bool> JoinRoomAsync(string roomCode, string stationName);

    /// <summary>
    /// Closes and permanently disposes of a classroom room pod.
    /// </summary>
    Task<bool> CloseRoomAsync(string roomCode);

    /// <summary>
    /// Generates post-session group discussion debrief cards for the teacher.
    /// </summary>
    IReadOnlyList<string> GetDiscussionCards(string gameId);
}
