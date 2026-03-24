using Microsoft.OpenApi.Models;
using Project_Group3.Hubs;

namespace Project_Group3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.   
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddControllersWithViews();

            builder.Services.AddSignalR();

            builder.Services.AddSession();  
            builder.Services.AddSignalR();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Project_Group3 API", Version = "v1" });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project_Group3 API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSession();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapHub<AdminNotificationHub>("/hubs/admin-notifications");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapHub<NotificationHub>("/notificationHub");
            app.Run();
        }
    }
}