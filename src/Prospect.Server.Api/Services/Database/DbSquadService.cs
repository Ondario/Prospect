using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prospect.Server.Api.Services.Database
{
    public class DbSquadService : BaseDbService<SquadModel>
    {
        public DbSquadService(IOptions<DatabaseSettings> options) : base(options, nameof(SquadModel))
        {
        }

        public async Task<SquadModel> GetSquadAsync(string squadId)
        {
            return await Collection.Find(squad => squad.SquadId == squadId).FirstOrDefaultAsync();
        }

        public async Task<List<SquadModel>> GetSquadsByMemberAsync(string userId)
        {
            var filter = Builders<SquadModel>.Filter.ElemMatch(squad => squad.Members, 
                member => member.UserId == userId);
            return await Collection.Find(filter).ToListAsync();
        }

        public async Task<SquadModel> GetSquadByLeaderAsync(string leaderId)
        {
            return await Collection.Find(squad => squad.LeaderId == leaderId).FirstOrDefaultAsync();
        }

        public async Task<SquadModel> CreateSquadAsync(SquadModel squad)
        {
            squad.CreatedAt = DateTime.UtcNow;
            squad.UpdatedAt = DateTime.UtcNow;
            await Collection.InsertOneAsync(squad);
            return squad;
        }

        public async Task<bool> UpdateSquadAsync(SquadModel squad)
        {
            squad.UpdatedAt = DateTime.UtcNow;
            var result = await Collection.ReplaceOneAsync(s => s.SquadId == squad.SquadId, squad);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteSquadAsync(string squadId)
        {
            var result = await Collection.DeleteOneAsync(squad => squad.SquadId == squadId);
            return result.DeletedCount > 0;
        }

        public async Task<bool> AddMemberToSquadAsync(string squadId, SquadMemberModel member)
        {
            var update = Builders<SquadModel>.Update
                .Push(squad => squad.Members, member)
                .Set(squad => squad.UpdatedAt, DateTime.UtcNow);
            
            var result = await Collection.UpdateOneAsync(squad => squad.SquadId == squadId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveMemberFromSquadAsync(string squadId, string userId)
        {
            var update = Builders<SquadModel>.Update
                .PullFilter(squad => squad.Members, member => member.UserId == userId)
                .Set(squad => squad.UpdatedAt, DateTime.UtcNow);
            
            var result = await Collection.UpdateOneAsync(squad => squad.SquadId == squadId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateMemberInSquadAsync(string squadId, string userId, SquadMemberModel updatedMember)
        {
            var filter = Builders<SquadModel>.Filter.And(
                Builders<SquadModel>.Filter.Eq(squad => squad.SquadId, squadId),
                Builders<SquadModel>.Filter.ElemMatch(squad => squad.Members, member => member.UserId == userId)
            );

            var update = Builders<SquadModel>.Update
                .Set(squad => squad.Members[-1], updatedMember)
                .Set(squad => squad.UpdatedAt, DateTime.UtcNow);
            
            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateSquadStateAsync(string squadId, string mapName, bool allReady, bool inDeployFlow, int matchmakingState, string sessionId)
        {
            var update = Builders<SquadModel>.Update
                .Set(squad => squad.MapName, mapName)
                .Set(squad => squad.AllReady, allReady)
                .Set(squad => squad.InDeployFlow, inDeployFlow)
                .Set(squad => squad.MatchmakingState, matchmakingState)
                .Set(squad => squad.SessionId, sessionId)
                .Set(squad => squad.UpdatedAt, DateTime.UtcNow);
            
            var result = await Collection.UpdateOneAsync(squad => squad.SquadId == squadId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<List<SquadModel>> GetSquadsInMatchmakingAsync()
        {
            return await Collection.Find(squad => squad.MatchmakingState == 1).ToListAsync();
        }

        public async Task<List<SquadModel>> GetSquadsInMatchAsync()
        {
            return await Collection.Find(squad => squad.MatchmakingState == 2).ToListAsync();
        }
    }
} 