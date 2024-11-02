using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using YoutubeExplode;

public class DownloadService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly string _musicasDirectory;
    private readonly string _videosDirectory;

    public DownloadService(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;

        // Obter os diretórios de download a partir de variáveis de ambiente
        _musicasDirectory = Path.Combine("C:\\Downloads", "Musicas");
        _videosDirectory = Path.Combine("C:\\Downloads", "Videos");

        CreateDirectories();
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

        var cleanedTitle = CleanFileName(video.Title);
        var filePath = Path.Combine(_musicasDirectory, $"{cleanedTitle}.mp3");
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

        var cleanedTitle = CleanFileName(video.Title);

        // Define os caminhos dos arquivos temporários
        var videoFilePath = Path.Combine(_videosDirectory, $"{cleanedTitle}_video.mp4");
        var audioFilePath = Path.Combine(_videosDirectory, $"{cleanedTitle}_audio.mp3");
        var finalFilePath = Path.Combine(_videosDirectory, $"{cleanedTitle}.mp4");

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
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var regex = new Regex($"[{invalidChars}]");
        return regex.Replace(fileName, "_"); // Substitui caracteres inválidos por "_"
    }

    private void CreateDirectories()
    {
        // Cria o diretório Downloads se não existir
        var downloadsDirectory = Path.Combine("C:\\", "Downloads");
        if (!Directory.Exists(downloadsDirectory))
        {
            Directory.CreateDirectory(downloadsDirectory);
        }

        // Cria as pastas Musicas e Videos dentro do diretório Downloads se não existirem
        if (!Directory.Exists(_musicasDirectory))
        {
            Directory.CreateDirectory(_musicasDirectory); // Cria a pasta "Musicas"
        }

        if (!Directory.Exists(_videosDirectory))
        {
            Directory.CreateDirectory(_videosDirectory); // Cria a pasta "Videos"
        }
    }
}
