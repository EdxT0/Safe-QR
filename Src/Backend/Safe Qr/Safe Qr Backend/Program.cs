
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Safe_Qr_Backend.Data;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Services;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.UrlService;
using Safe_Qr_Backend.Services.VirusTotal;

namespace Safe_Qr_Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
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
            builder.Services.AddDbContext<AppDbContext>(options =>
                     options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddScoped<Phishing_Url_ONNX>();
            builder.Services.AddScoped<IEvaluateUrlService,EvaluateUrlService>();
            builder.Services.AddHttpClient<IGoogleSafeApiService,GoogleSafeApiService>();
            builder.Services.AddHttpClient<IVirusTotalApiService, VirusTotalApiService>();









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
