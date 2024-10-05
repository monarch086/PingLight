using Microsoft.AspNetCore.Identity;
using PingLight.WebApi.ServiceExtensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwagger(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddOauthAuthorization(builder.Configuration);

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.ApplyMigrations();

app.MapIdentityApi<IdentityUser>();

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
