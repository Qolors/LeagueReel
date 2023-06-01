using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace LeagueReel.Services
{
    public class VideoCompressorService
    {
        public async Task InitializeAsync()
        {
            // Get latest version of FFmpeg. It's great idea to set it in startup time.
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        }

        public async Task ConvertVideoAsync(string inputPath, string outputPath)
        {
            // Load video file to convert
            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);

            // Create new conversion object
            var conversion = FFmpeg.Conversions.New()

                // Add video stream to output file with lower quality
                .AddStream(mediaInfo.VideoStreams.First())
                .SetVideoBitrate(2000000) // Adjust this value as needed
                .SetOutputFormat(Format.mp4)
                // Set output file path
                .SetOutput(outputPath);

            // Start conversion
            await conversion.Start();

            File.Delete(inputPath);
        }

    }
}
