using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Orion.Common.Video.Compression
{
    public class JpgCompression : IDisposable
    {
        private static readonly ImageCodecInfo JpegEncoderInfo = GetEncoderInfoStatic();
        private readonly EncoderParameters _encoderParams;

        public JpgCompression(long quality)
        {
            EncoderParameter parameter = new EncoderParameter(Encoder.Quality, quality);
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = parameter;
        }

        public void Dispose()
        {
            if (_encoderParams != null)
            {
                _encoderParams.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public byte[] Compress(Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, JpegEncoderInfo, _encoderParams);
                return stream.ToArray();
            }
        }

        public void Compress(Bitmap bmp, Stream targetStream)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));
            if (targetStream == null) throw new ArgumentNullException(nameof(targetStream));
            bmp.Save(targetStream, JpegEncoderInfo, _encoderParams);
        }

        private static ImageCodecInfo GetEncoderInfoStatic()
        {
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < imageEncoders.Length; i++)
            {
                if (imageEncoders[i].MimeType == "image/jpeg")
                {
                    return imageEncoders[i];
                }
            }
            throw new InvalidOperationException("JPEG encoder not found");
        }
    }
}