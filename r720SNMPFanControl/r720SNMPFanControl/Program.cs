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

AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;


builder.Services.AddSingleton<OIDs>(OIDs);
builder.Services.AddSingleton<Passwords>(Passwords);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
{
    Console.WriteLine(e.ExceptionObject.ToString());
    Console.WriteLine("Press Enter to Exit");
    Console.ReadLine();
    Environment.Exit(0);
}