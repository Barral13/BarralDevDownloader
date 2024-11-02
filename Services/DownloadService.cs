using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using YoutubeExplode;

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

        // Usa o diretório padrão de Downloads do sistema
        var downloadsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
        var filePath = Path.Combine(downloadsDirectory, $"{video.Title}.mp3");
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);

        // Cria a pasta Musicas se não existir
        var musicDirectory = Path.Combine(downloadsDirectory, "Musicas");
        Directory.CreateDirectory(musicDirectory); // Cria a pasta se não existir

        // Move o arquivo de áudio para a pasta Musicas
        var newFilePath = Path.Combine(musicDirectory, $"{video.Title}.mp3");
        if (File.Exists(newFilePath))
        {
            File.Delete(newFilePath); // Remove o arquivo se já existir
        }
        File.Move(filePath, newFilePath);

        return newFilePath; // Retorna o caminho do arquivo movido
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

        // Usa o diretório padrão de Downloads do sistema
        var downloadsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

        // Define os caminhos dos arquivos temporários
        var videoFilePath = Path.Combine(downloadsDirectory, $"{video.Title}_video.mp4");
        var audioFilePath = Path.Combine(downloadsDirectory, $"{video.Title}_audio.mp3");
        var finalFilePath = Path.Combine(downloadsDirectory, $"{video.Title}.mp4");

        await _youtubeClient.Videos.Streams.DownloadAsync(videoStreamInfo, videoFilePath);
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);

        // Concatena o vídeo e o áudio
        await FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoFilePath}\"")
            .AddParameter($"-i \"{audioFilePath}\"")
            .AddParameter("-c:v copy -c:a aac -strict experimental")
            .AddParameter($"\"{finalFilePath}\"")
            .Start();

        // Cria a pasta Videos se não existir
        var videoDirectory = Path.Combine(downloadsDirectory, "Videos");
        Directory.CreateDirectory(videoDirectory); // Cria a pasta se não existir

        // Move o arquivo final para a pasta Videos
        var finalVideoPath = Path.Combine(videoDirectory, $"{video.Title}.mp4");
        if (File.Exists(finalVideoPath))
        {
            File.Delete(finalVideoPath); // Remove o arquivo se já existir
        }
        File.Move(finalFilePath, finalVideoPath);

        // Remove os arquivos temporários
        File.Delete(videoFilePath);
        File.Delete(audioFilePath);

        return finalVideoPath; // Retorna o caminho do arquivo final movido
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
