using Interpret_grading_documents.Services;
using OpenAI.Chat;

namespace Interpret_grading_documents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Retrieve the Environment Variable

            var apiKey = Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key not found. Please set the environment variable.");
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register GPTService
            builder.Services.AddSingleton<GPTService>();
            

            // Register GPTService
            builder.Services.AddSingleton<GPTService>();

            // Register Form Recognizer Service
            builder.Services.AddSingleton<FormRecognizerService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var endpoint = configuration["AzureFormRecognizer:Endpoint"];
                // var apiKey = configuration["AzureFormRecognizer:ApiKey"];
                return new FormRecognizerService(endpoint, apiKey);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
        }
    }
}
