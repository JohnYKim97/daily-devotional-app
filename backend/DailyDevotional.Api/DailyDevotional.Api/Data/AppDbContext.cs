using DailyDevotional.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace DailyDevotional.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<Journal> Journals { get; set; }
  public DbSet<DailyReading> DailyReadings { get; set; }
  public DbSet<DailyReadingVerse> DailyReadingVerses { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<DailyReadingVerse>()
      .HasOne(v => v.DailyReading)
      .WithMany(r => r.Verses)
      .HasForeignKey(v => v.DailyReadingId);

    modelBuilder.Entity<DailyReading>().HasData(
      new DailyReading
      {
        Id = 1,
        Date = new DateOnly(2026, 8, 11),
        Book = "Genesis",
        Chapter = 1,
        StartVerse = 1,
        EndVerse = 5,
        Commentary = "God begins the creation story by bringing order and light into darkness."
      },
      new DailyReading {
        Id = 2,
        Date = new DateOnly(2026, 8, 12),
        Book = "Genesis",
        Chapter = 1,
        StartVerse = 6,
        EndVerse = 10,
        Commentary = "God continues bringing structure and distinction to creation."
      },
      new DailyReading
      {
        Id = 3,
        Date = new DateOnly(2026, 8, 13),
        Book = "Genesis",
        Chapter = 1,
        StartVerse = 11,
        EndVerse = 15,
        Commentary = "Creation continues as God brings forth life and establishes the rhythms of the world."
      });

    modelBuilder.Entity<DailyReadingVerse>().HasData(
      new DailyReadingVerse
      {
        Id = 1,
        DailyReadingId = 1,
        VerseNumber = 1,
        Text = "Placeholder text for Genesis 1:1."
      },
      new DailyReadingVerse()
      {
        Id = 2,
        DailyReadingId = 1,
        VerseNumber = 2,
        Text = "Placeholder text for Genesis 1:2."
      },
      new DailyReadingVerse()
      {
        Id = 3,
        DailyReadingId = 1,
        VerseNumber = 3,
        Text = "Placeholder text for Genesis 1:3."
      },
      new DailyReadingVerse()
      {
        Id = 4,
        DailyReadingId = 1,
        VerseNumber = 4,
        Text = "Placeholder text for Genesis 1:4."
      },
      new DailyReadingVerse()
      {
        Id = 5,
        DailyReadingId = 1,
        VerseNumber = 5,
        Text = "Placeholder text for Genesis 1:5."
      });
  }
}
