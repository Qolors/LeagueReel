using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LeagueReel.Services
{
    //TODO --> FrameRate and frameDelay do not make sense, kind of hacked to get a smooth gif
    public class ScreenRecorderService
    {
        private const int FrameRate = 33;
        private const int BufferSize = FrameRate * 5;
        private const PixelFormat CapturePixelFormat = PixelFormat.Format32bppArgb;
        private ImageFormat CompressionFormat = ImageFormat.Jpeg;
        private ConcurrentQueue<byte[]> _frameBuffer = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<DateTime> _frameTimestamps = new ConcurrentQueue<DateTime>();
        public volatile bool IsRecording = false;

        public ScreenRecorderService()
        {
            // Start the recording task in the background
        }

        public void Start()
        {
            IsRecording = true;
            Debug.WriteLine("Starting");
            Task.Run(() => Recording());
        }

        private async Task Recording()
        {
            var bounds = Screen.PrimaryScreen.Bounds;

            while (IsRecording)
            {
                await CaptureFrame(bounds);

                // Wait for the next frame
                await Task.Delay(1000 / FrameRate);
            }
        }

        private async Task CaptureFrame(System.Drawing.Rectangle bounds)
        {
            using (var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height, CapturePixelFormat))
            {
                // Capture frame
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);

                    // Compress frame
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, CompressionFormat);
                        var compressedFrame = ms.ToArray();

                        // Add frame to buffer
                        _frameBuffer.Enqueue(compressedFrame);
                        _frameTimestamps.Enqueue(DateTime.UtcNow);
                    }
                }
            }

            // Remove old frames if buffer is full
            while (_frameBuffer.Count > BufferSize)
            {
                _frameBuffer.TryDequeue(out var _);
                _frameTimestamps.TryDequeue(out var _);
            }
        }



        public void SaveHighlight(string outputFilename)
        {
            var gifFile = $"C:\\LeagueGif\\{outputFilename}.gif";

            var width = 600;
            var height = 600;

            var options = new ResizeOptions
            {
                Mode = ResizeMode.Pad,
                Size = new SixLabors.ImageSharp.Size(width, height),
            };

            var frameDelay = 8;

            using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height))
            {

                while (!_frameBuffer.IsEmpty)
                {
                    if (_frameBuffer.TryDequeue(out var frameData) && _frameTimestamps.TryDequeue(out var timestamp))
                    {
                        using (var imgFrame = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(frameData))
                        {
                            imgFrame.Mutate(x => x.Resize(options));

                            var metadata = imgFrame.Frames.RootFrame.Metadata.GetFormatMetadata(SixLabors.ImageSharp.Formats.Gif.GifFormat.Instance);
                            metadata.FrameDelay = frameDelay;
                            image.Frames.AddFrame(imgFrame.Frames.RootFrame);
                        }
                    }
                }


                image.SaveAsGif(gifFile, new SixLabors.ImageSharp.Formats.Gif.GifEncoder 
                { 
                    ColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode.Global,
                    
                });
            }

            Debug.WriteLine("Finished creating GIF");
        }



        public void Stop()
        {
            IsRecording = false;
            //empty out the buffer
            while (!_frameBuffer.IsEmpty)
            {
                _frameBuffer.TryDequeue(out var _);
                _frameTimestamps.TryDequeue(out var _);
            }
        }
    }
}

