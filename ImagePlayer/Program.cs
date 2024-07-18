using ImagePlayer.Components;
using ImagePlayer.Services;
using Microsoft.Net.Http.Headers;

var imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(sp => new ImageWatcher(new Uri("http://0.0.0.0/Images/"), imagesPath));
builder.Services.AddSingleton(sp => new ImageCyclerContainer(sp.GetService<ImageWatcher>()!));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
    RequestPath = "/Images",
    OnPrepareResponseAsync = async (context) => await Task.Run(() =>
    {
        const int durationInSeconds = 60 * 60 * 24;
        context.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    })
});
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
