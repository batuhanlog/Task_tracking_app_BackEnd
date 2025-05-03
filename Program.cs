using Microsoft.EntityFrameworkCore;
using TaskTitan.Api.Mappings;
using TaskTitanData;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
// === YENÝ USING'LER ===
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// ======================

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; // Configuration'ý bir deðiþkene alalým

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- Servis Kayýtlarý ---
builder.Services.AddDbContext<TaskTitanDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("TaskTitanDb"))); // configuration deðiþkenini kullan

builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// === Swagger'a Authentication Desteði Ekleyelim ===
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskTitan API", Version = "v1", Description = "Görev ve Proje Yönetimi API" });
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // JWT Bearer Authentication tanýmýný Swagger'a ekle
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, // veya Http (Bearer için)
        Scheme = "Bearer" // "Bearer" þemasýný belirt
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // Yukarýdaki definition ile eþleþmeli
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
// ================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// === AUTHENTICATION VE AUTHORIZATION SERVÝSLERÝNÝ EKLE ===
builder.Services.AddAuthentication(options =>
{
    // Varsayýlan þemalarý ayarla
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => // JWT Bearer yapýlandýrmasý
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Issuer'ý doðrula
        ValidateAudience = true, // Audience'ý doðrula
        ValidateLifetime = true, // Token süresini doðrula
        ValidateIssuerSigningKey = true, // Ýmza anahtarýný doðrula
        ValidIssuer = configuration["Jwt:Issuer"], // appsettings'den al
        ValidAudience = configuration["Jwt:Audience"], // appsettings'den al
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)) // appsettings'den al ve byte dizisine çevir
        // ClockSkew = TimeSpan.Zero // Ýsteðe baðlý: Zaman farký toleransýný kaldýr
    };
});

// Authorization servisini ekle (Rol bazlý veya policy bazlý yetkilendirme için)
builder.Services.AddAuthorization();
// ======================================================

var app = builder.Build();

// --- HTTP Request Pipeline Yapýlandýrmasý ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTitan API V1");
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);


app.UseAuthentication(); 

app.UseAuthorization(); 
// ==========================================================

app.MapControllers();

app.Run();