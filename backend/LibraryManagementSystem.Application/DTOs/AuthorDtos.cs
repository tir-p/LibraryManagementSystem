// -----------------------------------------------------------------------------
// File header:
// AuthorDtos defines the shapes for author data: AuthorDto is what the API
// returns, Create/Update requests are what the client sends. Requests are kept
// separate so clients cannot set Id or audit fields directly.
// For junior devs: record = compact immutable class ideal for DTOs.
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

// Read model returned to clients.
/// <summary>
/// Author data returned to clients.
/// </summary>
/// <param name="Id">Unique author identifier.</param>
/// <param name="Name">Full/display name of the author.</param>
/// <param name="Biography">Optional short bio.</param>
/// <param name="DateOfBirth">Optional birth date (date only, no time).</param>
public record AuthorDto(Guid Id, string Name, string? Biography, DateOnly? DateOfBirth);

// Payloads accepted by the API. Kept separate so clients can't set Id/audit fields.
/// <summary>
/// Payload for creating a new author.
/// </summary>
/// <param name="Name">Required author name.</param>
/// <param name="Biography">Optional biography text.</param>
/// <param name="DateOfBirth">Optional birth date.</param>
public record CreateAuthorRequest(string Name, string? Biography, DateOnly? DateOfBirth);
/// <summary>
/// Payload for updating an existing author (DateOfBirth is immutable here).
/// </summary>
/// <param name="Name">Updated author name.</param>
/// <param name="Biography">Updated biography text.</param>
public record UpdateAuthorRequest(string Name, string? Biography);
