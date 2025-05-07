namespace EmailService_Clarity25.API.Models
{
    using System.ComponentModel.DataAnnotations;

    public class EmailLog
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Sender { get; set; }
        [Required]
        public string Recipient { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public DateTime SentDate { get; set; }
        [Required]
        public string Status { get; set; } // "Pending", "Failed", "Retrying", "Sent"
        public int Attempts { get; set; }
        public string ErrorMessage { get; set; }
    }
}
