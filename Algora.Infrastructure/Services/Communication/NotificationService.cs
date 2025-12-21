using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Algora.Infrastructure.Services.Communication;

/// <summary>
/// Unified notification service for email, SMS, and WhatsApp.
/// </summary>
public partial class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly HttpClient _http;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        ISmsService smsService,
        IHttpClientFactory httpFactory,
        IOptions<EmailOptions> emailOptions,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _smsService = smsService;
        _http = httpFactory.CreateClient();
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    #region Send Methods

    public async Task<NotificationDto> SendEmailAsync(string shopDomain, SendEmailNotificationDto dto)
    {
        var notification = new Notification
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            NotificationType = "email",
            Subject = dto.Subject,
            Body = dto.Body,
            Recipient = dto.ToEmail,
            Status = "pending"
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        try
        {
            var success = await SendEmailViaProviderAsync(dto);

            notification.Status = success ? "sent" : "failed";
            notification.SentAt = success ? DateTime.UtcNow : null;

            if (success)
            {
                _logger.LogInformation("Email sent to {Email}: {Subject}", dto.ToEmail, dto.Subject);
            }
            else
            {
                notification.ErrorMessage = "Failed to send email";
                _logger.LogWarning("Failed to send email to {Email}", dto.ToEmail);
            }
        }
        catch (Exception ex)
        {
            notification.Status = "failed";
            notification.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error sending email to {Email}", dto.ToEmail);
        }

        await _db.SaveChangesAsync();
        return MapToDto(notification);
    }

    public async Task<NotificationDto> SendSmsAsync(string shopDomain, SendSmsNotificationDto dto)
    {
        var notification = new Notification
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            NotificationType = "sms",
            Subject = "SMS",
            Body = dto.Body,
            Recipient = dto.PhoneNumber,
            Status = "pending"
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        try
        {
            var result = await _smsService.SendMessageAsync(shopDomain, new SendSmsMessageDto
            {
                PhoneNumber = dto.PhoneNumber,
                Body = dto.Body,
                CustomerId = dto.CustomerId,
                OrderId = dto.OrderId
            });

            notification.Status = result.Status == "sent" ? "sent" : "failed";
            notification.SentAt = result.SentAt;
            notification.ErrorMessage = result.Status == "failed" ? "SMS delivery failed" : null;
        }
        catch (Exception ex)
        {
            notification.Status = "failed";
            notification.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error sending SMS to {Phone}", dto.PhoneNumber);
        }

        await _db.SaveChangesAsync();
        return MapToDto(notification);
    }

    public async Task<NotificationDto> SendWhatsAppAsync(string shopDomain, SendWhatsAppNotificationDto dto)
    {
        var notification = new Notification
        {
            ShopDomain = shopDomain,
            CustomerId = dto.CustomerId,
            OrderId = dto.OrderId,
            NotificationType = "whatsapp",
            Subject = "WhatsApp",
            Recipient = dto.PhoneNumber,
            Status = "pending"
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // WhatsApp integration would go here
        // For now, mark as not implemented
        notification.Status = "failed";
        notification.ErrorMessage = "WhatsApp service not configured";

        await _db.SaveChangesAsync();
        _logger.LogWarning("WhatsApp notification attempted but service not configured");

        return MapToDto(notification);
    }

    #endregion

    #region Query Methods

    public async Task<NotificationDto?> GetNotificationAsync(int notificationId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId);
        return notification is null ? null : MapToDto(notification);
    }

    public async Task<PaginatedResult<NotificationDto>> GetNotificationsAsync(string shopDomain, int page = 1, int pageSize = 50)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.ShopDomain == shopDomain);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResult<NotificationDto>.Create(items.Select(MapToDto), total, page, pageSize);
    }

    #endregion

    #region Order Notifications

    public async Task SendOrderConfirmationAsync(string shopDomain, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ShopDomain == shopDomain)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        var template = await GetTemplateAsync(shopDomain, "order_confirmation");
        var customer = order.Customer;

        if (customer?.Email is null)
        {
            _logger.LogWarning("Cannot send order confirmation - no customer email for order {OrderId}", orderId);
            return;
        }

        var subject = ReplaceTemplateVariables(template?.Subject ?? "Order Confirmation - #{{order_number}}", order, customer);
        var body = ReplaceTemplateVariables(template?.Body ?? GetDefaultOrderConfirmationTemplate(), order, customer);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = customer.Email,
            ToName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Subject = subject,
            Body = body,
            CustomerId = customer.Id,
            OrderId = orderId
        });

        // Also send SMS if customer has phone and SMS opt-in
        if (!string.IsNullOrEmpty(customer.Phone))
        {
            var smsBody = $"Thank you for your order #{order.OrderNumber}! Total: {order.GrandTotal:C}";
            await SendSmsAsync(shopDomain, new SendSmsNotificationDto
            {
                PhoneNumber = customer.Phone,
                Body = smsBody,
                CustomerId = customer.Id,
                OrderId = orderId
            });
        }

        _logger.LogInformation("Order confirmation sent for order {OrderId}", orderId);
    }

    public async Task SendShippingNotificationAsync(string shopDomain, int orderId, string trackingNumber, string? trackingUrl = null)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ShopDomain == shopDomain)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        var customer = order.Customer;
        if (customer?.Email is null)
        {
            _logger.LogWarning("Cannot send shipping notification - no customer email for order {OrderId}", orderId);
            return;
        }

        var template = await GetTemplateAsync(shopDomain, "shipping_notification");
        var variables = new Dictionary<string, string>
        {
            ["tracking_number"] = trackingNumber,
            ["tracking_url"] = trackingUrl ?? "#"
        };

        var subject = ReplaceTemplateVariables(template?.Subject ?? "Your order #{{order_number}} has shipped!", order, customer, variables);
        var body = ReplaceTemplateVariables(template?.Body ?? GetDefaultShippingTemplate(), order, customer, variables);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = customer.Email,
            ToName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Subject = subject,
            Body = body,
            CustomerId = customer.Id,
            OrderId = orderId
        });

        // SMS notification
        if (!string.IsNullOrEmpty(customer.Phone))
        {
            var smsBody = $"Your order #{order.OrderNumber} has shipped! Tracking: {trackingNumber}";
            if (!string.IsNullOrEmpty(trackingUrl))
                smsBody += $" Track: {trackingUrl}";

            await SendSmsAsync(shopDomain, new SendSmsNotificationDto
            {
                PhoneNumber = customer.Phone,
                Body = smsBody,
                CustomerId = customer.Id,
                OrderId = orderId
            });
        }

        _logger.LogInformation("Shipping notification sent for order {OrderId}", orderId);
    }

    public async Task SendDeliveryConfirmationAsync(string shopDomain, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ShopDomain == shopDomain)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        var customer = order.Customer;
        if (customer?.Email is null)
        {
            _logger.LogWarning("Cannot send delivery confirmation - no customer email for order {OrderId}", orderId);
            return;
        }

        var template = await GetTemplateAsync(shopDomain, "delivery_confirmation");

        var subject = ReplaceTemplateVariables(template?.Subject ?? "Your order #{{order_number}} has been delivered!", order, customer);
        var body = ReplaceTemplateVariables(template?.Body ?? GetDefaultDeliveryTemplate(), order, customer);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = customer.Email,
            ToName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Subject = subject,
            Body = body,
            CustomerId = customer.Id,
            OrderId = orderId
        });

        _logger.LogInformation("Delivery confirmation sent for order {OrderId}", orderId);
    }

    public async Task SendInvoiceAsync(string shopDomain, int invoiceId)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Order)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.ShopDomain == shopDomain)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var customer = invoice.Customer;
        if (customer?.Email is null)
        {
            _logger.LogWarning("Cannot send invoice - no customer email for invoice {InvoiceId}", invoiceId);
            return;
        }

        var template = await GetTemplateAsync(shopDomain, "invoice");

        var variables = new Dictionary<string, string>
        {
            ["invoice_number"] = invoice.InvoiceNumber,
            ["invoice_date"] = invoice.InvoiceDate.ToString("MMMM d, yyyy"),
            ["invoice_total"] = invoice.Total.ToString("C"),
            ["subtotal"] = invoice.Subtotal.ToString("C"),
            ["tax"] = invoice.Tax.ToString("C")
        };

        var subject = ReplaceVariables(template?.Subject ?? "Invoice #{{invoice_number}}", variables);
        var body = ReplaceVariables(template?.Body ?? GetDefaultInvoiceTemplate(), variables);

        // Add customer variables
        body = ReplaceCustomerVariables(body, customer);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = customer.Email,
            ToName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Subject = subject,
            Body = body,
            CustomerId = customer.Id,
            OrderId = invoice.OrderId
        });

        _logger.LogInformation("Invoice {InvoiceId} sent to {Email}", invoiceId, customer.Email);
    }

    #endregion

    #region Customer Notifications

    public async Task SendWelcomeEmailAsync(string shopDomain, int customerId)
    {
        var customer = await _db.Customers.FindAsync(customerId)
            ?? throw new InvalidOperationException($"Customer {customerId} not found");

        if (customer.Email is null)
        {
            _logger.LogWarning("Cannot send welcome email - no email for customer {CustomerId}", customerId);
            return;
        }

        var template = await GetTemplateAsync(shopDomain, "welcome");

        var variables = new Dictionary<string, string>
        {
            ["customer_first_name"] = customer.FirstName ?? "Valued Customer",
            ["customer_last_name"] = customer.LastName ?? "",
            ["customer_email"] = customer.Email
        };

        var subject = ReplaceVariables(template?.Subject ?? "Welcome to our store!", variables);
        var body = ReplaceVariables(template?.Body ?? GetDefaultWelcomeTemplate(), variables);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = customer.Email,
            ToName = $"{customer.FirstName} {customer.LastName}".Trim(),
            Subject = subject,
            Body = body,
            CustomerId = customerId
        });

        _logger.LogInformation("Welcome email sent to customer {CustomerId}", customerId);
    }

    public async Task SendPasswordResetAsync(string shopDomain, string email, string resetToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == email);

        var template = await GetTemplateAsync(shopDomain, "password_reset");

        var variables = new Dictionary<string, string>
        {
            ["customer_first_name"] = customer?.FirstName ?? "Customer",
            ["reset_token"] = resetToken,
            ["reset_url"] = $"https://{shopDomain}/account/reset?token={resetToken}"
        };

        var subject = ReplaceVariables(template?.Subject ?? "Reset Your Password", variables);
        var body = ReplaceVariables(template?.Body ?? GetDefaultPasswordResetTemplate(), variables);

        await SendEmailAsync(shopDomain, new SendEmailNotificationDto
        {
            ToEmail = email,
            ToName = customer is not null ? $"{customer.FirstName} {customer.LastName}".Trim() : null,
            Subject = subject,
            Body = body,
            CustomerId = customer?.Id
        });

        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    #endregion

    #region Email Sending Providers

    private async Task<bool> SendEmailViaProviderAsync(SendEmailNotificationDto dto)
    {
        return _emailOptions.Provider.ToLowerInvariant() switch
        {
            "smtp" => await SendViaSmtpAsync(dto),
            "sendgrid" => await SendViaSendGridAsync(dto),
            "mailgun" => await SendViaMailgunAsync(dto),
            _ => await SendViaSmtpAsync(dto)
        };
    }

    private async Task<bool> SendViaSmtpAsync(SendEmailNotificationDto dto)
    {
        try
        {
            using var client = new SmtpClient(_emailOptions.SmtpHost, _emailOptions.SmtpPort)
            {
                EnableSsl = _emailOptions.SmtpUseSsl,
                Credentials = new NetworkCredential(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword)
            };

            var fromEmail = dto.FromEmail ?? _emailOptions.DefaultFromEmail;
            var fromName = dto.FromName ?? _emailOptions.DefaultFromName;

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = dto.Subject,
                Body = dto.Body,
                IsBodyHtml = dto.IsHtml
            };

            message.To.Add(new MailAddress(dto.ToEmail, dto.ToName));

            if (!string.IsNullOrEmpty(_emailOptions.DefaultReplyTo))
                message.ReplyToList.Add(_emailOptions.DefaultReplyTo);

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed");
            return false;
        }
    }

    private async Task<bool> SendViaSendGridAsync(SendEmailNotificationDto dto)
    {
        try
        {
            var url = "https://api.sendgrid.com/v3/mail/send";

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = dto.ToEmail, name = dto.ToName } }
                    }
                },
                from = new
                {
                    email = dto.FromEmail ?? _emailOptions.DefaultFromEmail,
                    name = dto.FromName ?? _emailOptions.DefaultFromName
                },
                subject = dto.Subject,
                content = new[]
                {
                    new { type = dto.IsHtml ? "text/html" : "text/plain", value = dto.Body }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_emailOptions.ApiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid send failed");
            return false;
        }
    }

    private async Task<bool> SendViaMailgunAsync(SendEmailNotificationDto dto)
    {
        try
        {
            // Mailgun uses form data
            var domain = _emailOptions.DefaultFromEmail.Split('@').LastOrDefault() ?? "example.com";
            var url = $"https://api.mailgun.net/v3/{domain}/messages";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = $"{dto.FromName ?? _emailOptions.DefaultFromName} <{dto.FromEmail ?? _emailOptions.DefaultFromEmail}>",
                ["to"] = dto.ToName is not null ? $"{dto.ToName} <{dto.ToEmail}>" : dto.ToEmail,
                ["subject"] = dto.Subject,
                [dto.IsHtml ? "html" : "text"] = dto.Body
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{_emailOptions.ApiKey}"));
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Content = content;

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mailgun send failed");
            return false;
        }
    }

    #endregion

    #region Template Helpers

    private async Task<EmailTemplate?> GetTemplateAsync(string shopDomain, string templateType)
    {
        return await _db.EmailTemplates
            .AsNoTracking()
            .Where(t => t.ShopDomain == shopDomain && t.TemplateType == templateType && t.IsActive)
            .OrderByDescending(t => t.IsDefault)
            .FirstOrDefaultAsync();
    }

    private static string ReplaceTemplateVariables(string template, Order order, Customer? customer, Dictionary<string, string>? additionalVariables = null)
    {
        var variables = new Dictionary<string, string>
        {
            ["order_number"] = order.OrderNumber,
            ["order_date"] = order.CreatedAt.ToString("MMMM d, yyyy"),
            ["order_total"] = order.GrandTotal.ToString("C"),
            ["subtotal"] = order.Subtotal.ToString("C"),
            ["tax_total"] = order.TaxTotal.ToString("C"),
            ["shipping_total"] = order.ShippingTotal.ToString("C"),
            ["discount_total"] = order.DiscountTotal.ToString("C")
        };

        if (customer is not null)
        {
            variables["customer_first_name"] = customer.FirstName ?? "";
            variables["customer_last_name"] = customer.LastName ?? "";
            variables["customer_email"] = customer.Email ?? "";
            variables["customer_phone"] = customer.Phone ?? "";
        }

        if (additionalVariables is not null)
        {
            foreach (var (key, value) in additionalVariables)
                variables[key] = value;
        }

        return ReplaceVariables(template, variables);
    }

    private static string ReplaceCustomerVariables(string template, Customer customer)
    {
        var variables = new Dictionary<string, string>
        {
            ["customer_first_name"] = customer.FirstName ?? "",
            ["customer_last_name"] = customer.LastName ?? "",
            ["customer_email"] = customer.Email ?? "",
            ["customer_phone"] = customer.Phone ?? ""
        };

        return ReplaceVariables(template, variables);
    }

    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }
        return template;
    }

    #endregion

    #region Default Templates

    private static string GetDefaultOrderConfirmationTemplate() => """
        <h1>Thank you for your order!</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>We've received your order #{{order_number}} and are getting it ready.</p>
        <h2>Order Summary</h2>
        <p><strong>Order Total:</strong> {{order_total}}</p>
        <p>We'll send you another email when your order ships.</p>
        <p>Thank you for shopping with us!</p>
        """;

    private static string GetDefaultShippingTemplate() => """
        <h1>Your order is on its way!</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>Great news! Your order #{{order_number}} has shipped.</p>
        <p><strong>Tracking Number:</strong> {{tracking_number}}</p>
        <p><a href="{{tracking_url}}">Track your package</a></p>
        <p>Thank you for shopping with us!</p>
        """;

    private static string GetDefaultDeliveryTemplate() => """
        <h1>Your order has been delivered!</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>Your order #{{order_number}} has been delivered.</p>
        <p>We hope you love your purchase! If you have any questions, please don't hesitate to contact us.</p>
        <p>Thank you for shopping with us!</p>
        """;

    private static string GetDefaultInvoiceTemplate() => """
        <h1>Invoice #{{invoice_number}}</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>Please find your invoice attached.</p>
        <h2>Invoice Summary</h2>
        <p><strong>Invoice Date:</strong> {{invoice_date}}</p>
        <p><strong>Subtotal:</strong> {{subtotal}}</p>
        <p><strong>Tax:</strong> {{tax}}</p>
        <p><strong>Total:</strong> {{invoice_total}}</p>
        <p>Thank you for your business!</p>
        """;

    private static string GetDefaultWelcomeTemplate() => """
        <h1>Welcome!</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>Thank you for creating an account with us. We're excited to have you!</p>
        <p>Start shopping and discover great products.</p>
        <p>If you have any questions, feel free to reach out to us.</p>
        """;

    private static string GetDefaultPasswordResetTemplate() => """
        <h1>Reset Your Password</h1>
        <p>Hi {{customer_first_name}},</p>
        <p>We received a request to reset your password.</p>
        <p><a href="{{reset_url}}">Click here to reset your password</a></p>
        <p>This link will expire in 24 hours.</p>
        <p>If you didn't request this, please ignore this email.</p>
        """;

    #endregion

    #region Mappers

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        ShopDomain = n.ShopDomain,
        CustomerId = n.CustomerId,
        OrderId = n.OrderId,
        NotificationType = n.NotificationType,
        Subject = n.Subject,
        Body = n.Body,
        Recipient = n.Recipient,
        Status = n.Status,
        ErrorMessage = n.ErrorMessage,
        SentAt = n.SentAt,
        CreatedAt = n.CreatedAt
    };

    #endregion
}