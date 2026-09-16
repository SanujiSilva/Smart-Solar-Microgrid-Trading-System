using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs.Users;

public sealed record UserDetailsResponse(string Id, string? NIC, string FullName, string Email,
    string Phone, string Role, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static UserDetailsResponse From(User user) => new(user.Id.ToString(), user.NIC, user.FullName,
        user.Email, user.Phone, user.Role.ToString(), user.Status.ToString(), user.CreatedAt, user.UpdatedAt);
}

public sealed record UserPageResponse(IReadOnlyList<UserDetailsResponse> Items, long TotalCount, int Page, int PageSize);
