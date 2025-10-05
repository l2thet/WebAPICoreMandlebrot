using ILGPU;
using WebAPICoreMandlebrot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Initialize ILGPU services using Factory pattern
builder.Services.AddSingleton<Context>(provider =>
{
    return Context.Create(builder => builder.Default().EnableAlgorithms());
});

builder.Services.AddSingleton<IILGPUAcceleratorFactory, ILGPUAcceleratorFactory>();

builder.Services.AddSingleton<IILGPUAcceleratorService>(provider =>
{
    var factory = provider.GetRequiredService<IILGPUAcceleratorFactory>();
    return factory.CreateAcceleratorService();
});

var app = builder.Build();

app.UseHttpsRedirection();

// Serve static files FIRST - index.html will be served at root by default
app.UseDefaultFiles();

// Configure static files with no-cache headers in development
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Disable caching for CSS and JS files in development
            if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        }
    });
}
else
{
    app.UseStaticFiles();
}

// Configure routing AFTER static files
app.UseRouting();

// Configure Swagger with specific routing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger"; // Swagger ONLY at /swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mandelbrot API V1");
        c.DocumentTitle = "Mandelbrot API Documentation";
    });
}

app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();