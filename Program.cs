using System.Text;
using System.Text.Json.Serialization;
using LinchpinBackend.Data;
using LinchpinBackend.Infrastructure;
using LinchpinBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Banco de dados (MySQL)
var connectionString = config.GetConnectionString("LinchpinDb")
    ?? throw new InvalidOperationException("Configure a connection string 'ConnectionStrings:Default'.");
var serverVersion = new MySqlServerVersion(Version.Parse(config["Database:ServerVersion"] ?? "8.0.36"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion));

// Autenticação JWT
var jwtSection = config.GetSection(JwtOptions.Section);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Configure 'Jwt:Key' com pelo menos 32 caracteres.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// CORS: origens do front-end
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // O front envia alguns ids como texto ("12"); aceitamos números em string.
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.JsonSerializerOptions.Converters.Add(new FlexibleStringConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// Serviços (regras de negócio)
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SimulatorService>();
builder.Services.AddScoped<SimulatorClassService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<LessonGroupService>();
builder.Services.AddScoped(typeof(ReportService<>));
builder.Services.AddScoped<DbSeeder>();

var app = builder.Build();

// Aplica as migrations e importa os dados iniciais (se o banco estiver vazio).
if (config.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().RunAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
