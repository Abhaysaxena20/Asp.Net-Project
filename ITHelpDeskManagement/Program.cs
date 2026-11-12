
using ITHelpDeskManagement.Data;
using ITHelpDeskManagement.Hubs;
using ITHelpDeskManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace ITHelpDeskManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();

            // MVC services
            builder.Services.AddControllersWithViews();

            // SQL Server database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("newconnection")));

            // ASP.NET Core Identity
            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";

                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
            });

            // Login Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("LoginPolicy", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?
                                        .ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(45),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                options.RejectionStatusCode = 429;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var retryAfter = 45;

                    if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out TimeSpan retryTime))
                    {
                        retryAfter = Math.Max(
                            1,
                            (int)Math.Ceiling(retryTime.TotalSeconds)
                        );
                    }

                    var returnUrl = context.HttpContext.Request
                        .Query["returnUrl"].ToString();

                    var loginUrl = "/Account/Login?rateLimited=true"
                                 + "&retryAfter=" + retryAfter;

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        loginUrl += "&returnUrl=" +
                            Uri.EscapeDataString(returnUrl);
                    }

                    context.HttpContext.Response.Redirect(loginUrl);
                };
            });

            var app = builder.Build();

            app.MapHub<TicketHub>("/ticketHub");

            // Create roles and configure Admin account
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var roleManager =
                    services.GetRequiredService<RoleManager<IdentityRole>>();

                var userManager =
                    services.GetRequiredService<UserManager<ApplicationUser>>();

                var configuration =
                    services.GetRequiredService<IConfiguration>();

                string[] roles =
                {
                    "Admin",
                    "User",
                    "SupportStaff"
                };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(
                            new IdentityRole(role));

                        if (!result.Succeeded)
                        {
                            throw new Exception(
                                $"Could not create role {role}: " +
                                string.Join(", ",
                                    result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                // Assign Admin role to configured account
                var adminEmail = configuration["AdminSetup:Email"];

                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    var adminUser =
                        await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser != null &&
                        !await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        var result = await userManager.AddToRoleAsync(
                            adminUser, "Admin");

                        if (!result.Succeeded)
                        {
                            throw new Exception(
                                "Could not assign Admin role: " +
                                string.Join(", ",
                                    result.Errors.Select(e => e.Description)));
                        }
                    }
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseRateLimiter();

            // Authentication before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
