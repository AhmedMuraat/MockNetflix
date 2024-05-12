using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Login.Models;

[Table("RefreshToken")]
public partial class RefreshToken
{
    [Key]
    [Column("Token_Id")]
    public int TokenId { get; set; }

    [Column("User_Id")]
    public int? UserId { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? Token { get; set; }

    [Column("Expiry_Date", TypeName = "datetime")]
    public DateTime? ExpiryDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User? User { get; set; }
}
