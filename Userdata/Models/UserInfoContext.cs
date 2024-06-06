using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Userdata.Models;

public partial class UserInfoContext : DbContext
{
    public UserInfoContext()
    {
    }

    public UserInfoContext(DbContextOptions<UserInfoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<UserDatum> UserData { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDatum>(entity =>
        {
            entity.HasKey(e => e.UserInfoId)
                  .HasName("PK__UserData__D07EF2E48BE271BB");

            entity.Property(e => e.UserInfoId)
                  .ValueGeneratedOnAdd();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
