using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Subscription.Models;

public partial class Credit
{
    [Key]
    [Column("CreditsID")]
    public int CreditsId { get; set; }

    [Column("ExternalUserID")]
    [StringLength(50)]
    public int ExternalUserId { get; set; }

    public int Amount { get; set; }

    public DateOnly PurchaseDate { get; set; }
}
