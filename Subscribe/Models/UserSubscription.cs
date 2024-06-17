using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Subscribe.Models;

[Table("UserSubscription")]
public partial class UserSubscription
{
    [Key]
    [Column("SubscriptionID")]
    public int SubscriptionId { get; set; }

    [Column("ExternalUserID")]
    public int ExternalUserId { get; set; }

    [Column("PlanID")]
    public int PlanId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [ForeignKey("PlanId")]
    [InverseProperty("UserSubscriptions")]
    [JsonIgnore]
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
