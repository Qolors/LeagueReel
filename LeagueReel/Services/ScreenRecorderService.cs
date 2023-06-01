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
using SharpAvi;
using SharpAvi.Output;
using SharpAvi.Codecs;

namespace LeagueReel.Services
{
    public class ScreenRecorderService
    {
        private const int FrameRate = 30;
        private const int BufferSize = FrameRate * 15;
        private const PixelFormat CapturePixelFormat = PixelFormat.Format32bppArgb;
        private ImageFormat CompressionFormat = ImageFormat.Jpeg;
        private ConcurrentQueue<byte[]> _frameBuffer = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> _audioBuffer = new ConcurrentQueue<byte[]>();
        private WaveInEvent waveSource = null;
        private VideoCompressorService videoCompressorService;
        public volatile bool IsRecording = false;
        private bool isSaving = false;

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

            waveSource = new WaveInEvent();
            waveSource.DeviceNumber = 0;
            waveSource.WaveFormat = new WaveFormat(44100, 2);
            waveSource.DataAvailable += WaveSource_DataAvailable;
            waveSource.StartRecording();

            while (IsRecording)
            {
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

                while (_frameBuffer.Count > BufferSize)
                {
                    _frameBuffer.TryDequeue(out var _);
                }
            }

            waveSource.StopRecording();
            waveSource.Dispose();
            waveSource = null;
        }

        public void SaveHighlight(string outputFilename, int quality)
        {
            // copy frames from buffer to local list2
            //isSaving = true;

            // copy frames from buffer to local list
            var frames = _frameBuffer.ToList();
            var audioFrames = _audioBuffer.ToList();

            var aviFile = $"{outputFilename}.avi";

            var width = Screen.PrimaryScreen.Bounds.Width;
            var height = Screen.PrimaryScreen.Bounds.Height;

            using (var writer = new AviWriter(aviFile)
            {
                FramesPerSecond = 30,
                EmitIndex1 = true,
            })
            {
                var encoder = new MJpegWpfVideoEncoder(width, height, quality);
                var videoStream = writer.AddEncodingVideoStream(encoder, true, width, height);
                var audioStream = writer.AddAudioStream(channelCount: 2, samplesPerSecond: 44100);

                

                int frameIndex = 0, audioIndex = 0;
                while (frameIndex < frames.Count && audioIndex < audioFrames.Count)
                {
                    using var ms = new MemoryStream(frames[frameIndex++]);
                    var audioFrame = audioFrames[audioIndex++];

                    using var bitmap = new Bitmap(ms);
                    

                    var bits = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                    int bytes = Math.Abs(bits.Stride) * bitmap.Height;
                    var frameData = new byte[bytes];
                    System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, frameData, 0, frameData.Length);
                    bitmap.UnlockBits(bits);

                    videoStream.WriteFrame(true, frameData, 0, frameData.Length);
                    audioStream.WriteBlock(audioFrame, 0, audioFrame.Length);
                }
            }

            Debug.WriteLine("Starting Compression");

            // Task.Run(async () => await videoCompressorService.ConvertVideoAsync(aviFile, $"{outputFilename}.mp4"));
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            var audioData = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, audioData, audioData.Length);
            _audioBuffer.Enqueue(audioData);

            while (_audioBuffer.Count > BufferSize)
            {
                _audioBuffer.TryDequeue(out var _);
            }
        }

        public void Stop()
        {
            IsRecording = false;
            if (waveSource != null)
            {
                waveSource.StopRecording();
                waveSource.Dispose();
                waveSource = null;
            }
        }
    }
}
