using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Domain.Entities
{
    public class Shop
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Domain { get; set; } = string.Empty;
        public string? OfflineAccessToken { get; set; }
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    }
}
