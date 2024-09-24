using Interpret_grading_documents.Services;

namespace Interpret_grading_documents
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddControllersWithViews();

            // Register Form Recognizer Service
            builder.Services.AddSingleton<FormRecognizerService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var endpoint = configuration["AzureFormRecognizer:Endpoint"];
                var apiKey = configuration["AzureFormRecognizer:ApiKey"];
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
