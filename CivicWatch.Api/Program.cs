using Microsoft.EntityFrameworkCore;
using CivicWatch.Api.Data;
using CivicWatch.Api.Services; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 
using System.Text;
using System.Net.Http.Headers;
using System;
using System.Collections.Generic; // Para a lista de string no Swagger

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// 1. CONFIGURAÇÃO DE SERVIÇOS (DI CONTAINER)
// =================================================================

// 1.1 Configuração do Entity Framework Core e SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 1.2 Configuração de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// 1.3 Registro dos Controllers e Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORREÇÃO: Registro de TODOS os serviços na seção correta
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAlertaService, AlertaService>(); 
builder.Services.AddScoped<IFornecedorService, FornecedorService>(); 
builder.Services.AddScoped<ITransparenciaService, TransparenciaService>(); 

// Configuração do Cliente HTTP para API Externa
builder.Services.AddHttpClient("PortalTransparenciaClient", client =>
{
    var token = builder.Configuration["ApiKeys:PortalTransparenciaToken"];
    
    client.BaseAddress = new Uri("https://api.portaldatransparencia.gov.br/");

    // Adiciona o cabeçalho 'chave-api-dados'
    client.DefaultRequestHeaders.Add("chave-api-dados", token); 
    
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});


// Configuração do Swagger para usar Bearer Token (JWT)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CivicWatch API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no campo 'Value' (Ex: Bearer {token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// 1.5 Configuração do JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] 
             ?? throw new InvalidOperationException("Chave JWT não configurada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });


// =================================================================
// 2. BUILD E CONFIGURAÇÃO DO PIPELINE HTTP
// =================================================================

var app = builder.Build();

// BLOCO CRÍTICO: EXECUTAR MIGRATIONS E SEEDING NA INICIALIZAÇÃO
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    context.Database.Migrate(); 
    await DataSeeder.SeedRolesAndAdmin(context);
}

// 2.1 Configuração do Pipeline em Ambiente de Desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy"); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();