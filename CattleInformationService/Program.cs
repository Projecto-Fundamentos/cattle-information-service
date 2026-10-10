using CattleInformationService.CattleInformation.Application.Internal.Command;
using CattleInformationService.CattleInformation.Application.Internal.Queries;
using CattleInformationService.CattleInformation.Domain.Repositories;
using CattleInformationService.CattleInformation.Domain.Services.Commands;
using CattleInformationService.CattleInformation.Domain.Services.Queries;
using CattleInformationService.CattleInformation.Infrastructure.Persistence.EFC.Repositories;
using CattleInformationService.Shared.Domain.Repositories;
using CattleInformationService.Shared.Infrastructure.Persistence.EFC.Configuration;
using CattleInformationService.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Service Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});

//Service Products
builder.Services.AddScoped<ICattleInformationCommandService, CattleInformationCommandService>();
builder.Services.AddScoped<ICattleInformationQueryService, CattleInformationQueryService>();
builder.Services.AddScoped<ICattleInformationRepository, CattleInformationRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


// Add Databases Services
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
    else if (builder.Environment.IsProduction())
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .LogTo(Console.WriteLine, LogLevel.Error);
        
});

var app = builder.Build();
 
// Verify if the database exists and create it if it doesn't
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
context.Database.EnsureCreated();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();