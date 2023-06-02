using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Windows.Forms;
using SharpAvi.Output;
using SharpAvi.Codecs;
using System.Threading;
namespace LeagueReel.Services
{
    public class ScreenRecorderService
    {
        private IAviVideoStream videoStream;
        private IAviAudioStream audioStream;
        private AviWriter writer;

        private const int FrameRate = 30;
        private const int BufferSize = FrameRate * 15;
        private const PixelFormat CapturePixelFormat = PixelFormat.Format32bppArgb;
        private ImageFormat CompressionFormat = ImageFormat.Jpeg;
        private ConcurrentQueue<byte[]> _frameBuffer = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> _audioBuffer = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<DateTime> _frameTimestamps = new ConcurrentQueue<DateTime>();
        private ConcurrentQueue<DateTime> _audioTimestamps = new ConcurrentQueue<DateTime>();
        private VideoCompressorService videoCompressorService;
        private WasapiLoopbackCapture captureInstance = null;
        private WaveFileWriter recordedAudioWriter = null;
        public volatile bool IsRecording = false;

        public ScreenRecorderService()
        {
            videoCompressorService = new VideoCompressorService();
            Task.Run(async () => await FetchFFmpeg());
        }

        public void Start()
        {
            IsRecording = true;
            Debug.WriteLine("Starting");
            Task.Run(() => Recording());
        }

        public async Task FetchFFmpeg()
        {
            await videoCompressorService.InitializeAsync();
        }

        private void Recording()
        {
            var bounds = Screen.PrimaryScreen.Bounds;

            captureInstance = new WasapiLoopbackCapture();

            captureInstance.WaveFormat = new WaveFormat(44100, 16, 2);

            captureInstance.DataAvailable += WaveSource_DataAvailable;

            captureInstance.RecordingStopped += (s, a) =>
            {
                recordedAudioWriter.Dispose();
                recordedAudioWriter = null;
                captureInstance.Dispose();
            };

            captureInstance.StartRecording();
            Task.Run(() => CaptureFrame());

            while (IsRecording)
            {
                // Wait for the next frame to be captured
            }

            captureInstance.StopRecording();
        }


        private async Task CaptureFrame()
        {
            while (IsRecording)
            {
                var bounds = Screen.PrimaryScreen.Bounds;

                using var bitmap = new Bitmap(bounds.Width, bounds.Height, CapturePixelFormat);

                // capture frame
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                // compress frame
                using var ms = new MemoryStream();
                bitmap.Save(ms, CompressionFormat);
                var compressedFrame = ms.ToArray();

                // add frame to buffer
                _frameBuffer.Enqueue(compressedFrame);

                _frameTimestamps.Enqueue(DateTime.UtcNow);

                while (_frameBuffer.Count > BufferSize)
                {
                    _frameBuffer.TryDequeue(out var _);
                    _frameTimestamps.TryDequeue(out var _);
                }

                await Task.Delay(1000 / FrameRate); // Wait for the next frame
            }
        }

        public void SaveHighlight(string outputFilename, int quality)
        {
            var aviFile = $"{outputFilename}.avi";

            var width = Screen.PrimaryScreen.Bounds.Width;
            var height = Screen.PrimaryScreen.Bounds.Height;

            using (writer = new AviWriter(aviFile)
            {
                FramesPerSecond = FrameRate,
                EmitIndex1 = true,
            })
            {
                var encoder = new MJpegWpfVideoEncoder(width, height, quality);
                videoStream = writer.AddEncodingVideoStream(encoder, true, width, height);
                audioStream = writer.AddAudioStream(2, 44100, 16);

                while (!_frameBuffer.IsEmpty && !_audioBuffer.IsEmpty)
                {
                    if (_frameTimestamps.TryPeek(out var frameTimestamp) &&
                        _audioTimestamps.TryPeek(out var audioTimestamp))
                    {
                        if (frameTimestamp <= audioTimestamp)
                        {
                            // Frame should be written
                            if (_frameBuffer.TryDequeue(out var frameData))
                            {
                                _frameTimestamps.TryDequeue(out _);

                                using var ms = new MemoryStream(frameData);
                                using var bitmap = new Bitmap(ms);

                                var bits = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                                int bytes = Math.Abs(bits.Stride) * bitmap.Height;
                                var frameDataBytes = new byte[bytes];
                                System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, frameDataBytes, 0, frameDataBytes.Length);
                                bitmap.UnlockBits(bits);

                                videoStream.WriteFrame(true, frameDataBytes, 0, frameDataBytes.Length);
                            }
                        }
                        else
                        {
                            // Audio should be written
                            if (_audioBuffer.TryDequeue(out var audioData))
                            {
                                _audioTimestamps.TryDequeue(out _);
                                audioStream.WriteBlock(audioData, 0, audioData.Length);
                            }
                        }
                    }
                }
            }

            Debug.WriteLine("Starting Compression");
        }


        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            //File.WriteAllBytes("audio.raw", e.Buffer);

            if (audioStream == null) return;

            var audioData = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, audioData, audioData.Length);
            _audioBuffer.Enqueue(audioData);
            _audioTimestamps.Enqueue(DateTime.UtcNow);

        }

        public void Stop()
        {
            IsRecording = false;
            if (captureInstance != null)
            {
                captureInstance.StopRecording();
                captureInstance.Dispose();
                captureInstance = null;

                recordedAudioWriter.Dispose();
                recordedAudioWriter = null;
            }
        }

    }
}
