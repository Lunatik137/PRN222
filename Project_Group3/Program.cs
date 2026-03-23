
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;

namespace PRN222_Group3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            DotNetEnv.Env.Load();

            // Add services to the container
            builder.Services.AddDbContext<CloneEbayDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionStringDB")));

            // Configure Data Protection for load balanced environment
            // All instances must share the same keys for cookies/sessions to work across instances
            var dataProtectionPath = builder.Environment.IsDevelopment()
                ? Path.Combine(Directory.GetCurrentDirectory(), "keys")
                : "/app/keys"; // Docker container path

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
                .SetApplicationName("PRN222_Group3_App");

            builder.Services.AddControllersWithViews();

            // Configure Redis for distributed session storage in load-balanced environment
            var redisConnection = builder.Configuration.GetValue<string>("REDIS_CONNECTION")
                ?? builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379";

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "PRN222_Session_";
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
                options.Cookie.HttpOnly = true; // Set the HttpOnly flag for security
                options.Cookie.IsEssential = true; // Make the session cookie essential
                options.Cookie.Name = ".PRN222.Session"; // Consistent session cookie name
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
            // 2. Đăng ký Repository (để "tiêm" vào Controller)
            builder.Services.AddScoped<StoresRepository>();
            builder.Services.AddScoped<ReturnRequestAdminRepository>();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opt =>
            {
                opt.LoginPath = "/Login/Login";
                //opt.LogoutPath = "/Login/Logout";
                //opt.AccessDeniedPath = "/Home/Denied";  
                opt.SlidingExpiration = true;
                opt.ExpireTimeSpan = TimeSpan.FromHours(12);
            });

            builder.Services.AddAuthorization(opts =>
            {
                opts.AddPolicy("UserManageRead", p => p.RequireRole("SuperAdmin", "Moderator", "Monitor", "Support", "Ops"));
                opts.AddPolicy("UserManageWrite", p => p.RequireRole("SuperAdmin", "Moderator"));
                opts.AddPolicy("ReturnAndSystemNotify", p => p.RequireRole("SuperAdmin", "Moderator", "Monitor", "Support", "Ops"));
            });

            // Dependency Injection registrations
            builder.Services.AddScoped<PRN222_Group3.Repository.UserRepository>();
            builder.Services.AddScoped<PRN222_Group3.Service.StoreStatsService>();
            builder.Services.AddScoped<PRN222_Group3.Service.IUserService, PRN222_Group3.Service.UserService>();
            builder.Services.AddScoped<PRN222_Group3.Service.ITwoFactorAuthService, PRN222_Group3.Service.TwoFactorAuthService>();
            builder.Services.AddScoped<PRN222_Group3.Service.IEmailService, PRN222_Group3.Service.EmailService>();
            builder.Services.AddScoped<PRN222_Group3.Service.RiskScoringService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }




            app.UseHttpsRedirection();
            app.UseRouting();

            // Session must be BEFORE Authentication/Authorization
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
              name: "default",
              pattern: "{controller=Login}/{action=Login}/{id?}")
              .WithStaticAssets();

            app.Run();
        }
    }
}
