using CatStealer.Application.BackgroundServices;
using CatStealer.Application.Data;
using CatStealer.Application.Services;
using CatStealer.Application.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(DataContext).Assembly.GetName().Name);
    }));

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICatService, CatService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(sp => new BackgroundTaskQueue(capacity: 100));
builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHostedService<CatFetchingBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}

var configuredImageBasePath = app.Configuration["FileStorage:BasePath"];
var imageStoragePhysicalPath = !string.IsNullOrWhiteSpace(configuredImageBasePath) ? Path.GetFullPath(configuredImageBasePath) :
    Path.Combine(AppContext.BaseDirectory, "CatImages");

if (Directory.Exists(imageStoragePhysicalPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageStoragePhysicalPath),
        RequestPath = "/StoredCatImages"
    });
}

app.Run();