using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Http;

public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;

    public LicenseValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService, IShopContext shopContext)
    {
        if (context.Request.Path.StartsWithSegments("/admin") ||
            context.Request.Path.StartsWithSegments("/api"))
        {
            var license = await licenseService.GetLicenseAsync(shopContext.ShopDomain);
            if (license == null || !license.IsActive || license.ExpiryDate < DateTime.UtcNow)
            {
                context.Response.Redirect("/licensing/subscribe");
                return;
            }
        }

        await _next(context);
    }
}
