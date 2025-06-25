using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prospect.Server.Api.Services.Database
{
    public class DbFriendService : BaseDbService<FriendModel>
    {
        public DbFriendService(IOptions<DatabaseSettings> options) : base(options, nameof(FriendModel))
        {
        }

        public async Task<List<FriendModel>> GetFriendsAsync(string userId)
        {
            var filter = Builders<FriendModel>.Filter.And(
                Builders<FriendModel>.Filter.Eq(f => f.UserId, userId),
                Builders<FriendModel>.Filter.Eq(f => f.Status, FriendStatus.Accepted)
            );
            return await Collection.Find(filter).ToListAsync();
        }

        public async Task<FriendModel> GetFriendshipAsync(string userId, string friendUserId)
        {
            var filter = Builders<FriendModel>.Filter.And(
                Builders<FriendModel>.Filter.Eq(f => f.UserId, userId),
                Builders<FriendModel>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task AddFriendRequestAsync(string userId, string friendUserId)
        {
            var friendRequest = new FriendModel
            {
                UserId = userId,
                FriendUserId = friendUserId,
                Status = FriendStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await Collection.InsertOneAsync(friendRequest);
        }

        public async Task UpdateFriendStatusAsync(string userId, string friendUserId, FriendStatus status)
        {
            var filter = Builders<FriendModel>.Filter.And(
                Builders<FriendModel>.Filter.Eq(f => f.UserId, userId),
                Builders<FriendModel>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );
            var update = Builders<FriendModel>.Update
                .Set(f => f.Status, status)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);
            await Collection.UpdateOneAsync(filter, update);
        }

        public async Task<List<FriendModel>> GetPendingRequestsAsync(string userId)
        {
            var filter = Builders<FriendModel>.Filter.And(
                Builders<FriendModel>.Filter.Eq(f => f.FriendUserId, userId),
                Builders<FriendModel>.Filter.Eq(f => f.Status, FriendStatus.Pending)
            );
            return await Collection.Find(filter).ToListAsync();
        }
    }
} 