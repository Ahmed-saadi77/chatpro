using System.Text;
using chatpro.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // add this for NpgsqlConnection
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<JwtService>();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost", "http://34.89.228.75")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Needed for SignalR and cookies/auth
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Wait for PostgreSQL to be ready
WaitForDatabase(app.Configuration.GetConnectionString("DefaultConnection")!);

// Apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowReactFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();

void WaitForDatabase(string connectionString, int maxRetries = 10, int delaySeconds = 5)
{
    int retries = 0;
    while (retries < maxRetries)
    {
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            Console.WriteLine("Connected to the database successfully.");
            return;
        }
        catch (Exception ex)
        {
            retries++;
            Console.WriteLine($"Waiting for database (attempt {retries}/{maxRetries}): {ex.Message}");
            Thread.Sleep(delaySeconds * 1000);
        }
    }

    throw new Exception("Could not connect to the database after multiple attempts.");
}
