using DailyDevotional.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DailyDevotional.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<Journal> Journals { get; set; }
  public DbSet<DailyReading> DailyReadings { get; set; }
  public DbSet<Book> Books { get; set; }
  public DbSet<Translation> Translations { get; set; }
  public DbSet<TranslationBook> TranslationBooks { get; set; }
  public DbSet<TranslationChapter> TranslationChapters { get; set; }
  public DbSet<Verse> Verses { get; set; }
  public DbSet<UserSettings> UserSettings { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Translation>(entity =>
    {
      entity.Property(t => t.StorageMode).HasConversion<string>().HasMaxLength(16);
      entity.Property(t => t.ProviderKind).HasConversion<string>().HasMaxLength(16);
    });

    modelBuilder.Entity<TranslationBook>()
      .HasKey(tb => new { tb.TranslationId, tb.BookId });

    modelBuilder.Entity<TranslationChapter>()
      .HasKey(tc => new { tc.TranslationId, tc.BookId, tc.Chapter });

    modelBuilder.Entity<Verse>(entity =>
    {
      entity.HasKey(v => new { v.TranslationId, v.BookId, v.Chapter, v.VerseNumber });
      entity.HasIndex(v => new { v.TranslationId, v.FetchedAt });
    });

    // Deleting a translation or a book must never silently remove users' settings
    // or the reading schedule, so these relationships block the delete instead.
    modelBuilder.Entity<UserSettings>()
      .HasOne<Translation>()
      .WithMany()
      .HasForeignKey(s => s.PreferredTranslationId)
      .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<DailyReading>()
      .HasOne(r => r.Book)
      .WithMany()
      .HasForeignKey(r => r.BookId)
      .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Book>().HasData(BibleBookData.Books);

    modelBuilder.Entity<Translation>().HasData(
      new Translation
      {
        Id = 1,
        Code = "ESV",
        Name = "English Standard Version",
        StorageMode = TranslationStorageMode.Cache,
        ProviderKind = TranslationProviderKind.EsvApi,
        MaxCachedVerses = 500,
        MaxCacheAgeDays = 14,
        CopyrightNotice = "Scripture quotations are from the ESV® Bible (The Holy Bible, English Standard Version®), © 2001 by Crossway, a publishing ministry of Good News Publishers. Used by permission. All rights reserved.",
        NonCommercialOnly = true,
        IsEnabled = true,
        SortOrder = 1
      },
      new Translation
      {
        Id = 2,
        Code = "KJV",
        Name = "King James Version",
        StorageMode = TranslationStorageMode.Full,
        ProviderKind = TranslationProviderKind.Local,
        CopyrightNotice = "King James Version (public domain in the United States).",
        NonCommercialOnly = false,
        // Enabled by `import-translation KJV` once its verses are loaded.
        IsEnabled = false,
        SortOrder = 2
      });

    modelBuilder.Entity<DailyReading>().HasData(
      new DailyReading
      {
        Id = 1,
        Date = new DateOnly(2026, 8, 11),
        BookId = 1,
        Chapter = 1,
        StartVerse = 1,
        EndVerse = 5,
        Commentary = "God begins the creation story by bringing order and light into darkness."
      },
      new DailyReading {
        Id = 2,
        Date = new DateOnly(2026, 8, 12),
        BookId = 1,
        Chapter = 1,
        StartVerse = 6,
        EndVerse = 10,
        Commentary = "God continues bringing structure and distinction to creation."
      },
      new DailyReading
      {
        Id = 3,
        Date = new DateOnly(2026, 8, 13),
        BookId = 1,
        Chapter = 1,
        StartVerse = 11,
        EndVerse = 15,
        Commentary = "Creation continues as God brings forth life and establishes the rhythms of the world."
      });
  }
}
