using r720SNMPFanControl.BackgroundService;
using r720SNMPFanControl.Configs;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TimedReaderService>();

OIDs OIDs = new OIDs();
OIDs.Fans = builder.Configuration.GetSection("FanOIDs").Get<string[]>();
OIDs.Temperatures = builder.Configuration.GetSection("TemperatureOIDs").Get<string[]>();
OIDs.CPUTemperatures = builder.Configuration.GetSection("CPUTemperatureOIDs").Get<string[]>();

Passwords Passwords = new();
Passwords.Hostname = builder.Configuration.GetSection("Hostname").Get<string>();  
Passwords.User = builder.Configuration.GetSection("User").Get<string>();
Passwords.Password = builder.Configuration.GetSection("Password").Get<string>();



builder.Services.AddSingleton<OIDs>(OIDs);
builder.Services.AddSingleton<Passwords>(Passwords);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           summaries[Random.Shared.Next(summaries.Length)]
       ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}