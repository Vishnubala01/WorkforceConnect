using WorkforceConnect.Api.Extensions;
using WorkforceConnect.Api.Middleware;
using WorkforceConnect.Application;
using WorkforceConnect.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

await app.Services.InitializeDatabaseAsync(app.Configuration);

app.Run();
