using Algora.Chatbot.Infrastructure;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Add Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS for widget
builder.Services.AddCors(options =>
{
    options.AddPolicy("WidgetPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatbotDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
