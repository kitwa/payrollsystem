using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Payroll.Infrastructure;
using Payroll.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Payroll.Application.Auth.Commands.LoginCommand).Assembly);
        cfg.AddOpenBehavior(typeof(Payroll.Application.Common.Behaviours.ValidationBehaviour<,>));
        cfg.AddOpenBehavior(typeof(Payroll.Application.Common.Behaviours.LoggingBehaviour<,>));
    });

    builder.Services.AddControllers();
    builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", p =>
        p.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200")));

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payroll SA API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT. Example: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // Auto-migrate and seed on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Payroll.Domain.Identity.AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Payroll.Domain.Identity.AppRole>>();
        await db.Database.MigrateAsync();
        await Seed.SeedAsync(db, userManager, roleManager);
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<Payroll.Api.Middleware.ExceptionMiddleware>();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payroll SA v1"));
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new Payroll.Api.Middleware.HangfireAuthFilter()]
    });
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
