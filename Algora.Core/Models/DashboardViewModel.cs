using System;
using System.Collections.Generic;
using System.Linq;

namespace Algora.Core.Models
{
    public class DashboardViewModel
    {
        public decimal TodaysMoney { get; set; }
        public int TodaysUsers { get; set; }
        public int NewClients { get; set; }
        public decimal Sales { get; set; }

        public IEnumerable<OrderViewModel> Orders { get; set; } = Enumerable.Empty<OrderViewModel>();
    }
}