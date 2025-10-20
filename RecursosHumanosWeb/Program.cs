using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecursosHumanosWeb.Controllers;
using RecursosHumanosWeb.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuthenticationDataAttribute>();
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddDbContext<RecursosHumanosContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("RecursosHumanosContext"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exception != null)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(exception.Error, "Excepción no manejada: {Message}", exception.Error.Message);
                await context.Response.WriteAsync($"Error: {exception.Error.Message}");
            }
        });
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogDebug("Procesando solicitud: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next(context);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();