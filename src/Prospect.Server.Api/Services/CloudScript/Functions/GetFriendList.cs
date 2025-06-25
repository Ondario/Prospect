using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Database;
using Prospect.Server.Api.Services.Auth.Extensions;
using Microsoft.AspNetCore.Http;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetFriendList")]
public class GetFriendList : ICloudScriptFunction<FYBaseSocialRequest, object?>
{
    private readonly DbFriendService _friendService;
    private readonly DbUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int FriendLimit = 100;

    public GetFriendList(DbFriendService friendService, DbUserService userService, IHttpContextAccessor httpContextAccessor)
    {
        _friendService = friendService;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<object?> ExecuteAsync(FYBaseSocialRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return new { friendLimit = FriendLimit, friends = new object[0], invites = new object[0] };

        var userId = context.User.FindAuthUserId();

        // Get accepted friends
        var friends = await _friendService.GetFriendsAsync(userId);
        var friendList = new List<object>();
        foreach (var friend in friends)
        {
            var user = await _userService.FindAsync(friend.FriendUserId);
            friendList.Add(new {
                userId = friend.FriendUserId,
                displayName = user?.DisplayName ?? "Unknown"
            });
        }

        // Get pending friend invites (received)
        var invites = await _friendService.GetPendingRequestsAsync(userId);
        var inviteList = new List<object>();
        foreach (var invite in invites)
        {
            var user = await _userService.FindAsync(invite.UserId);
            inviteList.Add(new {
                userId = invite.UserId,
                displayName = user?.DisplayName ?? "Unknown"
            });
        }

        return new {
            friendLimit = FriendLimit,
            friends = friendList,
            invites = inviteList
        };
    }
}