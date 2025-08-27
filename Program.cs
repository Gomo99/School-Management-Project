using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Service;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TwoFactorAuthService>();


builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "MyCookieAuth";
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "SchoolProject.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

});


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



var app = builder.Build();

// Optional logging
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("App is shutting down. Cookies will become invalid.");
});

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
