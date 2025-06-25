using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;

namespace Prospect.Server.Api.Services.Database;

public class DbUserService : BaseDbService<PlayFabUser>
{
    public DbUserService(IOptions<DatabaseSettings> settings) : base(settings, nameof(PlayFabUser))
    {
    }

    public async Task<PlayFabUser> FindAsync(PlayFabUserAuthType type, string key)
    {
        return await Collection.Find(user => user.Auth.Any(auth => 
            auth.Type == type && 
            auth.Key == key)).FirstOrDefaultAsync();
    }

    private async Task<PlayFabUser> CreateAsync(PlayFabUserAuthType type, string key)
    {
        var now = DateTime.UtcNow;
        var user = new PlayFabUser
        {
            DisplayName = "Unknown",
            Auth = new List<PlayFabUserAuth>
            {
                new PlayFabUserAuth
                {
                    Type = type,
                    Key = key
                }
            },
            CreatedAt = now,
            LastLoginAt = now
        };
            
        await Collection.InsertOneAsync(user);

        return user;
    }

    public async Task<PlayFabUser> FindOrCreateAsync(PlayFabUserAuthType type, string key)
    {
        var user = await FindAsync(type, key);
        if (user != null)
        {
            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await Collection.ReplaceOneAsync(u => u.Id == user.Id, user);
            return user;
        }
        return await CreateAsync(type, key);
    }

    public async Task<PlayFabUser?> FindAsync(string playFabId)
    {
        return await Collection.Find(user => user.Id == playFabId).FirstOrDefaultAsync();
    }

    public async Task<PlayFabUser?> FindByDisplayNameAsync(string displayName)
    {
        var filter = Builders<PlayFabUser>.Filter.Regex(user => user.DisplayName, 
            new MongoDB.Bson.BsonRegularExpression(displayName, "i")); // Case-insensitive
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<PlayFabUser?> FindByUsernameAsync(string username)
    {
        // For now, search by display name since username is not stored separately
        return await FindByDisplayNameAsync(username);
    }

    public async Task<PlayFabUser?> FindByEmailAsync(string email)
    {
        // Email is not currently stored in the user model, so return null
        // This can be implemented when email support is added
        return null;
    }
}