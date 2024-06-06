using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Subscription.Models;

[Table("Subscription")]
public partial class Subscription
{
    [Key]
    public int SubscriptionId { get; set; }

    public int? UserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? SubscriptionType { get; set; }

    public int? Credits { get; set; }

    public DateOnly? SubscriptionStart { get; set; }

    public DateOnly? SubscriptionEnd { get; set; }
}
