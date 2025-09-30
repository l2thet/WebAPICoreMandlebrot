using ILGPU;
using ILGPU.Runtime;
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

builder.Services.AddSingleton<ILGPUAcceleratorService>(provider =>
{
    var factory = provider.GetRequiredService<IILGPUAcceleratorFactory>();
    return factory.CreateAcceleratorService();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();