using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EnglishHub.Server.Models
{
    public class OtpCode
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("userId")]
        public required string UserId { get; set; }
        [BsonElement("code")]
        public required string Code { get; set; }
        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
