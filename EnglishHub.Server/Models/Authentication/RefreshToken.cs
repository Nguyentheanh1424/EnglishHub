using MongoDB.Bson.Serialization.Attributes;

namespace EnglishHub.Server.Models
{
    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        public required string UserId { get; set; }

        [BsonElement("token")]
        public string? Token { get; set; }

        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [BsonElement("isRevoked")]
        public bool IsRevoked { get; set; } = false;
    }
}
