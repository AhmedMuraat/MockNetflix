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

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credit>(entity =>
        {
            entity.HasKey(e => e.CreditsId).HasName("PK__Credits__6415AC1A764CDE67");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Subscrip__755C22D7CFBDEC28");
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__UserSubs__9A2B24BDC404830A");

            entity.HasOne(d => d.Plan).WithMany(p => p.UserSubscriptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserSubsc__PlanI__38996AB5");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
