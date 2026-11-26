using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;

namespace HostGrid.Api.Mappers;

internal static class CurrentUserMapper
{
    internal static CurrentUserView ToView(ApplicationUser user, bool superAdmin, IReadOnlyList<MembershipView> memberships)
        => new(user.Id, user.Email!, superAdmin, user.TwoFactorEnabled, memberships);
}
