using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace KinectRecognizeSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        ColorFrameReader colorFrameReader;
        FrameDescription colorFrameDesc;
        byte[] colorBuffer;

        BodyFrameReader bodyFrameReader;
        /// <summary> bodyデータ </summary>
        Body[] bodies;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                kinect = KinectSensor.GetDefault();
                if (kinect == null)
                {
                    throw new Exception("Kinectを開けません");
                }

                kinect.Open();

                // カラー画像の情報を作成する(BGRAフォーマット)
                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                
                Console.WriteLine(colorFrameDesc.Width + "," + colorFrameDesc.Height);
                // カラーリーダーを開く
                colorFrameReader = kinect.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

                // Bodyを入れる配列を作る
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                // ボディーリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // カラーフレームを取得する
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                // BGRAデータを取得する
                colorBuffer = new byte[colorFrameDesc.Width * colorFrameDesc.Height * colorFrameDesc.BytesPerPixel];
                colorFrame.CopyConvertedFrameDataToArray(colorBuffer, ColorImageFormat.Bgra);

                // ビットマップにする
                ImageColor.Source = BitmapSource.Create(colorFrameDesc.Width, colorFrameDesc.Height, 96, 96,
                    PixelFormats.Bgra32, null, colorBuffer, colorFrameDesc.Width * (int)colorFrameDesc.BytesPerPixel);
            }
        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            UpdateBodyFrame(e);
            DrawBodyFrame();
        }

        // ボディの更新
        private void UpdateBodyFrame(BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                // ボディデータを取得する
                bodyFrame.GetAndRefreshBodyData(bodies);
            }
        }

        // ボディの表示
        private void DrawBodyFrame()
        {
            CanvasBody.Children.Clear();
            var d = bodies[0].Joints[JointType.HandRight];
            if (d.TrackingState == TrackingState.Tracked) DrawEllipse(d, 10, Brushes.Blue);
        }

        private void DrawEllipse(Joint joint, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // カメラ座標系をDepth座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            Canvas.SetLeft(ellipse, (point.X - (R / 2))/2);
            Canvas.SetTop(ellipse, (point.Y - (R / 2))/2);

            CanvasBody.Children.Add(ellipse);
        }
    }
}
