using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Functionalities.Implementations
{
    class Ps_Szurkites : FunctionBase, FunctionInterface
    {
        private ConcurrentDictionary<int, byte> lookUpTable;

        public Ps_Szurkites(Logger logger) : base(ProgramFunction.Ps_Szurkites, logger)
        {
            this.lookUpTable = new ConcurrentDictionary<int, byte>();
        }

        public Bitmap ExecuteFunction(Bitmap inputImage)
        {
            this.StartTimer();
            this.lookUpTable.Clear();
            this.FillLookUpTable();
            int imageWidth = inputImage.Width;
            int imageHeight = inputImage.Height;
            Bitmap newImage = new Bitmap(imageWidth, imageHeight);

            // Parallel solution:
            unsafe
            {
                BitmapData inputBitmapData = inputImage.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadOnly, inputImage.PixelFormat);
                BitmapData outputBitmapData = newImage.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, inputImage.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(inputImage.PixelFormat) / 8;
                int widthInBytes = imageWidth * bytesPerPixel;
                byte* inputPtrFirstPixel = (byte*)inputBitmapData.Scan0;
                byte* outputPtrFirstPixel = (byte*)outputBitmapData.Scan0;

                Parallel.For(0, imageHeight, y =>
                {
                    byte* inputCurrentLine = inputPtrFirstPixel + (y * inputBitmapData.Stride);
                    byte* outputCurrentLine = outputPtrFirstPixel + (y * outputBitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        byte oldBlue = inputCurrentLine[x];
                        byte oldGreen = inputCurrentLine[x + 1];
                        byte oldRed = inputCurrentLine[x + 2];

                        int szum = oldBlue + oldGreen + oldRed;
                        byte value = this.lookUpTable[szum];

                        outputCurrentLine[x] = value;
                        outputCurrentLine[x + 1] = value;
                        outputCurrentLine[x + 2] = value;
                    }
                });
                inputImage.UnlockBits(inputBitmapData);
                newImage.UnlockBits(outputBitmapData);
            }

            // NOT parallel solution:

            /*for (int i = 0; i < imageWidth; i++)
            {
                for (int j = 0; j < imageHeight; j++)
                {
                    Color currentPixel = inputImage.GetPixel(i, j);
                    Color newPixel = GreyScaling(currentPixel);
                    newImage.SetPixel(i, j, newPixel);
                }
            }*/

            this.StopTimer();
            this.LogFunctionResult();
            this.ResetTimer();

            return newImage;
        }
        private Color GreyScaling(Color pixel)
        {
            return Color.FromArgb(pixel.A,
                 this.lookUpTable[pixel.R],
                 this.lookUpTable[pixel.G],
                 this.lookUpTable[pixel.B]);
        }

        private void FillLookUpTable()
        {
            for (int i = 0; i < 766; i++)
            {
                int value = i/3;
                this.lookUpTable.TryAdd(i, (byte)value);
            }
        }
    }
}
