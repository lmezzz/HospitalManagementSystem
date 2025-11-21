using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem;
var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<HmsContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.

builder.Services.AddAuthentication("LoginAuthCookie")
    .AddCookie("LoginAuthCookie", options =>
    {
        options.AccessDeniedPath = "/Account/Login"; // Redirect here if access is denied need to make a denied page and change the path to "Account/Denied"
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "LoginAuthCookie";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true; //sliding expiration means that if the user is active the cookie expiration time will be extended
    });

builder.Services.AddAuthorization();  //enabhles us to use [Authorize] attribute in controllers
builder.Services.AddControllersWithViews();


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

app.UseAuthentication(); //when this is called it will check for the auth cookie in the request and like loads the addcookie method we just defined ooper 
app.UseAuthorization();  // this will check if the user is authorized to access the resource

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
