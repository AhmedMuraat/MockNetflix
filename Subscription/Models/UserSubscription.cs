using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Subscription.Models;

[Table("UserSubscription")]
public partial class UserSubscription
{
    [Key]
    [Column("SubscriptionID")]
    public int SubscriptionId { get; set; }

    [Column("ExternalUserID")]
    [StringLength(50)]
    public string ExternalUserId { get; set; } = null!;

    [Column("PlanID")]
    public int PlanId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [ForeignKey("PlanId")]
    [InverseProperty("UserSubscriptions")]
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
