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

            //Chat entity configuration
            builder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.ChatId);

                // We need 2 foreign keys for users 1 and 2.
                entity.HasOne(u => u.User1)
                    .WithMany() // each user can have multiple chats
                    .HasForeignKey(e => e.User1Id)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasOne(u => u.User2)
                    .WithMany()
                    .HasForeignKey(e => e.User2Id)
                    .OnDelete(DeleteBehavior.Restrict); 


                entity.Property(e => e.UserName1).IsRequired().HasMaxLength(250);
                entity.Property(e => e.UserName2).IsRequired().HasMaxLength(250);
                entity.Property(e => e.BlockFlag).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.LastMessage);

                //Indexing is needed
                entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
            });

            //Message entity configuration 
            builder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageId);

                // Relationship with Chat
                entity.HasOne(u => u.Chat)
                    .WithMany() // A chat can have many messages.
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade); // If a chat is deleted, delete the messages

                // Relationship with User (sender)
                entity.HasOne( u => u.Sender )
                    .WithMany() // A user can send many messages
                    .HasForeignKey(e => e.SenderId);

                entity.Property(e => e.MessageText).IsRequired(false); // Nullable
                entity.Property(e => e.PhotoId).IsRequired(false); // Nullable
                entity.Property(e => e.SoundId).IsRequired(false); // Nullable
                entity.Property(e => e.Timestamp).IsRequired();
            });


            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole{
                    Name = "ChatUser",
                    NormalizedName = "USER"
                },
                new IdentityRole{
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                }
            };
            builder.Entity<IdentityRole>().HasData(roles);
        }
    }
}