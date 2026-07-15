
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.DTO.UserController;
using Safe_Qr_Backend.Repository.Repository.Users;
using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Services;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.Url;
using Safe_Qr_Backend.Services.Users;
using Safe_Qr_Backend.Services.VirusTotal;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Safe_Qr_Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddAuthentication()
                .AddCookie("LoginCookie", options =>
                {
                    options.Cookie.Name = "sessionId";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                });
            builder.Services.AddAuthorization();


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

            builder.Services.AddScoped<IPasswordHasher<UserCreateDTO>, PasswordHasher<UserCreateDTO>>();

            builder.Services.AddScoped<Phishing_Url_ONNX>();
            builder.Services.AddHttpClient<IGoogleSafeApiService, GoogleSafeApiService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<SafeBrowsingOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            }).AddResilienceHandler("safe-browsing-retry", builder =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 2,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromMilliseconds(200)
                    });
                    builder.AddTimeout(TimeSpan.FromSeconds(5));
                });


            builder.Services.AddHttpClient<IVirusTotalApiService, VirusTotalApiService>();

            builder.Services.AddScoped<IUrlService, UrlService>();
            builder.Services.AddScoped<IUserService, UserService>();








            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();

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
    }
}
