using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Prospect.Server.Api.Services.Database.Models
{
    public enum FriendStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    public class FriendModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("friendUserId")]
        public string FriendUserId { get; set; }

        [BsonElement("status")]
        public FriendStatus Status { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 