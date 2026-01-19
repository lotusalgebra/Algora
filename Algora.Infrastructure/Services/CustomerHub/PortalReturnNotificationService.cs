using Algora.Application.DTOs.Communication;
using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Algora.Infrastructure.Services.CustomerHub;

/// <summary>
/// Service for sending email notifications related to portal return requests
/// </summary>
public class PortalReturnNotificationService : IPortalReturnNotificationService
{
    private readonly INotificationService _notificationService;
    private readonly IShopService _shopService;
    private readonly ILogger<PortalReturnNotificationService> _logger;

    public PortalReturnNotificationService(
        INotificationService notificationService,
        IShopService shopService,
        ILogger<PortalReturnNotificationService> logger)
    {
        _notificationService = notificationService;
        _shopService = shopService;
        _logger = logger;
    }

    public async Task SendReturnApprovedNotificationAsync(
        string shopDomain,
        PortalReturnDetailDto returnRequest,
        string? returnLabelUrl = null)
    {
        try
        {
            var shop = await _shopService.GetShopAsync(shopDomain);
            var storeName = shop?.ShopName ?? "Our Store";

            var subject = $"Your Return Request #{returnRequest.Id} Has Been Approved";
            var body = BuildApprovedEmailBody(returnRequest, storeName, returnLabelUrl);

            await _notificationService.SendEmailAsync(shopDomain, new SendEmailNotificationDto
            {
                ToEmail = returnRequest.CustomerEmail,
                Subject = subject,
                Body = body,
                IsHtml = true,
                FromName = storeName
            });

            _logger.LogInformation("Sent return approved notification for request {RequestId} to {Email}",
                returnRequest.Id, returnRequest.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return approved notification for request {RequestId}", returnRequest.Id);
        }
    }

    public async Task SendReturnRejectedNotificationAsync(
        string shopDomain,
        PortalReturnDetailDto returnRequest,
        string rejectionReason)
    {
        try
        {
            var shop = await _shopService.GetShopAsync(shopDomain);
            var storeName = shop?.ShopName ?? "Our Store";

            var subject = $"Update on Your Return Request #{returnRequest.Id}";
            var body = BuildRejectedEmailBody(returnRequest, storeName, rejectionReason);

            await _notificationService.SendEmailAsync(shopDomain, new SendEmailNotificationDto
            {
                ToEmail = returnRequest.CustomerEmail,
                Subject = subject,
                Body = body,
                IsHtml = true,
                FromName = storeName
            });

            _logger.LogInformation("Sent return rejected notification for request {RequestId} to {Email}",
                returnRequest.Id, returnRequest.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return rejected notification for request {RequestId}", returnRequest.Id);
        }
    }

    public async Task SendReturnCompletedNotificationAsync(
        string shopDomain,
        PortalReturnDetailDto returnRequest,
        decimal refundAmount)
    {
        try
        {
            var shop = await _shopService.GetShopAsync(shopDomain);
            var storeName = shop?.ShopName ?? "Our Store";

            var subject = $"Your Return #{returnRequest.Id} is Complete - Refund Issued";
            var body = BuildCompletedEmailBody(returnRequest, storeName, refundAmount);

            await _notificationService.SendEmailAsync(shopDomain, new SendEmailNotificationDto
            {
                ToEmail = returnRequest.CustomerEmail,
                Subject = subject,
                Body = body,
                IsHtml = true,
                FromName = storeName
            });

            _logger.LogInformation("Sent return completed notification for request {RequestId} to {Email}",
                returnRequest.Id, returnRequest.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return completed notification for request {RequestId}", returnRequest.Id);
        }
    }

    public async Task SendReturnReceivedNotificationAsync(
        string shopDomain,
        PortalReturnDetailDto returnRequest)
    {
        try
        {
            var shop = await _shopService.GetShopAsync(shopDomain);
            var storeName = shop?.ShopName ?? "Our Store";

            var subject = $"We've Received Your Return - Request #{returnRequest.Id}";
            var body = BuildReceivedEmailBody(returnRequest, storeName);

            await _notificationService.SendEmailAsync(shopDomain, new SendEmailNotificationDto
            {
                ToEmail = returnRequest.CustomerEmail,
                Subject = subject,
                Body = body,
                IsHtml = true,
                FromName = storeName
            });

            _logger.LogInformation("Sent return received notification for request {RequestId} to {Email}",
                returnRequest.Id, returnRequest.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return received notification for request {RequestId}", returnRequest.Id);
        }
    }

    public async Task SendReturnSubmittedNotificationAsync(
        string shopDomain,
        PortalReturnDetailDto returnRequest)
    {
        try
        {
            var shop = await _shopService.GetShopAsync(shopDomain);
            var storeName = shop?.ShopName ?? "Our Store";

            var subject = $"Return Request #{returnRequest.Id} Submitted Successfully";
            var body = BuildSubmittedEmailBody(returnRequest, storeName);

            await _notificationService.SendEmailAsync(shopDomain, new SendEmailNotificationDto
            {
                ToEmail = returnRequest.CustomerEmail,
                Subject = subject,
                Body = body,
                IsHtml = true,
                FromName = storeName
            });

            _logger.LogInformation("Sent return submitted notification for request {RequestId} to {Email}",
                returnRequest.Id, returnRequest.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send return submitted notification for request {RequestId}", returnRequest.Id);
        }
    }

    #region Email Templates

    private static string BuildApprovedEmailBody(PortalReturnDetailDto request, string storeName, string? returnLabelUrl)
    {
        var itemsHtml = BuildItemsTableHtml(request);
        var labelSection = string.IsNullOrEmpty(returnLabelUrl) ? "" : $@"
            <div style=""background-color: #e8f5e9; padding: 20px; border-radius: 8px; margin: 20px 0;"">
                <h3 style=""color: #2e7d32; margin: 0 0 10px 0;"">📦 Return Shipping Label</h3>
                <p style=""margin: 0 0 15px 0;"">We've prepared a return shipping label for you:</p>
                <a href=""{returnLabelUrl}"" style=""display: inline-block; background-color: #4caf50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;"">Download Return Label</a>
            </div>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <!-- Header -->
            <div style=""background: linear-gradient(135deg, #4caf50 0%, #8bc34a 100%); padding: 30px; text-align: center;"">
                <h1 style=""color: white; margin: 0; font-size: 24px;"">✅ Return Approved!</h1>
            </div>

            <!-- Content -->
            <div style=""padding: 30px;"">
                <p style=""font-size: 16px; color: #333; margin: 0 0 20px 0;"">
                    Great news! Your return request <strong>#{request.Id}</strong> for order <strong>{request.OrderNumber}</strong> has been approved.
                </p>

                {labelSection}

                <h3 style=""color: #333; border-bottom: 2px solid #eee; padding-bottom: 10px;"">Items to Return</h3>
                {itemsHtml}

                <div style=""background-color: #fff3e0; padding: 20px; border-radius: 8px; margin-top: 20px;"">
                    <h4 style=""color: #e65100; margin: 0 0 10px 0;"">📋 Next Steps</h4>
                    <ol style=""margin: 0; padding-left: 20px; color: #333;"">
                        <li style=""margin-bottom: 8px;"">Pack the items securely in their original packaging if possible</li>
                        <li style=""margin-bottom: 8px;"">Print and attach the return shipping label</li>
                        <li style=""margin-bottom: 8px;"">Drop off the package at your nearest shipping location</li>
                        <li style=""margin-bottom: 0;"">We'll process your refund once we receive the items</li>
                    </ol>
                </div>

                {(string.IsNullOrEmpty(request.AdminNotes) ? "" : $@"
                <div style=""margin-top: 20px; padding: 15px; background-color: #f5f5f5; border-radius: 8px;"">
                    <p style=""margin: 0; color: #666; font-size: 14px;""><strong>Note from our team:</strong> {request.AdminNotes}</p>
                </div>")}
            </div>

            <!-- Footer -->
            <div style=""background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;"">
                <p style=""margin: 0; color: #666; font-size: 14px;"">Thank you for shopping with {storeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildRejectedEmailBody(PortalReturnDetailDto request, string storeName, string rejectionReason)
    {
        var itemsHtml = BuildItemsTableHtml(request);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <!-- Header -->
            <div style=""background: linear-gradient(135deg, #f44336 0%, #e91e63 100%); padding: 30px; text-align: center;"">
                <h1 style=""color: white; margin: 0; font-size: 24px;"">Return Request Update</h1>
            </div>

            <!-- Content -->
            <div style=""padding: 30px;"">
                <p style=""font-size: 16px; color: #333; margin: 0 0 20px 0;"">
                    We've reviewed your return request <strong>#{request.Id}</strong> for order <strong>{request.OrderNumber}</strong>.
                </p>

                <div style=""background-color: #ffebee; padding: 20px; border-radius: 8px; border-left: 4px solid #f44336;"">
                    <h3 style=""color: #c62828; margin: 0 0 10px 0;"">Unable to Process Return</h3>
                    <p style=""margin: 0; color: #333;"">{rejectionReason}</p>
                </div>

                <h3 style=""color: #333; border-bottom: 2px solid #eee; padding-bottom: 10px; margin-top: 30px;"">Requested Items</h3>
                {itemsHtml}

                <div style=""background-color: #e3f2fd; padding: 20px; border-radius: 8px; margin-top: 20px;"">
                    <h4 style=""color: #1565c0; margin: 0 0 10px 0;"">💬 Need Help?</h4>
                    <p style=""margin: 0; color: #333;"">
                        If you believe this decision was made in error or have additional information to provide,
                        please don't hesitate to contact our customer support team. We're here to help!
                    </p>
                </div>
            </div>

            <!-- Footer -->
            <div style=""background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;"">
                <p style=""margin: 0; color: #666; font-size: 14px;"">Thank you for your understanding - {storeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildCompletedEmailBody(PortalReturnDetailDto request, string storeName, decimal refundAmount)
    {
        var itemsHtml = BuildItemsTableHtml(request);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <!-- Header -->
            <div style=""background: linear-gradient(135deg, #7c3aed 0%, #ec4899 100%); padding: 30px; text-align: center;"">
                <h1 style=""color: white; margin: 0; font-size: 24px;"">🎉 Return Complete!</h1>
            </div>

            <!-- Content -->
            <div style=""padding: 30px;"">
                <p style=""font-size: 16px; color: #333; margin: 0 0 20px 0;"">
                    Great news! Your return request <strong>#{request.Id}</strong> for order <strong>{request.OrderNumber}</strong> has been completed.
                </p>

                <div style=""background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 20px 0;"">
                    <p style=""margin: 0 0 5px 0; color: #2e7d32; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;"">Refund Amount</p>
                    <p style=""margin: 0; color: #1b5e20; font-size: 36px; font-weight: bold;"">{refundAmount:C}</p>
                    <p style=""margin: 10px 0 0 0; color: #388e3c; font-size: 14px;"">Refund has been initiated to your original payment method</p>
                </div>

                <div style=""background-color: #fff8e1; padding: 15px; border-radius: 8px; margin: 20px 0;"">
                    <p style=""margin: 0; color: #f57c00; font-size: 14px;"">
                        <strong>⏱ Processing Time:</strong> Please allow 5-10 business days for the refund to appear in your account,
                        depending on your payment provider.
                    </p>
                </div>

                <h3 style=""color: #333; border-bottom: 2px solid #eee; padding-bottom: 10px;"">Returned Items</h3>
                {itemsHtml}

                {(string.IsNullOrEmpty(request.AdminNotes) ? "" : $@"
                <div style=""margin-top: 20px; padding: 15px; background-color: #f5f5f5; border-radius: 8px;"">
                    <p style=""margin: 0; color: #666; font-size: 14px;""><strong>Note:</strong> {request.AdminNotes}</p>
                </div>")}
            </div>

            <!-- Footer -->
            <div style=""background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;"">
                <p style=""margin: 0 0 10px 0; color: #666; font-size: 14px;"">We hope to see you again soon!</p>
                <p style=""margin: 0; color: #999; font-size: 12px;"">{storeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildReceivedEmailBody(PortalReturnDetailDto request, string storeName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <!-- Header -->
            <div style=""background: linear-gradient(135deg, #2196f3 0%, #03a9f4 100%); padding: 30px; text-align: center;"">
                <h1 style=""color: white; margin: 0; font-size: 24px;"">📦 Return Received</h1>
            </div>

            <!-- Content -->
            <div style=""padding: 30px;"">
                <p style=""font-size: 16px; color: #333; margin: 0 0 20px 0;"">
                    We've received your return package for request <strong>#{request.Id}</strong> (Order {request.OrderNumber}).
                </p>

                <div style=""background-color: #e3f2fd; padding: 20px; border-radius: 8px;"">
                    <h3 style=""color: #1565c0; margin: 0 0 10px 0;"">What's Next?</h3>
                    <p style=""margin: 0; color: #333;"">
                        Our team will inspect the returned items and process your refund within 2-3 business days.
                        You'll receive another email once your refund has been issued.
                    </p>
                </div>

                <div style=""margin-top: 20px; padding: 15px; background-color: #f5f5f5; border-radius: 8px;"">
                    <p style=""margin: 0; color: #666; font-size: 14px;"">
                        <strong>Return ID:</strong> #{request.Id}<br>
                        <strong>Order:</strong> {request.OrderNumber}<br>
                        <strong>Items:</strong> {request.Items.Sum(i => i.Quantity)} item(s)
                    </p>
                </div>
            </div>

            <!-- Footer -->
            <div style=""background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;"">
                <p style=""margin: 0; color: #666; font-size: 14px;"">Thank you for your patience - {storeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildSubmittedEmailBody(PortalReturnDetailDto request, string storeName)
    {
        var itemsHtml = BuildItemsTableHtml(request);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
            <!-- Header -->
            <div style=""background: linear-gradient(135deg, #ff9800 0%, #ffc107 100%); padding: 30px; text-align: center;"">
                <h1 style=""color: white; margin: 0; font-size: 24px;"">📋 Return Request Received</h1>
            </div>

            <!-- Content -->
            <div style=""padding: 30px;"">
                <p style=""font-size: 16px; color: #333; margin: 0 0 20px 0;"">
                    We've received your return request <strong>#{request.Id}</strong> for order <strong>{request.OrderNumber}</strong>.
                </p>

                <div style=""background-color: #fff3e0; padding: 20px; border-radius: 8px; margin-bottom: 20px;"">
                    <h3 style=""color: #e65100; margin: 0 0 10px 0;"">⏳ What Happens Next?</h3>
                    <p style=""margin: 0; color: #333;"">
                        Our team will review your request within 1-2 business days. You'll receive an email
                        once we've made a decision, along with any next steps.
                    </p>
                </div>

                <div style=""background-color: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 20px;"">
                    <p style=""margin: 0; color: #333; font-size: 14px;"">
                        <strong>Request Type:</strong> {request.RequestType}<br>
                        <strong>Reason:</strong> {request.Reason}<br>
                        <strong>Preferred Resolution:</strong> {GetResolutionText(request.PreferredResolution)}
                    </p>
                </div>

                <h3 style=""color: #333; border-bottom: 2px solid #eee; padding-bottom: 10px;"">Items for Return</h3>
                {itemsHtml}

                <p style=""margin-top: 20px; color: #666; font-size: 14px;"">
                    You can check the status of your return request anytime by logging into your account.
                </p>
            </div>

            <!-- Footer -->
            <div style=""background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;"">
                <p style=""margin: 0; color: #666; font-size: 14px;"">Thank you for shopping with {storeName}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private static string BuildItemsTableHtml(PortalReturnDetailDto request)
    {
        var rows = string.Join("", request.Items.Select(item => $@"
            <tr>
                <td style=""padding: 12px; border-bottom: 1px solid #eee;"">
                    <div style=""display: flex; align-items: center;"">
                        {(string.IsNullOrEmpty(item.ImageUrl) ? "" : $@"<img src=""{item.ImageUrl}"" alt=""{item.Title}"" style=""width: 50px; height: 50px; object-fit: cover; border-radius: 6px; margin-right: 12px;"" />")}
                        <div>
                            <strong style=""color: #333;"">{item.Title}</strong>
                            {(string.IsNullOrEmpty(item.VariantTitle) ? "" : $@"<br><span style=""color: #666; font-size: 13px;"">{item.VariantTitle}</span>")}
                            <br><span style=""color: #999; font-size: 12px;"">Condition: {item.Condition}</span>
                        </div>
                    </div>
                </td>
                <td style=""padding: 12px; border-bottom: 1px solid #eee; text-align: center; color: #333;"">{item.Quantity}</td>
                <td style=""padding: 12px; border-bottom: 1px solid #eee; text-align: right; color: #333; font-weight: 500;"">{item.TotalPrice:C}</td>
            </tr>"));

        return $@"
            <table style=""width: 100%; border-collapse: collapse; margin-top: 15px;"">
                <thead>
                    <tr style=""background-color: #f9f9f9;"">
                        <th style=""padding: 12px; text-align: left; color: #666; font-weight: 600; font-size: 13px;"">Item</th>
                        <th style=""padding: 12px; text-align: center; color: #666; font-weight: 600; font-size: 13px;"">Qty</th>
                        <th style=""padding: 12px; text-align: right; color: #666; font-weight: 600; font-size: 13px;"">Value</th>
                    </tr>
                </thead>
                <tbody>
                    {rows}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan=""2"" style=""padding: 12px; text-align: right; font-weight: bold; color: #333;"">Total:</td>
                        <td style=""padding: 12px; text-align: right; font-weight: bold; color: #7c3aed; font-size: 18px;"">{request.Items.Sum(i => i.TotalPrice):C}</td>
                    </tr>
                </tfoot>
            </table>";
    }

    private static string GetResolutionText(string resolution) => resolution switch
    {
        "Refund" => "Full Refund",
        "StoreCredit" => "Store Credit",
        "Exchange" => "Exchange Item",
        _ => resolution
    };

    #endregion
}
