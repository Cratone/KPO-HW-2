// Create a new web application builder
var builder = WebApplication.CreateBuilder(args);

// Register services for dependency injection
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient for making requests to microservices
builder.Services.AddHttpClient();

// Configure CORS to allow cross-origin requests
// This is important for frontend applications to communicate with the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()  // Allow requests from any origin
               .AllowAnyMethod()  // Allow any HTTP method (GET, POST, etc.)
               .AllowAnyHeader(); // Allow any HTTP headers
    });
});

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI for API documentation in development
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure middleware pipeline
app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
app.UseCors();            // Enable CORS middleware
app.UseAuthorization();   // Enable authorization
app.MapControllers();     // Map controller routes

// Start the application
app.Run();
