using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp.Functionalities.Implementations
{
    public class Ps_Negalas : FunctionBase, FunctionInterface
    {
        private ConcurrentDictionary<byte, byte> lookUpTable;

        public Ps_Negalas(Logger logger) : base(ProgramFunction.Ps_Negalas, logger) 
        {
            this.lookUpTable = new ConcurrentDictionary<byte, byte>();
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

                        outputCurrentLine[x] = this.lookUpTable[oldBlue];
                        outputCurrentLine[x + 1] = this.lookUpTable[oldGreen];
                        outputCurrentLine[x + 2] = this.lookUpTable[oldRed];
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
                    Color newPixel = this.Negate(currentPixel);
                    newImage.SetPixel(i, j, newPixel);
                }
            }*/

            this.StopTimer();
            this.LogFunctionResult();
            this.ResetTimer();

            return newImage;
        }

        private Color Negate(Color pixel)
        {
            return Color.FromArgb(pixel.A, 
                this.lookUpTable[pixel.R], 
                this.lookUpTable[pixel.G],
                this.lookUpTable[pixel.B]);
        }

        private void FillLookUpTable()
        {
            for (int i = 0; i < 256; i++)
            {
                int value = 255-i;
                this.lookUpTable.TryAdd((byte)i, (byte)value);
            }
        }
    }
}
