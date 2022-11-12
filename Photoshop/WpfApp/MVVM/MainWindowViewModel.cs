using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using WpfApp.WpfUiHelpers;
using System.IO;
using WpfApp.Functionalities;
using System.Drawing;
using System.Drawing.Imaging;

namespace WpfApp.MVVM
{
    public class MainWindowViewModel : Bindable
    {
        public delegate void EventHandler(Bitmap image);
        public static event EventHandler DisplayInputImages;
        public static event EventHandler DisplayOutputImages;

        private FunctionFactory functionFactory;
        private Logger logger;

        private string inputImageName;
        private Bitmap inputImage;
        private Bitmap outputImage;

        private string selectedFunction;
        private string lastFunction;

        public string SelectedFunction { get => this.selectedFunction; set { this.selectedFunction = value; OnPropertyChange(nameof(this.SelectedFunction)); } }
        public ObservableCollection<string> Logs { get => this.logger.Logs; }

        public ICommand LoadNewImage { get; private set; }
        public ICommand RunFunction { get; private set; }
        public ICommand SaveResult { get; private set; }

        public MainWindowViewModel()
        {
            this.functionFactory = new FunctionFactory();
            this.logger = this.functionFactory.GetLogger();

            this.LoadNewImage = new RelayCommand(this._LoadNewImage);
            this.RunFunction = new RelayCommand(this._RunFunction);
            this.SaveResult = new RelayCommand(this._SaveResult);
        }

        private void _LoadNewImage(object o)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image (*.jpg)|*.jpg|" +
                            "Image (*.png)|*.png";

            string imagePath = null;

            if (dialog.ShowDialog() == true)
            {
                imagePath = dialog.FileName;
            }

            if (imagePath != null || imagePath != string.Empty)
            {
                this.inputImageName = Path.GetFileNameWithoutExtension(imagePath);
                this.inputImage = new Bitmap(imagePath);
                DisplayInputImages(this.inputImage);

                this.logger.Log($"New image loaded: {imagePath}");
            }
        }

        private void _RunFunction(object o)
        {
            if (this.selectedFunction != null && this.inputImage != null)
            {
                this.logger.Log($"Function called: {selectedFunction}");

                this.lastFunction = this.selectedFunction;
                
                ProgramFunction function = (ProgramFunction)Enum.Parse(typeof(ProgramFunction), this.selectedFunction, true);
                FunctionInterface functionImplementation = this.functionFactory.GetFunction(function);

                this.outputImage = functionImplementation.ExecuteFunction(this.inputImage);
                DisplayOutputImages(this.outputImage);
            }
        }

        private void _SaveResult(object o)
        {
            if (this.outputImage != null)
            {
                string name = $"{Directory.GetCurrentDirectory()}\\{this.inputImageName} - {this.lastFunction}.png";
                this.outputImage.Save(name, ImageFormat.Png);

                this.logger.Log($"Output image saved: {name}");
            }
        }
    }
}
