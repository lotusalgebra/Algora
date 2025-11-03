using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IShopifyBillingService
    {
        Task<string> CreateRecurringChargeAsync(string shopDomain, string accessToken, string planName, decimal price, int trialDays);
        Task<bool> ActivateChargeAsync(string shopDomain, string accessToken, string chargeId);
    }
}
