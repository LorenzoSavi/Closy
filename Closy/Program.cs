using Closy.Data;
using Closy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Closy.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// MANTIENI SOLO QUESTA configurazione del DbContext (usando SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// RIMUOVI la configurazione SQLite
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<IImageService, ImageService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddLogging();

builder.Services.AddScoped<IClothingItemService, ClothingItemService>();

// Aggiungi HttpClient per GeminiService
builder.Services.AddHttpClient<Closy.Services.IGeminiService, Closy.Services.GeminiService>();

// Aggiungi HttpClient
builder.Services.AddHttpClient();

// Configura le Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Account/Manage");
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Register");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
});

// Configurazione per i controller Web API
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        // Questo fa s� che ASP.NET Core cerchi i controller in tutto l'assembly
        // inclusi quelli nella cartella Modules e Controllers
        var assembly = typeof(Program).Assembly;
        manager.ApplicationParts.Add(new AssemblyPart(assembly));
    });

// Supporto per API Explorer
builder.Services.AddEndpointsApiExplorer();

// Rimuovi o commenta questa sezione se AddDefaultIdentity è già configurato per i cookie
/*
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
    });
*/

// Configura il cookie di autenticazione
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // Non reindirizzare automaticamente alla pagina di login per richieste AJAX o API
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("ModeratorPolicy", policy =>
        policy.RequireRole("Moderator", "Admin"));

    // Aggiungiamo una policy per le azioni sensibili
    options.AddPolicy("RegisteredUserPolicy", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Mappa i controller API
app.MapControllers();

// Mantieni questa mappatura per le Razor Pages
app.MapRazorPages();

// Aggiungi un reindirizzamento esplicito dalla root alla pagina di login
app.MapGet("/", context => {
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Crea i ruoli
        await CreateRolesAsync(roleManager);

        // Crea l'utente admin
        await CreateAdminUserAsync(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Si � verificato un errore durante l'inizializzazione del db.");
    }
}


app.Run();

// Metodi di supporto per l'inizializzazione
async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Admin", "Moderator", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@closy.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nome = "Admin",
            Cognome = "closy",
            DataRegistrazione = DateTime.UtcNow,
            EmailConfirmed = true,
            ProfilePictureUrl = "/images/default-avatar.png"
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}