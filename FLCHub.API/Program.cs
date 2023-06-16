// using FLCHub.Services;
using FLCHub.API.Services;
using FLCHub.API.Services.It;
using FLCHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
// using FLCHub.Services.Edgar9000;

//establishes a folder for project
string userName = Environment.UserName;
Global.ProjectPath = $@"C:\Users\{userName}\FLCHub";
if(!Directory.Exists(Global.ProjectPath)) Directory.CreateDirectory(Global.ProjectPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

//Gets connection string from secret
var connectionString = builder.Configuration.GetConnectionString("LocalConnection");
//Sets DB connection = connectionString
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(connectionString));


builder.Services.AddControllers();

//Adds services via dependency injection
builder.Services.AddScoped<ItToolService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


class Global
{
    public static string ProjectPath;
}