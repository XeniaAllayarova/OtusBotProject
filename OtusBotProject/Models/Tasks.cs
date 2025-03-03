using LinqToDB.Mapping;

namespace TgBotProject.Models
{
    [Table(Name = "tasks")]
    public class Tasks
    {
        [PrimaryKey, Identity]
        [Column(Name = "id")]
        public long Id { get; set; }

        [Column(Name = "title")]
        public string Title { get; set; }

        [Column(Name = "tags")]
        public string[] Tags { get; set; }

        [Column(Name = "date")]
        public DateTime Date { get; set; }

        [Column(Name = "chat_id")]
        public long ChatId { get; set; }

        [Column(Name = "notification_time")]
        public DateTime NotificationTime { get; set; }

        [Column(Name = "done")]
        public bool Done { get; set; }

        public Tasks(long chatId)
        {
            ChatId = chatId;
        }

        public Tasks()
        {
            
        }
    }
}
