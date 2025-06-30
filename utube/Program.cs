using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Nest;
using utube.Data;
using utube.Interfaces;
using utube.Options;
using utube.Repositories;
using utube.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:1234", "http://127.0.0.1:5501", "http://127.0.0.1:5502")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var settings = new ConnectionSettings(new Uri("http://localhost:9200")) // your ES URI
        .DefaultIndex("videos"); // default index name

    return new ElasticClient(settings);
});
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; // Allow large files
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.KeyLengthLimit = int.MaxValue;
});

// Configure request size limits
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IVideoRepository, VideoRepository>();

builder.Services.AddScoped<IVideoUploadService, VideoUploadService>();
builder.Services.AddScoped<IEncodingProfileRepository, EncodingProfileRepository>();
builder.Services.AddScoped<ITranscodeJobRepository, TranscodeJobRepository>();
builder.Services.AddScoped<IThumbnailJobRepository, ThumbnailJobRepository>();
builder.Services.AddScoped<IWatermarkingRepository, WatermarkingRepository>();
builder.Services.AddScoped<ThumbnailService>();
builder.Services.AddScoped<ElasticSearchService>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
// Cloud Storage setup
var config = builder.Configuration;
var storageProvider = config["StorageProvider"];

if (storageProvider == "Azure")
{
    builder.Services.AddSingleton<ICloudStorageUploader, AzureBlobUploader>();
    builder.Services.AddSingleton<ISignedUrlGenerator, AzureSignedUrlGenerator>();
}
else
    throw new Exception("Unsupported or missing StorageProvider in config.");


if (builder.Configuration["Messaging:Provider"] == "RabbitMQ")
{
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisherService>();
}


builder.Services.AddHostedService<TranscodingConsumerService>();
builder.Services.AddHostedService<ThumbnailConsumerService>();
builder.Services.AddHostedService<WatermarkingConsumer>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();