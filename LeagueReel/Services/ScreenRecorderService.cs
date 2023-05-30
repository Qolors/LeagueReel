using SharpAvi;
using SharpAvi.Output;
using SharpAvi.Codecs;
using System;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LeagueReel.Services
{
    public class ScreenRecorderService
    {
        private const int FrameRate = 30;
        private const int BufferSize = FrameRate * 15; // enough for 30 seconds
        private const PixelFormat CapturePixelFormat = PixelFormat.Format32bppRgb;
        private ImageFormat CompressionFormat = ImageFormat.Jpeg;
        private ConcurrentQueue<byte[]> _frameBuffer = new ConcurrentQueue<byte[]>();

        public volatile bool IsRecording = false;

        public ScreenRecorderService()
        {
        }

        public void Start()
        {
            IsRecording = true;
            Task.Run(() => Recording());
        }

        private void Recording()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, CapturePixelFormat);

            while (IsRecording)
            {
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

                // ensure buffer doesn't grow too large
                while (_frameBuffer.Count > BufferSize)
                {
                    _frameBuffer.TryDequeue(out _);
                }
            }
        }

        public void SaveHighlight(string outputFilename, int quality)
        {
            // copy frames from buffer to local list2
            var frames = _frameBuffer.ToList();

            // open output file
            using var writer = new AviWriter(outputFilename)
            {
                FramesPerSecond = FrameRate,
                EmitIndex1 = true
            };

            // initialize video stream in output file
            var encoder = new MJpegWpfVideoEncoder(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, quality);

            var stream = writer.AddEncodingVideoStream(encoder);
            stream.Width = Screen.PrimaryScreen.Bounds.Width;
            stream.Height = Screen.PrimaryScreen.Bounds.Height;

            // iterate over frames
            foreach (var compressedFrame in frames)
            {
                using var ms = new MemoryStream(compressedFrame);
                using var bitmap = new Bitmap(ms);

                // lock bitmap data
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                // get address of bitmap data
                IntPtr ptr = bitmapData.Scan0;

                // copy bitmap data to byte array
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                // unlock bitmap data
                bitmap.UnlockBits(bitmapData);

                // write frame to output file
                stream.WriteFrame(true, rgbValues.AsSpan());
            }

            writer.Close();
        }


        public void Stop()
        {
            IsRecording = false;
        }
    }
}
