using Microsoft.AspNetCore.Http.Features;
using NLog;
using NLog.Web;

var logger = LogManager.GetCurrentClassLogger();
try
{
    var applicationDirectory = AppContext.BaseDirectory;
    LogManager.Setup().LoadConfigurationFromFile(Path.Combine(applicationDirectory, "nlog.config"));
    var settingsFile = Path.Combine(applicationDirectory, "settings.xml");
    var settings = ServerSettings.Load(settingsFile);
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = applicationDirectory
    });
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    builder.WebHost.UseUrls(settings.ListenUrl);
    builder.Services.AddSingleton(settings);
    builder.Services.AddControllers();
    builder.Services.AddSingleton<FileService>();
    builder.Services.AddCors(options => options.AddPolicy("AllowLocalNetwork", policy =>
        policy.WithOrigins(settings.FrontendOrigin).AllowAnyHeader().AllowAnyMethod()));
    builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 524288000);
    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 524288000);
    var app = builder.Build();
    app.Services.GetRequiredService<FileService>();
    if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
    app.UseCors("AllowLocalNetwork");
    app.UseAuthorization();
    app.MapControllers();
    app.Lifetime.ApplicationStarted.Register(() =>
        logger.Info("Settings: {0}; listening on: {1}", settingsFile, string.Join(", ", app.Urls)));
    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Application startup or execution failed");
    Console.Error.WriteLine($"Ошибка запуска: {exception.Message}");
    Environment.ExitCode = 1;
}
finally
{
    LogManager.Shutdown();
}
