using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Subscribe.Models;

public partial class Credit
{
    [Key]
    [Column("CreditsID")]
    public int CreditsId { get; set; }

    [Column("ExternalUserID")]
    [StringLength(50)]
    public string ExternalUserId { get; set; } = null!;

    public int Amount { get; set; }

    public DateOnly PurchaseDate { get; set; }
}
