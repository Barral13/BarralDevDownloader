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

        // Define o diretório Downloads de forma manual
        string musicasDirectory = GetMusicasDirectory();
        var cleanedTitle = CleanFileName(video.Title);
        var filePath = Path.Combine(musicasDirectory, $"{cleanedTitle}.mp3");
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

        // Define o diretório Downloads de forma manual
        string videosDirectory = GetVideosDirectory();
        var cleanedTitle = CleanFileName(video.Title);

        // Define os caminhos dos arquivos temporários
        var videoFilePath = Path.Combine(videosDirectory, $"{cleanedTitle}_video.mp4");
        var audioFilePath = Path.Combine(videosDirectory, $"{cleanedTitle}_audio.mp3");
        var finalFilePath = Path.Combine(videosDirectory, $"{cleanedTitle}.mp4");

        await _youtubeClient.Videos.Streams.DownloadAsync(videoStreamInfo, videoFilePath);
        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);

        // Concatena o vídeo e o áudio
        await FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoFilePath}\"")
            .AddParameter($"-i \"{audioFilePath}\"")
            .AddParameter("-c:v copy -c:a aac -strict experimental")
            .AddParameter($"\"{finalFilePath}\"")
            .Start();

        // Remove os arquivos temporários
        File.Delete(videoFilePath);
        File.Delete(audioFilePath);

        return finalFilePath; // Retorna o caminho do arquivo final
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

    private string CleanFileName(string fileName)
    {
        // Remove caracteres inválidos do nome do arquivo
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var regex = new Regex($"[{invalidChars}]");
        return regex.Replace(fileName, "_"); // Substitui caracteres inválidos por "_"
    }

    private string GetMusicasDirectory()
    {
        string musicasDirectory = "/downloads/musicas"; // Caminho fixo para o contêiner

        if (!Directory.Exists(musicasDirectory))
        {
            Directory.CreateDirectory(musicasDirectory); // Cria a pasta "Musicas" se não existir
        }

        return musicasDirectory;
    }

    private string GetVideosDirectory()
    {
        string videosDirectory = "/downloads/videos"; // Caminho fixo para o contêiner

        if (!Directory.Exists(videosDirectory))
        {
            Directory.CreateDirectory(videosDirectory); // Cria a pasta "Videos" se não existir
        }

        return videosDirectory;
    }

}
