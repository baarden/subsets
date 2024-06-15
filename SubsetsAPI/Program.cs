using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using SubsetsAPI;
using SubsetsAPI.Models.Configuration;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

var services = builder.Services;

services.AddHttpLogging(o => { });
services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
services.AddSingleton<GameService>(new GameService(connectionString));

services.AddHttpClient();
services.AddDistributedMemoryCache();
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

services.AddSession(options =>
{
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(24);
});

services.AddCors(options =>
{
    options.AddPolicy("AllowNgrok",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Only loopback proxies are allowed by default.
    // Clear that restriction because forwarders are enabled by explicit configuration.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
services.Configure<ProxySettings>(builder.Configuration.GetSection("ProxySettings"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSession();
app.MapControllers();
app.UseHttpLogging();
app.UseCors("AllowNgrok");

app.Run();

