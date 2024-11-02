using BarraldevDownloader.Components;
using BarraldevDownloader.Services;
using YoutubeExplode;

var builder = WebApplication.CreateBuilder(args);

// Adiciona suporte para Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddSingleton<DownloadService>();
// Registra HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthorization();

// Mapeia os endpoints para Razor Components com o modo de renderização interativa
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Endpoint para download de áudio
app.MapGet("/api/download/audio", async (string videoUrl, DownloadService downloadService) =>
{
    var filePath = await downloadService.DownloadAudioAsync(videoUrl);
    return Results.File(filePath, "audio/mpeg", Path.GetFileName(filePath));
});

// Endpoint para download de vídeo
app.MapGet("/api/download/video", async (string videoUrl, DownloadService downloadService) =>
{
    var filePath = await downloadService.DownloadVideoAsync(videoUrl);
    return Results.File(filePath, "video/mp4", Path.GetFileName(filePath));
});

app.Run();
