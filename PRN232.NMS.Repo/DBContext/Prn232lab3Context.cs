using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PRN232.NMS.Repo.Entities;
using System;
using System.Collections.Generic;

namespace PRN232.NMS.Repo.DBContext;

public partial class Prn232lab3Context : DbContext
{
    public Prn232lab3Context()
    {
    }

    public Prn232lab3Context(DbContextOptions<Prn232lab3Context> options)
        : base(options)
    {
    }

    public virtual DbSet<GradingResult> GradingResults { get; set; }

    public static string GetConnectionString(string connectionStringName)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = config.GetConnectionString(connectionStringName);
        return connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString("DefaultConnection")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GradingResult>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__GradingR__32C52B9997314938");

            entity.Property(e => e.Points).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProjectFolder).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.StudentName).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
