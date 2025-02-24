using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.Helpers;
using chatbackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace chatbackend.Data
{
    public class ApplicationDBContext : IdentityDbContext<User>
    {

        private readonly string _baseFilePath;
        private readonly ILogger<ApplicationDBContext> _logger;
        private readonly IWebHostEnvironment _environment;
        public ApplicationDBContext(DbContextOptions dbContextOptions, 
                                    IConfiguration configuration,
                                    ILogger<ApplicationDBContext> logger,
                                    IWebHostEnvironment environment) 
                                        : base(dbContextOptions)
        {
            _baseFilePath = configuration["FileStorage:BaseFilePath"];
            _logger = logger;
            _environment = environment;
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
                
                entity.HasIndex(c => new { c.User1Id, c.User2Id });

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

                entity.HasOne( u => u.Receiver )
                    .WithMany() // A user can receive many messages
                    .HasForeignKey(e => e.ReceiverId);

                entity.Property(e => e.MessageText).IsRequired(false); // Nullable
                entity.Property(e => e.FileFlag).IsRequired(true);
                entity.Property(e => e.FileId).IsRequired(false); // Nullable
                entity.Property(e => e.FileExtension).IsRequired(false); // Nullable
                entity.Property(e => e.Timestamp).IsRequired();

                entity.HasIndex(c => new { c.SenderId });

                entity.HasIndex(e => new { e.ChatId });
            });
            
            builder.Entity<Chat>()
                .HasIndex(c => new { c.User1Id, c.User2Id });


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
            builder.Entity<User>(entity =>
                {
                    entity.Property(e => e.RefreshToken);
                }
            );
        }
    
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var deletedFilePaths = new List<string>(); // Collect file paths

            // 1. Detect Deleted Messages BEFORE calling base.SaveChangesAsync()
            var deletedMessageEntries = ChangeTracker.Entries<Message>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            // 2. Construct File Paths and Collect Them
            foreach (var entry in deletedMessageEntries)
            {
                var message = entry.Entity;

                if (message.FileFlag != 0)
                {
                    if (message.FileId == Guid.Empty)
                    {
                        _logger.LogWarning($"Message with ID {message.MessageId} has FileFlag set but FileId is null or empty.");
                        continue; // Skip this message and continue to the next
                    }

                    if (string.IsNullOrEmpty(message.FileExtension))
                    {
                        _logger.LogWarning($"Message with ID {message.MessageId} has FileFlag set but FileExtension is null or empty.");
                        continue;
                    }

                    string filePath = FileSystemAccess.CreateFilePath(message);
                    deletedFilePaths.Add(filePath);
                }
            }

            // 3. Call the base implementation to perform the database deletion
            int result = await base.SaveChangesAsync(cancellationToken);

            //4. Delete the files from the file system AFTER the database changes are saved. This is important in case database change fails you dont want files gone.
            foreach (var filePath in deletedFilePaths)
            {
                try
                {
                    //Extract foldername and filename to call method:
                    string fileName = Path.GetFileName(filePath);
                    string folderName = Path.GetDirectoryName(filePath);
                    FileSystemAccess.DeleteFile(folderName, fileName);
                    _logger.LogInformation($"Deleted file: {filePath}");
                }
                catch (Exception ex)
                {
                    // Log the error, but don't re-throw.  We don't want to prevent the
                    // database changes from being committed just because a file deletion failed.
                    _logger.LogError(ex, $"Error deleting file: {filePath}");
                }
            }

            return result;
        }
    }
}