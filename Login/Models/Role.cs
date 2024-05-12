using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Login.Models;

[Table("Role")]
public partial class Role
{
    [Key]
    [Column("Role_Id")]
    public int RoleId { get; set; }

    [Column("Role_Desc")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RoleDesc { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
