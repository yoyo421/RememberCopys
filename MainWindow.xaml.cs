using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RememberCopys
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // at class level
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private LowLevelKeyboardListener _listener;

        List<object> OLD_IData = new List<object>();
        Dictionary<Key, int> Keys = new Dictionary< Key, int>
        {
            { Key.D1, 0 },
            { Key.D2, 1 },
            { Key.D3, 2 },
            { Key.D4, 3 },
            { Key.D5, 4 },
            { Key.D6, 5 },
            { Key.D7, 6 },
            { Key.D8, 7 },
            { Key.D9, 8 }
        };

        List<string> supportedFormats = new List<string>()
        {
            DataFormats.Bitmap,
            DataFormats.Text,
        };

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _listener = new LowLevelKeyboardListener(PresentationSource.FromVisual(this));
            _listener.keyDown = keyDown;

            ListBOX.SelectionChanged += ListBOX_SelectionChanged;

            OnTop.Checked += OnTop_Checked;
            OnTop.Unchecked += OnTop_Unchecked;

            _listener.HookKeyboard();

            Timer timer = new Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void ListBOX_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (itemElement ie in ListBOX.Items)
            {
                ie.Foreground = ie.solid;
            }
        }

        private void OnTop_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void OnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private bool keyDown(object sender, KeyEventArgs e)
        {
            bool Handled = false;

            if (!Keyboard.IsKeyToggled(Key.Scroll))
            {
                return Handled;
            }

            if (Keys.ContainsKey(e.Key))
            {
                int index = Keys[e.Key];
                Handled = true;
                try
                {
                    ListBOX.SelectedIndex = index;
                }
                catch
                {}
            }
            return Handled;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                //foreach (FieldInfo type in typeof(DataFormats).GetFields())
                //{
                //    if (Clipboard.GetDataObject().GetDataPresent(type.GetValue("") as string))
                //    {
                //        Console.WriteLine(type.GetValue("") as string);
                //    }
                //}
                try
                {
                    transfareData IDataTranfare = new transfareData();
                    foreach (string _supported in supportedFormats)
                    {
                        if (!Clipboard.GetDataObject().GetDataPresent(_supported))
                        {
                            continue;
                        }

                        if (_supported == DataFormats.Bitmap)
                        {
                            Image image = new Image();
                            System.Windows.Interop.InteropBitmap interopBitmap = (System.Windows.Interop.InteropBitmap)Clipboard.GetImage();
                            image.Source = interopBitmap;
                            IDataTranfare.copyValue = image;
                            BitmapFrame interopBitmap1 = CreateResizedImage(interopBitmap, Math.Min(interopBitmap.PixelWidth, 100), Math.Min(interopBitmap.PixelHeight, 100), 0);
                            Image image2 = new Image();
                            image2.Source = interopBitmap1;
                            IDataTranfare.contentValue = image2;
                            Int32Rect rect = new Int32Rect(0, 0, interopBitmap1.PixelWidth, interopBitmap1.PixelHeight);
                            int stride = interopBitmap1.PixelWidth * (interopBitmap1.Format.BitsPerPixel + 7) / 8;
                            int arrayLength = stride * interopBitmap1.PixelHeight;
                            int[] arr = new int[arrayLength];
                            interopBitmap1.CopyPixels(rect, arr, stride, 0);

                            unchecked
                            {
                                int sum = 0;
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    sum += arr[i];
                                }
                                IDataTranfare.savedValue = sum / arr.Length;
                            }
                        }
                        else if (_supported == DataFormats.Text)
                        {
                            IDataTranfare.copyValue = Clipboard.GetText().Replace("\n", "").Replace("\r", "");
                            IDataTranfare.savedValue = IDataTranfare.copyValue;
                            IDataTranfare.contentValue = IDataTranfare.copyValue;
                        }

                        IDataTranfare.format = _supported;
                        break;
                    }

                    if (OLD_IData.Contains(IDataTranfare.savedValue) || IDataTranfare.format == null)
                    {
                        return;
                    }
                    OLD_IData.Add(IDataTranfare.savedValue);
                    ListBOX.Items.Insert(0, new itemElement(ListBOX, OLD_IData, IDataTranfare));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }));
        }

        private BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        private class transfareData
        {
            public transfareData() {}

            public object copyValue { get; set; }
            public object contentValue { get; set; }
            public object savedValue { get; set; }
            public string format { get; set; }
        }

        private class itemElement : ListBoxItem
        {
            private ListBox IC;
            private List<object> OLD_IData;
            private transfareData IDataTranfare;

            public Brush solid { get; private set; }

            public itemElement(ListBox IC, List<object> OLD_IData, transfareData IDataTranfare)
            {
                this.IC = IC;
                this.OLD_IData = OLD_IData;
                this.IDataTranfare = IDataTranfare;
                this.Foreground = new SolidColorBrush(Colors.Black);
                this.Content = this.IDataTranfare.contentValue;
                solid = this.Foreground;
                this.MouseDoubleClick += ItemElement_MouseDoubleClick;
            }

            private void ItemElement_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                if (e.ButtonState == e.RightButton)
                {
                    if (solid != this.Foreground)
                    {
                        OLD_IData.Remove(this.IDataTranfare.savedValue);
                        IC.Items.Remove(this);
                    }
                    this.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (e.ButtonState == e.LeftButton)
                {
                    if (this.IDataTranfare.format == DataFormats.Bitmap)
                    {
                        Clipboard.SetData(this.IDataTranfare.format, (this.IDataTranfare.copyValue as Image).Source);
                    }
                    if (this.IDataTranfare.format == DataFormats.Text)
                    {
                        Clipboard.SetData(this.IDataTranfare.format, this.IDataTranfare.copyValue);
                    }
                    this.Foreground = new SolidColorBrush(Colors.LimeGreen);

                    Task.Delay(250).ContinueWith(t =>
                    {
                        Dispatcher.Invoke((Action)(() =>
                        {
                            this.Foreground = new SolidColorBrush(Colors.Black);
                        }));
                    });
                }
            }
        }
    }
}
