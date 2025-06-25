using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Prospect.Server.Api.Services.Database.Models
{
    public class SquadModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("squadId")]
        public string SquadId { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("leaderId")]
        public string LeaderId { get; set; }

        [BsonElement("members")]
        public List<SquadMemberModel> Members { get; set; } = new List<SquadMemberModel>();

        [BsonElement("mapName")]
        public string MapName { get; set; } = "";

        [BsonElement("deployTime")]
        public int DeployTime { get; set; } = 0; // Unix timestamp

        [BsonElement("allReady")]
        public bool AllReady { get; set; } = false;

        [BsonElement("inDeployFlow")]
        public bool InDeployFlow { get; set; } = false;

        [BsonElement("matchmakingState")]
        public int MatchmakingState { get; set; } = 0; // 0 = not matchmaking, 1 = matchmaking, 2 = matched

        [BsonElement("sessionId")]
        public string SessionId { get; set; } = "";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SquadMemberModel
    {
        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("displayName")]
        public string DisplayName { get; set; }

        [BsonElement("isReady")]
        public bool IsReady { get; set; } = false;

        [BsonElement("isInDeployFlow")]
        public bool IsInDeployFlow { get; set; } = false;

        [BsonElement("isConnected")]
        public bool IsConnected { get; set; } = true;
    }
} 