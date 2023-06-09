using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Configuration;
using UdemyRabbitMQWeb.FileCreateWorkerService.Hubs;
using UdemyRabbitMQWeb.FileCreateWorkerService.Models;
using UdemyRabbitMQWeb.FileCreateWorkerService.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddSignalR();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("SqlSever"));
        });

        builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
        {
            opt.User.RequireUniqueEmail = true;

        }).AddEntityFrameworkStores<AppDbContext>();

        builder.Services.AddSingleton(sp => new ConnectionFactory() { Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")), DispatchConsumersAsync = true });
        builder.Services.AddSingleton<RabbitMQPublisher>();
        builder.Services.AddSingleton<RabbitMQClientService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            appDbContext.Database.Migrate();

            if (!appDbContext.Users.Any())
            {
                userManager.CreateAsync(new IdentityUser() { UserName = "deneme", Email = "deneme@outlook.com" }, "Password12*").Wait();
                userManager.CreateAsync(new IdentityUser() { UserName = "deneme2", Email = "deneme2@outlook.com" }, "Password12*").Wait();
            }
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapHub<MyHub>("/MyHub");
        app.Run();
    }
}