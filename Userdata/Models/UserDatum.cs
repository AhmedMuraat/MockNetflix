using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Userdata.Models;

[Index("UserId", Name = "UQ__UserData__1788CC4DB36C0F49", IsUnique = true)]
public partial class UserDatum
{
    [Key]
    public int UserInfoId { get; set; }

    public int UserId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(255)]
    public string? Address { get; set; }

    public DateOnly DateOfBirth { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AccountCreated { get; set; }
}
