using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Subscription.Models;

public partial class Credit
{
    //test
    [Key]
    public int CreditsId { get; set; }

    public int? UserId { get; set; }

    public int? AvailableCredits { get; set; }
}
