using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BBSReader
{
    /// <summary>
    /// CoverDownloader.xaml 的交互逻辑
    /// </summary>
    public partial class CoverDownloader : Window
    {
        const int STD_WIDTH = 160;
        const int STD_HEIGHT = 200;

        public byte[] coverData;
        private Bitmap coverBm;
        private byte[] rawCoverData;

        public CoverDownloader()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rawCoverData = DownloadCoverData(CoverUrl.Text);
            coverBm = ResizeImage(rawCoverData, STD_WIDTH, STD_HEIGHT);
            coverData = ConvertToBytes(coverBm);
            CoverImg.Source = BitmapToImageSource(coverBm);
        }

        public static byte[] BatchProc(string url)
        {
            byte[] rawCoverData = DownloadCoverData(url);
            Bitmap bm = ResizeImage(rawCoverData, STD_WIDTH, STD_HEIGHT);
            return ConvertToBytes(bm);
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private static Bitmap ResizeImage(byte[] rawCoverData, int destWidth, int destHeight)
        {
            if (rawCoverData == null)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream(rawCoverData))
            {
                using (Bitmap raw = new Bitmap(ms))
                {
                    int w = 0;
                    int h = 0;
                    int rawWidth = raw.Width;
                    int rawHeight = raw.Height;
                    if (rawWidth > destWidth || rawHeight > destHeight)
                    {
                        if (rawWidth * destHeight > destWidth * rawHeight)
                        {
                            w = destWidth;
                            h = destWidth * rawHeight / rawWidth;
                        }
                        else
                        {
                            w = destHeight * rawWidth / rawHeight;
                            h = destHeight;
                        }
                    }
                    else
                    {
                        if (rawWidth * destHeight > destWidth * rawHeight)
                        {
                            w = destWidth;
                            h = destWidth * rawHeight / rawWidth;
                        }
                        else
                        {
                            w = destHeight * rawWidth / rawHeight;
                            h = destHeight;
                        }
                    }
                    Bitmap bitmap = new Bitmap(destWidth, destHeight);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(System.Drawing.Color.Transparent);
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(raw, new System.Drawing.Rectangle((destWidth - w) / 2, (destHeight - h) / 2, w, h), 0, 0, raw.Width, raw.Height, GraphicsUnit.Pixel);
                    }
                    return bitmap;
                }
            }
        }

        private static byte[] ConvertToBytes(Bitmap bitmap)
        {
            //System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
            //long[] quality = new long[1] { 100 };
            //System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            using (MemoryStream ms2 = new MemoryStream())
            {
                bitmap.Save(ms2, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms2.ToArray();
            }
        }

        private static byte[] DownloadCoverData(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUriString: url);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                if (resp.ContentLength <= 0)
                {
                    return null;
                }
                Stream stream = resp.GetResponseStream();
                byte[] buffer = new byte[resp.ContentLength];
                int offset = 0;
                int actRead = 0;
                do
                {
                    actRead = stream.Read(buffer, offset, buffer.Length - offset);
                    offset += actRead;
                } while (actRead > 0);

                return buffer;
            }
            catch (WebException)
            {
                return null;
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
