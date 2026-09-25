namespace LibraryManagementSystem.Application.DTOs;

// Read model returned to clients.
public record AuthorDto(Guid Id, string Name, string? Biography, DateOnly? DateOfBirth);

// Payloads accepted by the API. Kept separate so clients can't set Id/audit fields.
public record CreateAuthorRequest(string Name, string? Biography, DateOnly? DateOfBirth);
public record UpdateAuthorRequest(string Name, string? Biography);
