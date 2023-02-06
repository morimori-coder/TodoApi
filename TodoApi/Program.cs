using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<TodoContext>(opt =>
    opt.UseSqlServer("name=ConnectionStrings:DefaultConnection"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfiguration configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", true, true)
      .Build();

IConfigurationSection section = configuration.GetSection("ConnectionStrings");
var cacheSection = section.GetSection("CacheConnection");

// セッション関連
// SQLServer分散キャッシュの有効化
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = cacheSection.Value;
    options.SchemaName = "dbo";
    options.TableName = "AppCache";
});
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "SampleCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromHours(1);
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCookiePolicy();
app.UseSession();

app.MapControllers();

app.Run();
