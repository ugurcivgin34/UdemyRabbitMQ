using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Configuration;
using UdemyRabbitMQWeb.Watermark.Models;
using UdemyRabbitMQWeb.Watermark.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ConnectionFactory>(sp =>
{
    var rabbitMQSection = builder.Configuration.GetSection("RabbitMQ");
    var uri = rabbitMQSection.GetValue<Uri>("Uri");

    return new ConnectionFactory() { Uri = uri };
});


builder.Services.AddSingleton<RabbitMQClientService>();


builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase(databaseName: "productDb");

});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
