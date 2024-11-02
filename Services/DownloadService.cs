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

        // Define o caminho para a pasta 'Downloads'
        var downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var filePath = Path.Combine(downloadDirectory, $"{SanitizeFileName(video.Title)}.mp3");

        // Verifica se o arquivo já existe
        if (!File.Exists(filePath))
        {
            await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);
        }

        return filePath;
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

        var downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var videoFilePath = Path.Combine(downloadDirectory, $"{SanitizeFileName(video.Title)}_video.mp4");
        var audioFilePath = Path.Combine(downloadDirectory, $"{SanitizeFileName(video.Title)}_audio.mp3");
        var finalFilePath = Path.Combine(downloadDirectory, $"{SanitizeFileName(video.Title)}.mp4");

        if (!File.Exists(videoFilePath))
        {
            await _youtubeClient.Videos.Streams.DownloadAsync(videoStreamInfo, videoFilePath);
        }

        if (!File.Exists(audioFilePath))
        {
            await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, audioFilePath);
        }

        // Cria IStream a partir dos arquivos baixados
        var videoStream = await FFmpeg.GetMediaInfo(videoFilePath);
        var audioStream = await FFmpeg.GetMediaInfo(audioFilePath);

        var videoTrack = videoStream.VideoStreams.First();
        var audioTrack = audioStream.AudioStreams.First();
        
        // Converte os arquivos de vídeo e áudio em um único arquivo
        await FFmpeg.Conversions.New()
            .AddStream<IStream>(videoTrack, audioTrack) // Especifica explicitamente o tipo IStream
            .SetOutput(finalFilePath)
            .Start();

        // Remove os arquivos intermediários
        File.Delete(videoFilePath);
        File.Delete(audioFilePath);

        return finalFilePath;
    }

    private void ValidateVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            throw new ArgumentException("URL inválida.");
        }
    }

    private string ExtractVideoId(string url)
    {
        var match = Regex.Match(url, @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})");
        if (!match.Success)
        {
            throw new ArgumentException("Não foi possível extrair o ID do vídeo.");
        }

        return match.Groups[1].Value;
    }

    private string SanitizeFileName(string name)
    {
        return Regex.Replace(name, @"[<>:""/\\|?*]", "_");
    }
}
