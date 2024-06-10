using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Subscription.Models;

public partial class SubContext : DbContext
{
    public SubContext()
    {
    }

    public SubContext(DbContextOptions<SubContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Credit> Credits { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credit>(entity =>
        {
            entity.HasKey(e => e.CreditsId).HasName("PK__Credits__6415A3FAEA0CE6DB");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__9A2B249DFE5CC95D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
