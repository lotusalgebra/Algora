using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities
{
    public class License
    {
        public int Id { get; set; }
        public string ShopDomain { get; set; } = string.Empty;
        public string PlanName { get; set; } = "Basic";
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string ChargeId { get; set; } = string.Empty;
        public string Status { get; set; } = "trial"; // active, trial, cancelled, expired
    }
}
