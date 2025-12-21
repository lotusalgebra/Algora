using Algora.Core.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace Algora.Web.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        public DashboardViewModel Dashboard { get; set; } = new();

        public void OnGet()
        {
            // Sample data — replace with real data retrieval
            Dashboard.TodaysMoney = 53000m;
            Dashboard.TodaysUsers = 2300;
            Dashboard.NewClients = 3462;
            Dashboard.Sales = 103430m;

            Dashboard.Orders = new List<OrderViewModel>
            {
                new OrderViewModel {
                    Id = 1001,
                    CustomerName = "John Doe",
                    Email = "john@example.com",
                    TotalAmount = 125.50m,
                    Status = "Completed",
                    Date = DateTime.UtcNow.AddDays(-1),
                    ShippingAddress = "123 Main St",
                    BillingAddress = "123 Main St",
                    Items = new List<OrderItemViewModel> {
                        new OrderItemViewModel { Name = "T-shirt", Qty = 2, Price = 25.00m },
                        new OrderItemViewModel { Name = "Hat", Qty = 1, Price = 75.50m }
                    }
                },
                new OrderViewModel {
                    Id = 1002,
                    CustomerName = "Jane Smith",
                    Email = "jane@example.com",
                    TotalAmount = 49.99m,
                    Status = "Pending",
                    Date = DateTime.UtcNow.AddDays(-2),
                    ShippingAddress = "45 Second Ave",
                    BillingAddress = "45 Second Ave",
                    Items = new List<OrderItemViewModel> {
                        new OrderItemViewModel { Name = "Mug", Qty = 1, Price = 49.99m }
                    }
                }
            };
        }
    }
}
