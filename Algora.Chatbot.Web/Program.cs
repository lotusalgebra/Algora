using Algora.Chatbot.Infrastructure;
using Algora.Chatbot.Infrastructure.Data;
using Algora.Chatbot.Web.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

// Add Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS for widget and SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("WidgetPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("WidgetPolicy");
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireCors("SignalRPolicy");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatbotDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
