using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Models.Client.Data;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.Auth;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.Auth.User;
using Prospect.Server.Api.Services.Database;
using Prospect.Server.Api.Services.Database.Models;
using Prospect.Server.Api.Services.UserData;
using Prospect.Steam;

namespace Prospect.Server.Api.Controllers;

[ApiController]
[Route("Client")]
public class ClientController : Controller
{
    private const int AppIdDefault = 480;
    private const int AppIdCycleBeta = 1600361;
    private const int AppIdCycle = 868270;

    private readonly ILogger<ClientController> _logger;
    private readonly PlayFabSettings _settings;
    private readonly AuthTokenService _authTokenService;
    private readonly DbUserService _userService;
    private readonly DbEntityService _entityService;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    private readonly DbFriendService _friendService;

    public ClientController(ILogger<ClientController> logger,
        IOptions<PlayFabSettings> settings,
        AuthTokenService authTokenService,
        DbUserService userService,
        DbEntityService entityService,
        UserDataService userDataService,
        TitleDataService titleDataService,
        DbFriendService friendService)
    {
        _settings = settings.Value;
        _logger = logger;
        _authTokenService = authTokenService;
        _userService = userService;
        _entityService = entityService;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
        _friendService = friendService;
    }

    [AllowAnonymous]
    [HttpPost("LoginWithSteam")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> LoginWithSteam(FLoginWithSteamRequest request)
    {
        if (!string.IsNullOrEmpty(request.SteamTicket))
        {
            var ticket = AppTicket.Parse(request.SteamTicket);
            if (ticket.IsValid && ticket.HasValidSignature && (ticket.AppId == AppIdDefault || ticket.AppId == AppIdCycleBeta || ticket.AppId == AppIdCycle))
            {
                var userSteamId = ticket.SteamId.ToString();

                var user = await _userService.FindOrCreateAsync(PlayFabUserAuthType.Steam, userSteamId);
                var entity = await _entityService.FindOrCreateAsync(user.Id);

                var userTicket = _authTokenService.GenerateUser(entity);
                var entityTicket = _authTokenService.GenerateEntity(entity);

                await _userDataService.InitAsync(user.Id);

                return Ok(new ClientResponse<FServerLoginResult>
                {
                    Code = 200,
                    Status = "OK",
                    Data = new FServerLoginResult
                    {
                        EntityToken = new FEntityTokenResponse
                        {
                            Entity = new FEntityKey
                            {
                                Id = entity.Id,
                                Type = "title_player_account",
                                TypeString = "title_player_account"
                            },
                            EntityToken = entityTicket,
                            TokenExpiration = DateTime.UtcNow.AddDays(6), // TODO:
                        },
                        InfoResultPayload = new FGetPlayerCombinedInfoResultPayload
                        {
                            CharacterInventories = new List<object>(),
                            PlayerProfile = new FPlayerProfileModel
                            {
                                DisplayName = user.DisplayName,
                                PlayerId = user.Id,
                                PublisherId = _settings.PublisherId,
                                TitleId = _settings.TitleId
                            },
                            UserDataVersion = 0,
                            UserInventory = new List<object>(),
                            UserReadOnlyDataVersion = 0
                        },
                        LastLoginTime = DateTime.UtcNow, // TODO:
                        NewlyCreated = false, // TODO:
                        PlayFabId = user.Id,
                        SessionTicket = userTicket,
                        SettingsForUser = new FUserSettings
                        {
                            GatherDeviceInfo = true,
                            GatherFocusInfo = true,
                            NeedsAttribution = false,
                        },
                        TreatmentAssignment = new FTreatmentAssignment
                        {
                            Variables = new List<FVariable>(),
                            Variants = new List<string>()
                        }
                    }
                });
            }

            _logger.LogWarning("Invalid steam ticket specified, IsExpired {IsExpired}, IsValid {IsValid}, HasValidSignature {Sig}, AppId {AppId}",
                ticket.IsExpired,
                ticket.IsValid,
                ticket.HasValidSignature,
                ticket.AppId);
        }

        return BadRequest(new ClientResponse
        {
            Code = 400,
            Status = "BadRequest",
            Error = "InvalidSteamTicket",
            ErrorCode = 1010,
            ErrorMessage = "Steam API AuthenticateUserTicket error response .."
        });
    }

    [HttpPost("AddGenericID")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public IActionResult AddGenericId(FAddGenericIDRequest request)
    {
        return Ok(new ClientResponse<object>
        {
            Code = 200,
            Status = "OK",
            Data = new {}
        });
    }

    [HttpPost("GetStoreItems")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public IActionResult GetStoreItems(FGetStoreItems request)
    {
        return Ok(new ClientResponse<object>
        {
            Code = 200,
            Status = "OK",
            Data = new {}
        });
    }

    [HttpPost("UpdateUserTitleDisplayName")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public IActionResult UpdateUserTitleDisplayName(FUpdateUserTitleDisplayNameRequest request)
    {
        return Ok(new ClientResponse<FUpdateUserTitleDisplayNameResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FUpdateUserTitleDisplayNameResult
            {
                DisplayName = request.DisplayName
            }
        });
    }

    [HttpPost("UpdateUserData")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public async Task<IActionResult> UpdateUserData(FUpdateUserDataRequest request)
    {
        var userId = User.FindAuthUserId();
        await _userDataService.UpdateAsync(userId, request.PlayFabId, request.Data);

        return Ok(new ClientResponse<FUpdateUserDataResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FUpdateUserDataResult
            {
                DataVersion = 0
            }
        });
    }

    [HttpPost("GetUserData")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public async Task<IActionResult> GetUserData(FGetUserDataRequest request)
    {
        var userId = User.FindAuthUserId();
        var userData = await _userDataService.FindAsync(userId, request.PlayFabId, request.Keys);

        return Ok(new ClientResponse<FGetUserDataResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FGetUserDataResult
            {
                Data = userData,
                DataVersion = 0
            }
        });
    }

    [HttpPost("GetUserReadOnlyData")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public async Task<IActionResult> GetUserReadOnlyData(FGetUserDataRequest request)
    {
        var userId = User.FindAuthUserId();
        var userData = await _userDataService.FindAsync(userId, request.PlayFabId, request.Keys);

        return Ok(new ClientResponse<FGetUserDataResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FGetUserDataResult
            {
                Data = userData,
                DataVersion = 0
            }
        });
    }

    [HttpPost("GetUserInventory")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public async Task<IActionResult> GetUserInventory(FGetUserInventoryRequest request)
    {
        var userId = User.FindAuthUserId();
        var userData = await _userDataService.FindAsync(userId, userId, new List<string>{"Balance", "Inventory", "VanityItems"});
        var inventory = JsonSerializer.Deserialize<FYCustomItemInfo[]>(userData["Inventory"].Value);
        // TODO: Per-user vanity
        // var vanity = JsonSerializer.Deserialize<CustomVanityItem[]>(userData["VanityItems"].Value);
        var titleData = _titleDataService.Find(new List<string>{"Blueprints", "Vanities"});
        var blueprints = JsonSerializer.Deserialize<Dictionary<string, TitleDataBlueprintInfo>>(titleData["Blueprints"]);
        var vanity = JsonSerializer.Deserialize<TitleDataVanityInfo[]>(titleData["Vanities"]);
        List<FItemInstance> items = new List<FItemInstance>();
        foreach (var item in inventory) {
            if (!blueprints.ContainsKey(item.BaseItemId)) {
                _logger.LogWarning("Failed to find item with ID {ItemID}", item.BaseItemId);
                continue;
            }
            var blueprintData = blueprints[item.BaseItemId];
            items.Add(new FItemInstance {
                ItemId = item.BaseItemId,
                ItemInstanceId = item.ItemId,
                ItemClass = blueprintData.Kind,
                // PurchaseDate = DateTime.Now, // TODO
                // UnitPrice = 0,
                CatalogVersion = "StaticItems",
                CustomData = new Dictionary<string, string> {
                    ["mods"] = JsonSerializer.Serialize(item.ModData),
                    // ["insurance"] = "None", // TODO
                    ["vanity"] = $"{{\"p\":{item.PrimaryVanityId},\"s\":{item.SecondaryVanityId}}}", // p - primary, s - secondary
                    ["coreData"] = $"{{\"a\":{item.Amount},\"d\":{item.Durability}}}", // a - amount, d - durability
                    ["origin"] = JsonSerializer.Serialize(item.Origin),
                    ["rolledPerks"] = JsonSerializer.Serialize(item.RolledPerks),
                }
            });
        }

        foreach (var item in vanity) {
            items.Add(new FItemInstance {
                ItemId = item.Name,
                // ItemInstanceId is not required since it's not a unique item.
                ItemClass = "Vanity",
                // PurchaseDate = DateTime.Now, // TODO
                CatalogVersion = "StaticItems",
                CustomData = new Dictionary<string, string> {
                    ["coreData"] = "{}", // Vanity items don't seem to have any coreData. But it's still required since it's an inventory item.
                    ["origin"] = "{}",
                }
            });
        }

        return Ok(new ClientResponse<FGetUserInventoryResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FGetUserInventoryResult
            {
                Inventory = items,
                VirtualCurrency = JsonSerializer.Deserialize<PlayerBalance>(userData["Balance"].Value),
                VirtualCurrencyRechargeTimes = new Dictionary<string, FVirtualCurrencyRechargeTime>()
            }
        });
    }

    [HttpPost("GetTitleData")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public IActionResult GetTitleData(FGetTitleDataRequest request)
    {
        return Ok(new ClientResponse<FGetTitleDataResult>
        {
            Code = 200,
            Status = "OK",
            Data = new FGetTitleDataResult()
            {
                Data = _titleDataService.Find(request.Keys)
            }
        });
    }

    [HttpPost("GetAccountInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
    public async Task<IActionResult> GetAccountInfo(FGetAccountInfoRequest request)
    {
        try
        {
            PlayFabUser? user = null;

            // Search by different criteria
            if (!string.IsNullOrEmpty(request.PlayFabId))
            {
                user = await _userService.FindAsync(request.PlayFabId);
            }
            else if (!string.IsNullOrEmpty(request.TitleDisplayName))
            {
                // Search by display name (case-insensitive)
                user = await _userService.FindByDisplayNameAsync(request.TitleDisplayName);
            }
            else if (!string.IsNullOrEmpty(request.Username))
            {
                // Search by username (case-insensitive)
                user = await _userService.FindByUsernameAsync(request.Username);
            }
            else if (!string.IsNullOrEmpty(request.Email))
            {
                // Search by email (case-insensitive)
                user = await _userService.FindByEmailAsync(request.Email);
            }

            if (user == null)
            {
                return Ok(new ClientResponse<FGetAccountInfoResult>
                {
                    Code = 200,
                    Status = "OK",
                    Data = new FGetAccountInfoResult
                    {
                        AccountInfo = null
                    }
                });
            }

            return Ok(new ClientResponse<FGetAccountInfoResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetAccountInfoResult
                {
                    AccountInfo = new FUserAccountInfo
                    {
                        PlayFabId = user.Id,
                        TitleDisplayName = user.DisplayName,
                        Username = user.DisplayName, // Using display name as username for now
                        Email = null, // Email not stored in current user model
                        Created = user.CreatedAt,
                        LastLogin = user.LastLoginAt
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAccountInfo");
            return StatusCode(500, new ClientResponse
            {
                Code = 500,
                Status = "InternalServerError",
                Error = "InternalServerError",
                ErrorCode = 1000,
                ErrorMessage = "An error occurred while retrieving account information"
            });
        }
    }
}