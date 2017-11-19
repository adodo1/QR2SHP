using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using MonoShapelib;
using System.Linq;

namespace QR2SHP
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            
        }


        private bool ScanQRCode(Screen screen, Bitmap fullImage, Rectangle cropRect, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap(cropRect.Width, cropRect.Height))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                                    cropRect,
                                    GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    rect = new Rectangle(cropRect.Left + (int)minX, cropRect.Top + (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private Rectangle GetScanRect(int width, int height, int index, out double stretch)
        {
            stretch = 1;
            if (index < 5)
            {
                const int div = 5;
                int w = width * 3 / div;
                int h = height * 3 / div;
                Point[] pt = new Point[5] {
                    new Point(1, 1),

                    new Point(0, 0),
                    new Point(0, 2),
                    new Point(2, 0),
                    new Point(2, 2),
                };
                return new Rectangle(pt[index].X * width / div, pt[index].Y * height / div, w, h);
            }
            {
                const int base_index = 5;
                if (index < base_index + 6)
                {
                    double[] s = new double[] {
                        1,
                        2,
                        3,
                        4,
                        6,
                        8
                    };
                    stretch = 1 / s[index - base_index];
                    return new Rectangle(0, 0, width, height);
                }
            }
            {
                const int base_index = 11;
                if (index < base_index + 8)
                {
                    const int hdiv = 7;
                    const int vdiv = 5;
                    int w = width * 3 / hdiv;
                    int h = height * 3 / vdiv;
                    Point[] pt = new Point[8] {
                        new Point(1, 1),
                        new Point(3, 1),

                        new Point(0, 0),
                        new Point(0, 2),

                        new Point(2, 0),
                        new Point(2, 2),

                        new Point(4, 0),
                        new Point(4, 2),
                    };
                    return new Rectangle(pt[index - base_index].X * width / hdiv, pt[index - base_index].Y * height / vdiv, w, h);
                }
            }
            return new Rectangle(0, 0, 0, 0);
        }

        private bool ScanQRCodeStretch(Screen screen, Bitmap fullImage, Rectangle cropRect, double mul, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap((int)(cropRect.Width * mul), (int)(cropRect.Height * mul)))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                    cropRect,
                                    GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    rect = new Rectangle(cropRect.Left + (int)(minX / mul), cropRect.Top + (int)(minY / mul), (int)((maxX - minX) / mul), (int)((maxY - minY) / mul));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private void ScanScreenQRCode(bool ss_only)
        {
            Thread.Sleep(100);
            foreach (Screen screen in Screen.AllScreens) {
                Point screen_size = Utils.GetScreenPhysicalSize();
                using (Bitmap fullImage = new Bitmap(screen_size.X, screen_size.Y)) {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    bool decode_fail = false;
                    for (int i = 0; i < 100; i++)
                    {
                        double stretch;
                        Rectangle cropRect = GetScanRect(fullImage.Width, fullImage.Height, i, out stretch);
                        if (cropRect.Width == 0)
                            break;

                        string url;
                        Rectangle rect;
                        if (stretch == 1 ? ScanQRCode(screen, fullImage, cropRect, out url, out rect) :
                                           ScanQRCodeStretch(screen, fullImage, cropRect, stretch, out url, out rect))
                        {
                            MessageBox.Show(url);
                            return;
                            //var success = controller.AddServerBySSURL(url);
                            //QRCodeSplashForm splash = new QRCodeSplashForm();
                            //if (success) {
                            //    splash.FormClosed += splash_FormClosed;
                            //}
                            //else if (!ss_only)
                            //{
                            //    _urlToOpen = url;
                            //    //if (url.StartsWith("http://") || url.StartsWith("https://"))
                            //    //    splash.FormClosed += openURLFromQRCode;
                            //    //else
                            //    splash.FormClosed += showURLFromQRCode;
                            //}
                            //else
                            //{
                            //    decode_fail = true;
                            //    continue;
                            //}
                            //splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
                            //double dpi = Screen.PrimaryScreen.Bounds.Width / (double)screen_size.X;
                            //splash.TargetRect = new Rectangle(
                            //    (int)(rect.Left * dpi + screen.Bounds.X),
                            //    (int)(rect.Top * dpi + screen.Bounds.Y),
                            //    (int)(rect.Width * dpi),
                            //    (int)(rect.Height * dpi));
                            //splash.Size = new Size(fullImage.Width, fullImage.Height);
                            //splash.Show();
                            //return;
                        }
                    }
                    if (decode_fail)
                    {
                        MessageBox.Show("Failed to decode QRCode");
                        return;
                    }
                }
            }
            MessageBox.Show("No QRCode found. Try to zoom in or move it to the center of the screen.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ScanScreenQRCode(false);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //QRCode code = ZXing.QrCode.Internal.Encoder.encode("wxp://f2f0_54rn3AJaTtepbaTzGdWgvBHiPofTsxX", ErrorCorrectionLevel.H);

            Dictionary<EncodeHintType, object> hints = new Dictionary<EncodeHintType, object>();
            hints.Add(EncodeHintType.QR_VERSION, 5);
            QRCode code = ZXing.QrCode.Internal.Encoder.encode("HTTPS://QR.ALIPAY.COM/FKX07116M1SFFZ2BP5CIE4", ErrorCorrectionLevel.H, hints);
            //wxp://f2f0_54rn3AJaTtepbaTzGdWgvBHiPofTsxX
            //HTTPS://QR.ALIPAY.COM/FKX07116M1SFFZ2BP5CIE4
            ByteMatrix matrix = code.Matrix;
            ToSHP(matrix, "./QRCODE");



            //int width = 256;
            //for (int i = 0; i < 8; i++)
            //{
            //    QRCode code = ZXing.QrCode.Internal.Encoder.encode("wxp://f2f0_54rn3AJaTtepbaTzGdWgvBHiPofTsxX", ErrorCorrectionLevel.H);
            //    ByteMatrix m = code.Matrix;
            //    int blockSize = Math.Max(width / (m.Width + 2), 1);
            //    Bitmap drawArea = new Bitmap(((m.Width + 2) * blockSize), ((m.Height + 2) * blockSize));
            //    using (Graphics g = Graphics.FromImage(drawArea)) {
            //        g.Clear(Color.White);
            //        using (Brush b = new SolidBrush(Color.Black)) {
            //            for (int row = 0; row < m.Width; row++)
            //            {
            //                for (int col = 0; col < m.Height; col++)
            //                {
            //                    if (m[row, col] != 0)
            //                    {
            //                        g.FillRectangle(b, blockSize * (row + 1), blockSize * (col + 1),
            //                            blockSize, blockSize);
            //                    }
            //                }
            //            }
            //        }
            //        //Bitmap ngnl = Resources.ngnl;
            //        //int div = 13, div_l = 5, div_r = 8;
            //        //int l = (m.Width * div_l + div - 1) / div * blockSize, r = (m.Width * div_r + div - 1) / div * blockSize;
            //        //g.DrawImage(ngnl, new Rectangle(l + blockSize, l + blockSize, r - l, r - l));
            //    }
            //    drawArea.Save(string.Format("e:\\temp\\{0}.jpg", i));
            //}


        }

        /// <summary>
        /// 生产SHP
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="fullname"></param>
        private void ToSHP(ByteMatrix matrix, string fullname)
        {
            double size = 6;
            SHPHandle hSHP = null;
            DBFHandle hDBF = null;
            try {
                SHPT shptype = SHPT.POLYGON;
                // 创建SHP文件
                hSHP = SHPHandle.Create(fullname, shptype);
                if (hSHP == null) throw new Exception("Unable to create SHP:" + fullname);
                // 创建DBF文件
                hDBF = DBFHandle.Create(fullname);
                if (hDBF == null) throw new Exception("Unable to create DBF:" + fullname);
                if (hDBF.AddField("_row", FT.Integer, 10, 0) < 0)
                    throw new Exception("DBFHandle.AddField(_row,Integer,10,0) failed.");
                if (hDBF.AddField("_col", FT.Integer, 10, 0) < 0)
                    throw new Exception("DBFHandle.AddField(_col,Integer,10,0) failed.");
                  
                // 绘制二维码
                int record_index = 0;
                for (int row = 0; row < matrix.Width; row++) {
                    for (int col = 0; col < matrix.Height; col++) {
                        if (matrix[row, col] == 0) continue;

                        // 构造SHPRecord
                        SHPRecord record = new SHPRecord() { ShapeType = shptype };
                        // 分段开始
                        record.Parts.Add(record.Points.Count);
                        // 添加4个角点
                        double ox = row * size + 300;
                        double oy = matrix.Height * size - col * size;
                        record.Points.Add(new double[] { ox, oy, 0, 0 });
                        record.Points.Add(new double[] { ox + size, oy, 0, 0 });
                        record.Points.Add(new double[] { ox + size, oy + size, 0, 0 });
                        record.Points.Add(new double[] { ox, oy + size, 0, 0 });
                        // 写图形
                        int num_parts = record.NumberOfParts;       // 总共分为几段
                        int[] parts = record.Parts.ToArray();       // 每一个分段的起始节点索引
                        int num_points = record.NumberOfPoints;     // 所有节点总数
                        double[] xs = new double[num_points];       // 所有节点X坐标
                        double[] ys = new double[num_points];       // 所有节点Y坐标
                        double[] zs = new double[num_points];       // 所有节点Z坐标
                        double[] ms = new double[num_points];       // 所有节点M坐标
                        for (int n = 0; n < num_points; n++) {
                            xs[n] = record.Points[n][0];            // X坐标
                            ys[n] = record.Points[n][1];            // Y坐标
                            zs[n] = record.Points[n][2];            // Z值
                            ms[n] = record.Points[n][3];            // M值
                        }
                        // PS: 节点 "逆时针"是加 "顺时针"是减
                        SHPObject shpobj = SHPObject.Create(shptype,    // 图形类别
                                                            -1,         // 图形ID -1表示新增
                                                            num_parts,  // 总共分为几段
                                                            parts,      // 每一个分段的起始节点索引
                                                            null,       // 每段的类别
                                                            num_points, // 所有节点总数
                                                            xs,         // 所有节点的X坐标
                                                            ys,         // 所有节点的Y坐标
                                                            zs,         // 所有节点的Z值
                                                            ms);        // 所有节点的M值
                        hSHP.WriteObject(-1, shpobj);
                        // 写属性
                        //hDBF.WriteNULLAttribute(record_index, 0);
                        hDBF.WriteDoubleAttribute(record_index, 0, row);
                        hDBF.WriteDoubleAttribute(record_index, 1, col);
                        record_index++;
                    }
			    }

            }
            catch (Exception) {
                throw;
            }
            finally {
                if (hSHP != null) hSHP.Close();
                if (hDBF != null) hDBF.Close();
            }
        }


    }
}
