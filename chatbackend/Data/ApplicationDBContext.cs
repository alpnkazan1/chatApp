using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace chatbackend.Data
{
    public class ApplicationDBContext : IdentityDbContext<User>
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
            
        }
        // There is an issue with HasData() taking in dynamically defined parameters. 
        // In this case it takes List<IdentityRole>. I believe it decides that 
        // this structure can change depending on different things so it refuses to do migrations
        // We solved this issue by just ignoring PendingModelChangesWarning with below code snippet.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.ConfigureWarnings(warnings => warnings
                .Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

        // After creating new DB entities like below you first run: 
        // dotnet ef migrations add <MigrationName>  
        // This adds migration, but, it will not update db until you run: 
        // dotnet ef database update
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Chat>(x => x.HasKey(c => c.ChatId));

            builder.Entity<Chat>()
                .HasOne(c => c.User1)
                .WithMany(u => u.Chats)
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Portfolio>()
                .HasOne( u=>u.Stock)
                .WithMany(u => u.Portfolios)
                .HasForeignKey(p => p.StockId);                

            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole{
                    Name = "ChatUser",
                    NormalizedName = "USER"
                },
            };
            builder.Entity<IdentityRole>().HasData(roles);
        }
    }
}