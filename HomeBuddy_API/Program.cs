using Azure.AI.OpenAI;
using HomeBuddy_API.Data;
using HomeBuddy_API.Exceptions;
using HomeBuddy_API.Interfaces;
using HomeBuddy_API.Interfaces.AdminInterfaces;
using HomeBuddy_API.Interfaces.AuthInterfaces;
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Interfaces.ReviewInterfaces;
using HomeBuddy_API.Interfaces.UserInterfaces;
using HomeBuddy_API.Repositories;
using HomeBuddy_API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

namespace HomeBuddy_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Log configuration for debugging
            Console.WriteLine("=== Starting HomeBuddy API ===");
            Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

            try
            {
                // Add DbContext
                var connString = builder.Configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty!");
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connString));

                // Dependency Injection for Repositories and Services
                builder.Services.AddScoped<IOrderRepository, OrderRepository>();
                builder.Services.AddScoped<IOrderService, OrderService>();
                builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
                builder.Services.AddScoped<IReviewService, ReviewService>();
                builder.Services.AddScoped<IInventoryService, InventoryService>();
                builder.Services.AddScoped<IProductGroupRepository, ProductGroupRepository>();

                builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
                builder.Services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
                builder.Services.AddScoped<IVariantRepository, VariantRepository>();
                builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                builder.Services.AddScoped<IAuthRepository, AuthRepository>();
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<IUserRepository, UserRepository>();
                builder.Services.AddScoped<IUserService, UserService>();
                builder.Services.AddScoped<IAdminRepository, AdminRepository>();
                builder.Services.AddScoped<IAdminService, AdminService>();

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                // Swagger with JWT Auth
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "HomeBuddy API",
                        Version = "v1",
                        Description = "Backend API for HomeBuddy with Auth, Reviews, Orders, and Users"
                    });

                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter 'Bearer' followed by your JWT token",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    };

                    var securityRequirement = new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "bearer"
                                }
                            },
                            new string[] {}
                        }
                    };

                    options.AddSecurityDefinition("bearer", securityScheme);
                    options.AddSecurityRequirement(securityRequirement);
                });

                // JWT configuration
                var jwt = builder.Configuration.GetSection("Jwt");
                var jwtKey = jwt["Key"];
                var jwtIssuer = jwt["Issuer"];
                var jwtAudience = jwt["Audience"];

                if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                {
                    throw new InvalidOperationException("JWT configuration is missing!");
                }

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

                builder.Services.AddAuthorization();

                // Azure OpenAI - Optional
                //string? endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"];
                //string? apiKey = builder.Configuration["AZURE_OPENAI_KEY"];

                //if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
                //{
                //    builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
                //    {
                //        return new AzureOpenAIClient(
                //            new Uri(endpoint),
                //            new System.ClientModel.ApiKeyCredential(apiKey)
                //        );
                //    });
                //    Console.WriteLine("Azure OpenAI configured");
                //}
                //else
                //{
                //    Console.WriteLine("Azure OpenAI not configured - skipping");
                //}

                // Add CORS
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowSpecificOrigins",
                        policy =>
                        {
                            policy.WithOrigins(
                                "http://localhost:3000",
                                "https://localhost:7039",
                                "https://homebuddy-react-aedac9f5ckbbfmcm.norwayeast-01.azurewebsites.net"
                            )
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                        });
                });

                var app = builder.Build();

                Console.WriteLine("=== App Built Successfully ===");

                // Enable Swagger
                app.UseSwagger();
                app.UseSwaggerUI();

                // Only redirect to HTTPS in Production
                if (!app.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }

                app.UseCors("AllowSpecificOrigins");

                // Global exception handler - MUST be early in pipeline
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        var feature = context.Features.Get<IExceptionHandlerFeature>();
                        var ex = feature?.Error;

                        if (ex is NotFoundException nfe)
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            context.Response.ContentType = "application/json";
                            var payload = new
                            {
                                error = "NotFound",
                                message = nfe.Message,
                                resource = nfe.Resource,
                                identifier = nfe.Identifier
                            };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                            return;
                        }

                        if (ex is InsufficientStockException ise)
                        {
                            context.Response.StatusCode = StatusCodes.Status409Conflict;
                            context.Response.ContentType = "application/json";
                            var payload = new
                            {
                                error = "InsufficientStock",
                                message = ise.Message,
                                skuOrVariant = ise.SkuOrVariantId,
                                requested = ise.Requested,
                                available = ise.Available
                            };
                            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                            return;
                        }

                        throw ex!;
                    });
                });

                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

                // Health check endpoint
                app.MapGet("/health", () => Results.Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    environment = app.Environment.EnvironmentName
                }));

                app.MapGet("/", () => Results.Ok(new
                {
                    message = "HomeBuddy API is running",
                    endpoints = new[] { 
                        "/api/products",
                        //"/admin/reset-db",
                        "/admin/seed-db",
                    }
                }));

                //app.MapPost("/admin/reset-db", (IServiceProvider sp) =>
                //{
                //    using var scope = sp.CreateScope();
                //    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                //    db.Database.EnsureDeleted();
                //    db.Database.Migrate();

                //    return Results.Ok("Database was reset");
                //});

                app.MapPost("/admin/seed-db", (IServiceProvider sp) =>
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    SeedData.EnsureSeeded(db);
                    return Results.Ok("Database was seeded");
                });

                Console.WriteLine("=== Starting App ===");
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}