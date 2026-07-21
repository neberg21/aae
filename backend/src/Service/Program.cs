using Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCore();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCoreHost();

app.Run();
