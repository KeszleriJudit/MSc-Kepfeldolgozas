using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
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
using Image = System.Windows.Controls.Image;
using SudokuChecker.MVVM;
using SudokuChecker.Functionalities;

namespace SudokuChecker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            List<ProgramFunction> functions = Enum.GetValues(typeof(ProgramFunction)).Cast<ProgramFunction>().ToList();
            this.functions.ItemsSource = functions.ConvertAll(x => x.ToString());
            this.functions.SelectedIndex = 0;

            MainWindowViewModel.DisplayInputImages += DisplayInputImage;
            MainWindowViewModel.DisplayOutputImages += DisplayOutputImage;
        }

        private void DisplayInputImage(Bitmap inputImage)
        {
            Grid grid = this.InputImageArea;
            this.Display(grid, inputImage);
        }

        private void DisplayOutputImage(Bitmap outputImage)
        {
            Grid grid = this.OutputImageArea;
            this.Display(grid, outputImage);
        }

        private void Display(Grid grid, Bitmap bitmap)
        {
            this.Clear(grid);
            Image image = this.ConvertBitmapToImage(grid, bitmap);
            grid.Children.Add(image);
        }

        private void Clear(Grid grid)
        {
            if (grid.Children.Count > 0)
            {
                grid.Children.Clear();
            }
        }

        private Image ConvertBitmapToImage(Grid grid, Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            double ratio = this.GetRatio(grid, bitmapImage);
            Image image = new Image();
            image.Source = bitmapImage;
            image.Width = bitmapImage.Width * ratio;
            image.Height = bitmapImage.Height * ratio;
            image.Margin = new Thickness { Bottom = 0, Left = 0, Right = 0, Top = 0 };

            return image;
        }

        private double GetRatio(Grid grid, BitmapImage bitmapImage)
        {
            double widthRatio = grid.ActualWidth / bitmapImage.Width;
            double heighRatio = grid.ActualHeight / bitmapImage.Height;
            double minratio = Math.Min(widthRatio, heighRatio);
            return minratio;
        }
    }
}
