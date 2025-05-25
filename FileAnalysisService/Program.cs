using FileStorageService.Data;
using FileAnalysisService.Services;
using Microsoft.EntityFrameworkCore;

// Create a new web application builder
var builder = WebApplication.CreateBuilder(args);

// Register services for dependency injection
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL database context
builder.Services.AddDbContext<FileAnalysisDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register the text analysis service as a singleton
// Note: This service handles generating text statistics and word clouds
builder.Services.AddSingleton<TextAnalysisService>();

// Register HttpClient for making HTTP requests to other services
builder.Services.AddHttpClient();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<FileAnalysisDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure middleware pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start the application
app.Run();