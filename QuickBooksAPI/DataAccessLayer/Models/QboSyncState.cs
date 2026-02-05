using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBooksAPI.DataAccessLayer.Models
{
    [Table("QBO_Sync_State")]
    public class QboSyncState
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string RealmId { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }

        public DateTime? LastUpdatedAfter { get; set; }

        public int? LastStartPosition { get; set; }

        public DateTime? LastRunAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Enums for type safety
    public enum QboEntityType
    {
        Invoice,
        Customer,
        Chart_Of_Accounts,
        Products,
        Manual_Journals,
        Bills,
        Vendors

    }

    public enum QboSyncStatus
    {
        Running,
        Completed,
        Failed
    }

    // Extension class for easy enum conversion
    public static class QboSyncStateExtensions
    {
        public static QboSyncStatus GetStatusEnum(this QboSyncState syncState)
        {
            return Enum.Parse<QboSyncStatus>(syncState.Status);
        }

        public static void SetStatus(this QboSyncState syncState, QboSyncStatus status)
        {
            syncState.Status = status.ToString();
            syncState.UpdatedAt = DateTime.UtcNow;
        }

        public static QboEntityType GetEntityTypeEnum(this QboSyncState syncState)
        {
            return Enum.Parse<QboEntityType>(syncState.EntityType);
        }

        public static void SetEntityType(this QboSyncState syncState, QboEntityType entityType)
        {
            syncState.EntityType = entityType.ToString();
        }
    }
}
