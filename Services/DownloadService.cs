using YoutubeExplode;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace BarraldevDownloader.Services;

public class DownloadService
{
    private readonly YoutubeClient _youtubeClient;

    public DownloadService(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;
    }

    public async Task<string> DownloadAudioAsync(string videoUrl)
    {
        ValidateVideoUrl(videoUrl);
        var videoId = ExtractVideoId(videoUrl);

        var video = await _youtubeClient.Videos.GetAsync(videoId);
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
        var audioStreamInfo = streamManifest.GetAudioStreams()
            .OrderByDescending(s => s.Bitrate)
            .FirstOrDefault();

        if (audioStreamInfo == null)
        {
            throw new InvalidOperationException("Nenhum stream de áudio disponível.");
        }

        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{video.Title}.mp3");
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);

        return filePath; // Retorna o caminho do arquivo baixado
    }

    public async Task<string> DownloadVideoAsync(string videoUrl)
    {
        ValidateVideoUrl(videoUrl);
        var videoId = ExtractVideoId(videoUrl);

        var video = await _youtubeClient.Videos.GetAsync(videoId);
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);

        var videoStreamInfo = streamManifest.GetVideoStreams()
            .OrderByDescending(s => s.Bitrate)
            .FirstOrDefault();

        var audioStreamInfo = streamManifest.GetAudioStreams()
            .OrderByDescending(s => s.Bitrate)
            .FirstOrDefault();

        if (videoStreamInfo == null || audioStreamInfo == null)
        {
            throw new InvalidOperationException("Nenhum stream de vídeo ou áudio disponível.");
        }

        var videoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{video.Title}_video.mp4");
        var audioFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{video.Title}_audio.mp3");
        var finalFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{video.Title}.mp4");

        await _youtubeClient.Videos.Streams.DownloadAsync(videoStreamInfo, videoFilePath);
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);

        await FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoFilePath}\"")
            .AddParameter($"-i \"{audioFilePath}\"")
            .AddParameter("-c:v copy -c:a aac -strict experimental")
            .AddParameter($"\"{finalFilePath}\"")
            .Start();

        File.Delete(videoFilePath);
        File.Delete(audioFilePath);

        return finalFilePath; // Retorna o caminho do arquivo baixado
    }

    private void ValidateVideoUrl(string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            throw new ArgumentException("URL do vídeo é obrigatória.", nameof(videoUrl));
        }

        if (ExtractVideoId(videoUrl) == null)
        {
            throw new ArgumentException("URL do vídeo é inválida.", nameof(videoUrl));
        }
    }

    private string? ExtractVideoId(string url)
    {
        var regex = new Regex(@"(?<=v=|\/)([a-zA-Z0-9_-]{11})");
        var match = regex.Match(url);
        return match.Success ? match.Value : null;
    }
}
