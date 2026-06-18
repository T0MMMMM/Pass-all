using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Passall.Modeles;

public class DataContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbDir = Path.Combine(appData, "Pass-all");
            Directory.CreateDirectory(dbDir);
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(dbDir, "passall.db")}");
        }
    }
    
    public DbSet<DBUser> User { get; set; }
    public DbSet<DBDictionary> Dictionary { get; set; }
    public DbSet<DBSettings> Settings { get; set; }
    public DbSet<DBUserProfile> UserProfile { get; set; }
    public DbSet<DBProfileCategory> ProfileCategory { get; set; }
}
