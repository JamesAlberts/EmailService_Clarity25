namespace EmailService_Clarity25.API.Data
{
    using EmailService_Clarity25.API.Models;
    using Microsoft.EntityFrameworkCore;

    public class EmailLogDbContext : DbContext
    {
        public EmailLogDbContext(DbContextOptions<EmailLogDbContext> options) : base(options) { }

        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailLog>()
                .Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<EmailLog>()
                .Property(e => e.ErrorMessage)
                .HasMaxLength(255);
        }
    }
}
