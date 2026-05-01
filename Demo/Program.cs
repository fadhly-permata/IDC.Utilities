using Demo.Middlewares;
using Demo.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new()
        {
            Title = "Weather Forecast API",
            Version = "v1",
            Description = "API untuk mendapatkan prakiraan cuaca",
            Contact = new() { Name = "Tim Pengembang", Email = "dev@example.com" },
        }
    );
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Forecast API V1"));

app.UseHttpsRedirection();
app.UseAuthorization();

// Gunakan middleware dengan konfigurasi
app.UseApiResponseWrapper(Commons.IsDevelopment());

app.MapControllers();
app.Run();
