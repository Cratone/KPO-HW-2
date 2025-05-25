using FileStorageService.Data;
using FileStorageService.Services;
using Microsoft.EntityFrameworkCore;

// Create a new web application builder
var builder = WebApplication.CreateBuilder(args);

// Register services for dependency injection
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL database context
builder.Services.AddDbContext<FileStorageDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register file storage services
// FileDBStorageService is scoped because it depends on DbContext
builder.Services.AddScoped<FileDBStorageService>();
// FileDriveStorageService is a singleton as it doesn't depend on scoped services
builder.Services.AddSingleton<FileDriveStorageService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI for API documentation in development
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Create database if it doesn't exist (development only)
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure middleware pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start the application
app.Run();
