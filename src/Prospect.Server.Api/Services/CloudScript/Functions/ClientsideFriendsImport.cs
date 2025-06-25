using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript;
using Prospect.Server.Api.Services.Database;
using Prospect.Server.Api.Services.Database.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Prospect.Server.Api.Services.Auth.Extensions;

public class ClientsideFriendsImportRequest
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; }
    [JsonPropertyName("userIds")]
    public string[] UserIDs { get; set; }
}

public class ClientsideFriendsImportResponse
{
    [JsonPropertyName("imported")]
    public int Imported { get; set; }
    [JsonPropertyName("linked")]
    public int Linked { get; set; }
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

[CloudScriptFunction("ClientsideFriendsImport")]
public class ClientsideFriendsImportFunction : ICloudScriptFunction<ClientsideFriendsImportRequest, ClientsideFriendsImportResponse>
{
    private readonly ILogger<ClientsideFriendsImportFunction> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DbFriendService _friendService;
    private readonly DbUserService _userService;

    public ClientsideFriendsImportFunction(
        ILogger<ClientsideFriendsImportFunction> logger,
        IHttpContextAccessor httpContextAccessor,
        DbFriendService friendService,
        DbUserService userService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _friendService = friendService;
        _userService = userService;
    }

    public async Task<ClientsideFriendsImportResponse> ExecuteAsync(ClientsideFriendsImportRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null || context.User == null)
        {
            _logger.LogWarning("No HTTP context or user for friends import");
            return new ClientsideFriendsImportResponse { Imported = 0, Linked = 0, Success = false };
        }
        var userId = context.User.FindAuthUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No userId found in context for friends import");
            return new ClientsideFriendsImportResponse { Imported = 0, Linked = 0, Success = false };
        }
        int imported = 0;
        int linked = 0;
        foreach (var friendId in request.UserIDs.Distinct())
        {
            if (friendId == userId) continue; // Don't friend yourself
            var friendUser = await _userService.FindAsync(friendId);
            if (friendUser == null) continue; // Only import if user exists
            var existing = await _friendService.GetFriendshipAsync(userId, friendId);
            if (existing == null)
            {
                // Create accepted friendship (imported)
                await _friendService.AddFriendRequestAsync(userId, friendId);
                await _friendService.UpdateFriendStatusAsync(userId, friendId, FriendStatus.Accepted);
                imported++;
            }
            else if (existing.Status != FriendStatus.Accepted)
            {
                await _friendService.UpdateFriendStatusAsync(userId, friendId, FriendStatus.Accepted);
                linked++;
            }
        }
        _logger.LogInformation("Imported {Imported} and linked {Linked} friends for user {UserId}", imported, linked, userId);
        return new ClientsideFriendsImportResponse { Imported = imported, Linked = linked, Success = true };
    }
}
