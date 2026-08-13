
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.DTO.UserController;
using Safe_Qr_Backend.Repository.Repository.Users;
using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Services;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.UrlScans;
using Safe_Qr_Backend.Services.Users;
using Safe_Qr_Backend.Services.VirusTotal;
using System.Text.Json.Serialization;
using Polly;
using Safe_Qr_Backend.Services.UrlThreatEngine;
using Safe_Qr_Backend.Services.UrlReports;

namespace Safe_Qr_Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddAuthentication("LoginCookie")
                .AddCookie("LoginCookie", options =>
                {
                    options.Cookie.Name = "sessionId";
                    options.Cookie.HttpOnly = true;
                    // The frontend (http://localhost:3000) and API (https://localhost:56166) differ
                    // in scheme, which Chromium's schemeful-same-site treats as cross-site — Lax
                    // would silently drop the cookie on cross-origin fetches. None+Secure is the
                    // standard pairing for a separately-hosted SPA talking to a cookie-auth API.
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                });
            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy => policy
                    .WithOrigins(
                        builder.Configuration.GetSection("FrontendOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:3000" })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });


            //Dependancy Injection
            builder.Services.AddSingleton<InferenceSession>(_ =>
            {
                var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "model.onnx");
                var options = new Microsoft.ML.OnnxRuntime.SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = 1
                };

                return new InferenceSession(modelPath, options);
            });

            builder.Services.Configure<SafeBrowsingOptions>(builder.Configuration.GetSection(SafeBrowsingOptions.SectionName));



            builder.Services.AddDbContext<AppDbContext>(options =>
                     options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddScoped<IUrlReportRepository, UrlReportRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<Safe_Qr_Backend.Repository.ScanHistories.IScanHistoryRepository, Safe_Qr_Backend.Repository.ScanHistories.ScanHistoryRepository>();
            builder.Services.AddScoped<Safe_Qr_Backend.Services.ScanHistories.IScanHistoryService, Safe_Qr_Backend.Services.ScanHistories.ScanHistoryService>();

            builder.Services.AddScoped<IPasswordHasher<UserCreateDTO>, PasswordHasher<UserCreateDTO>>();
            builder.Services.AddScoped<PasswordHasher<Safe_Qr_Backend.Entities.User>>();
            builder.Services.AddScoped<Safe_Qr_Backend.Services.Auth.IAuthService, Safe_Qr_Backend.Services.Auth.AuthService>();

            builder.Services.AddScoped<IPhishingUrlOnnxService, Phishing_Url_ONNX>();
            builder.Services.AddScoped<IUrlThreatEngineService, UrlThreatEngineService>();
            builder.Services.AddHttpClient<IGoogleSafeApiService, GoogleSafeApiService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<SafeBrowsingOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            }).AddResilienceHandler("safe-browsing-retry", builder =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(5));
                });


            builder.Services.AddHttpClient<IVirusTotalApiService, VirusTotalApiService>();
            builder.Services.AddScoped<IUrlReportService, UrlReportService>();
            builder.Services.AddScoped<IUrlScanService, UrlScanService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddSingleton<Safe_Qr_Backend.Services.Sandbox.ISandboxScreenshotService, Safe_Qr_Backend.Services.Sandbox.SandboxScreenshotService>();

            builder.Services.AddScoped<Safe_Qr_Backend.Repository.ThreatFeedbacks.IThreatFeedbackRepository, Safe_Qr_Backend.Repository.ThreatFeedbacks.ThreatFeedbackRepository>();
            builder.Services.AddScoped<Safe_Qr_Backend.Services.ThreatFeedbacks.IThreatFeedbackService, Safe_Qr_Backend.Services.ThreatFeedbacks.ThreatFeedbackService>();








            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Not using UseHttpsRedirection(): when tunnelled through ngrok (or any
            // reverse proxy that already terminates HTTPS at the edge), a forced
            // local HTTP->HTTPS redirect breaks CORS preflight requests outright
            // (redirects aren't allowed for OPTIONS preflights). Public traffic is
            // still fully HTTPS via the tunnel/proxy either way.
            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            await SeedAdminUserAsync(app);

            await app.RunAsync();

            //var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "model.onnx");
            //var options = new Microsoft.ML.OnnxRuntime.SessionOptions
            //{
            //    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            //    IntraOpNumThreads = 1
            //};
            //InferenceSession session = new InferenceSession(modelPath, options);
            //Phishing_Url_ONNX test2 = new Phishing_Url_ONNX(session);
            //EvaluateUrlService test = new EvaluateUrlService(test2);
            //var result = test.EvaluateUrl(new List<string> { "www.example.com" });

            //for(int i =0; i < result.Count; i++)
            //{
            //    Console.WriteLine(result[i]);
            //}
        }

        /// <summary>
        /// Idempotently ensures exactly one Admin account exists, from Admin:Email /
        /// Admin:Password / Admin:Name config (set via user-secrets, never appsettings.json).
        /// Admin accounts can't be created through public registration — this is the
        /// only path that creates one. Skips silently if the config isn't set.
        /// </summary>
        private static async Task SeedAdminUserAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var adminEmail = config["Admin:Email"];
            var adminPassword = config["Admin:Password"];
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alreadyExists = await db.User.AnyAsync(u => u.Email == adminEmail);
            if (alreadyExists)
            {
                return;
            }

            var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
            var admin = new User
            {
                Name = config["Admin:Name"] ?? "Administrator",
                Email = adminEmail,
                Role = UserRoleEnum.Admin,
                HashedPassword = string.Empty,
            };
            admin.HashedPassword = hasher.HashPassword(admin, adminPassword);

            db.User.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
