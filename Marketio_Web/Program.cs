using Marketio_Shared.Entities;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
using Marketio_Web;
using Marketio_Web.Data;
using Marketio_Web.Localization;
using Marketio_Web.Middleware;
using Marketio_Web.Models;
using Marketio_Web.Repositories;
using Marketio_Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Voeg services toe aan de container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//  Session support voor shopping cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//  HttpContextAccessor voor cart service
builder.Services.AddHttpContextAccessor();

//  Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

//  Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
// JWT Token Service
builder.Services.AddScoped<JwtTokenService>();

// GDPR Compliance Service
builder.Services.AddScoped<IGdprAuditService, GdprAuditService>();

// Email Service (implementeer IEmailSender)
builder.Services.AddTransient<IEmailSender, EmailSenderService>();

// Localization Services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("nl"),
        new CultureInfo("en"),
        new CultureInfo("fr")
    };

    options.DefaultRequestCulture = new RequestCulture("nl");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

//  Identity mit ApplicationUser en Rollen
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Email Verification
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;

    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Account Lockout Configuration
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

// ✅ JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not found in configuration.");

builder.Services.AddAuthentication(options =>
{
    // Default schema blijft Cookies voor Razor Pages
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Event handlers voor debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userName = context.Principal?.Identity?.Name ?? "Unknown";
            logger.LogInformation("JWT Token validated for user: {UserName}", userName);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge triggered: {Error} - {ErrorDescription}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResources));
    });

// ✅ Swagger/OpenAPI Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Marketio API",
        Version = "v1",
        Description = "REST API voor Marketio e-commerce platform met JWT authenticatie",
        Contact = new OpenApiContact
        {
            Name = "Marketio Support",
            Email = "support@marketio.nl",
            Url = new Uri("https://github.com/SoufianeAbk/Marketio")
        }
    });

    // JWT Authentication in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Voer je JWT token in. Voorbeeld: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Lees XML comments voor betere documentatie (optioneel)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Ensure database is created and apply migrations
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding roles and admin user...");
        await SeedRolesAndAdminAsync(services);
        logger.LogInformation("Roles and admin user seeded successfully.");

        logger.LogInformation("Seeding products...");
        await SeedProductsAsync(services);
        logger.LogInformation("Products seeded successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// Configureer de HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // Enable Swagger in Development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketio API v1");
        options.RoutePrefix = "api-docs";
        options.DocumentTitle = "Marketio API Documentation";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCookieManagement();
app.UseRequestLocalization();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Manager", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@marketio.nl";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "Marketio",
            Address = "Hoofdstraat 1, 1000 AA Amsterdam"
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

async Task SeedProductsAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

    if (await context.Products.AnyAsync())
        return; // Al geseed

    var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    context.Products.AddRange(
        new Product { Name = "Laptop Dell XPS 15", Description = "Krachtige laptop met 16GB RAM en 512GB SSD", Price = 1299.99m, Stock = 15, Category = ProductCategory.Electronics, ImageUrl = "/images/laptop.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "iPhone 15 Pro", Description = "Nieuwste iPhone met A17 Pro chip", Price = 1099.00m, Stock = 25, Category = ProductCategory.Electronics, ImageUrl = "/images/iphone.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Samsung 4K TV 55\"", Description = "Crystal UHD 4K Smart TV", Price = 699.99m, Stock = 10, Category = ProductCategory.Electronics, ImageUrl = "/images/tv.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Sony WH-1000XM5", Description = "Noise cancelling koptelefoon", Price = 349.99m, Stock = 30, Category = ProductCategory.Electronics, ImageUrl = "/images/headphones.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Nike Air Max Sneakers", Description = "Comfortabele sportschoenen", Price = 129.99m, Stock = 50, Category = ProductCategory.Clothing, ImageUrl = "/images/sneakers.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Levi's 501 Jeans", Description = "Klassieke straight fit jeans", Price = 89.99m, Stock = 40, Category = ProductCategory.Clothing, ImageUrl = "/images/jeans.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Adidas Hoodie", Description = "Warme hoodie met logo", Price = 59.99m, Stock = 35, Category = ProductCategory.Clothing, ImageUrl = "/images/hoodie.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Tommy Hilfiger Polo", Description = "Katoenen polo shirt", Price = 69.99m, Stock = 45, Category = ProductCategory.Clothing, ImageUrl = "/images/polo.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Clean Code - Robert Martin", Description = "Handbook of Agile Software Craftsmanship", Price = 39.99m, Stock = 60, Category = ProductCategory.Books, ImageUrl = "/images/cleancode.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "The Pragmatic Programmer", Description = "From Journeyman to Master", Price = 44.99m, Stock = 55, Category = ProductCategory.Books, ImageUrl = "/images/pragmatic.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Design Patterns", Description = "Elements of Reusable Object-Oriented Software", Price = 49.99m, Stock = 40, Category = ProductCategory.Books, ImageUrl = "/images/patterns.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Harry Potter Box Set", Description = "Complete serie van 7 boeken", Price = 89.99m, Stock = 25, Category = ProductCategory.Books, ImageUrl = "/images/harrypotter.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Dyson V15 Stofzuiger", Description = "Draadloze stofzuiger met laser", Price = 599.99m, Stock = 15, Category = ProductCategory.HomeAndGarden, ImageUrl = "/images/vacuum.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Philips Airfryer XXL", Description = "Hetelucht friteuse 7.3L", Price = 249.99m, Stock = 20, Category = ProductCategory.HomeAndGarden, ImageUrl = "/images/airfryer.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "IKEA Bureau BEKANT", Description = "Verstelbaar bureau 160x80cm", Price = 349.00m, Stock = 12, Category = ProductCategory.HomeAndGarden, ImageUrl = "/images/desk.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Nespresso Machine", Description = "Koffiemachine met melkopschuimer", Price = 199.99m, Stock = 30, Category = ProductCategory.HomeAndGarden, ImageUrl = "/images/nespresso.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Yoga Mat Premium", Description = "Extra dikke yoga mat 6mm", Price = 39.99m, Stock = 50, Category = ProductCategory.Sports, ImageUrl = "/images/yogamat.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Dumbbells Set 20kg", Description = "Verstelbare dumbbell set", Price = 149.99m, Stock = 25, Category = ProductCategory.Sports, ImageUrl = "/images/dumbbells.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Garmin Forerunner 265", Description = "GPS hardloop smartwatch", Price = 449.99m, Stock = 18, Category = ProductCategory.Sports, ImageUrl = "/images/garmin.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
        new Product { Name = "Voetbal Nike Strike", Description = "Officiële wedstrijdbal", Price = 29.99m, Stock = 40, Category = ProductCategory.Sports, ImageUrl = "/images/football.jpg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate }
    );

    await context.SaveChangesAsync();
}