//using System.IO;
//using System.Threading.Tasks;
//using YoutubeExplode;
//using YoutubeExplode.Videos.Streams;

//namespace BarraldevDownloader.Services
//{
//    public class DownloadService
//    {
//        private readonly YoutubeClient _youtubeClient;

//        public DownloadService()
//        {
//            _youtubeClient = new YoutubeClient();
//        }

//        public async Task<MemoryStream> DownloadVideoAsync(string url)
//        {
//            var videoId = YoutubeClient.ParseVideoId(url);
//            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
//            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

//            var memoryStream = new MemoryStream();
//            await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, memoryStream);
//            memoryStream.Position = 0;
//            return memoryStream;
//        }

//        public async Task<MemoryStream> DownloadAudioAsync(string url)
//        {
//            var videoId = YoutubeClient.ParseVideoId(url);
//            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
//            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

//            var memoryStream = new MemoryStream();
//            await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, memoryStream);
//            memoryStream.Position = 0;
//            return memoryStream;
//        }
//    }
//}
