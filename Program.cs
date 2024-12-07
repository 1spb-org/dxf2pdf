/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/

using Hangfire;
using Microsoft.OpenApi.Models;
using Dxf2Pdf.Queue.Services;
using Hangfire.LiteDB;
using System.Text;

"Dxf2Pdf universal microservice started".CoutLn(ConsoleColor.Green);

if(args.FirstOrDefault() == "/?")
{
    Usage.Print();
    return;
}

// Enable code page 866 
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo { Title = "gRPC transcoding", Version = "v1" });

    var filePath = Path.Combine(System.AppContext.BaseDirectory, "Dxf2Pdf.Queue.xml");
    c.IncludeXmlComments(filePath);
    c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);

});

var c = builder.Configuration;

builder.Services.    // Add Hangfire services.
    AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseLiteDbStorage(c["Hangfire:DbName"].EmptyAsNull() ?? "Hangfire.db"));

int wc = 1;
try
{
    wc = c.GetValue<int>("Hangfire:WorkerCount");
    if (wc < 1) wc = 1;
    else if (wc > 5) wc = 5;
} catch 
{
    "Unable to get Hangfire:WorkerCount value".Error();
}

// Add the processing server as IHostedService
builder.Services.AddHangfireServer(options => options.WorkerCount = wc); 

var app = builder.Build();

app.UseSwagger();
//if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}
// Configure the HTTP request pipeline.
app.MapGrpcService<LauncherService>();
app.MapGet("/", () => 
"Communication with gRPC endpoints must be made through a gRPC client. " +
"To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

 
app.UseHangfireDashboard();

app.Run();


"Dxf2Pdf universal microservice stopped".CoutLn(ConsoleColor.Green);
