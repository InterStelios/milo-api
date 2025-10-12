using MiloApi.Middleware;
using MiloApi.Services;
using Serilog;

Log.Logger = new LoggerConfiguration().CreateLogger();

try
{
    Log.Information("Starting Milo Server");

    DotNetEnv.Env.Load();

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
    );

    builder.Services.Configure<BackblazeStorageOptions>(
        builder.Configuration.GetSection(BackblazeStorageOptions.SectionName)
    );
    builder.Services.AddSingleton<IStorageService, BackblazeStorageService>();
    builder.Services.AddSingleton<IUploadTrackingService, InMemoryUploadTrackingService>();

    // Add exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowAll",
            policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); }
        );
    });

    var app = builder.Build();

    // Add exception handling middleware - must be early in pipeline
    app.UseExceptionHandler();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value!);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set(
                "RemoteIP",
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            );
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("AllowAll");
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Milo Server started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Milo Server terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}