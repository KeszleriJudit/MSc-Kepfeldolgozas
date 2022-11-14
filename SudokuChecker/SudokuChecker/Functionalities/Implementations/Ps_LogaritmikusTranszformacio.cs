using SudokuChecker.Functionalities.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuChecker.Functionalities.Implementations
{
    class Ps_LogaritmikusTranszformacio : FunctionBase, FunctionInterface
    {
        private ConcurrentDictionary<byte, double> lookUpTable;

        public Ps_LogaritmikusTranszformacio(Logger logger) : base(ProgramFunction.Ps_Logaritmikus_Transzformacio, logger)
        {
            this.lookUpTable = new ConcurrentDictionary<byte, double>();
        }

        public Bitmap ExecuteFunction(Bitmap inputImage)
        {
           this.StartTimer();
            this.lookUpTable.Clear();
            this.FillLookUpTable();
            int imageWidth = inputImage.Width;
            int imageHeight = inputImage.Height;
            Bitmap newImage = new Bitmap(imageWidth, imageHeight);
            int[] maxValues = this.GetMaxValues(inputImage, imageWidth, imageHeight);
            int maxValueR = maxValues[0];
            int maxValueG = maxValues[1];
            int maxValueB = maxValues[2];

            double maxR = maxValueR > 0 ? 255.0 / Math.Log10(1 + maxValueR) : 0;
            double maxG = maxValueG > 0 ? 255.0 / Math.Log10(1 + maxValueG) : 0;
            double maxB = maxValueB > 0 ? 255.0 / Math.Log10(1 + maxValueB) : 0;

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

                        /* oldBlue = Convert.ToByte(oldBlue == 0 ? 1 : oldBlue);
                         oldGreen = Convert.ToByte(oldGreen == 0 ?  1 : oldGreen);
                         oldRed = Convert.ToByte(oldRed == 0 ? 1 : oldRed);*/
                        /*double maxR = oldRed > 0 ? 255.0 / Math.Log10(1 + oldRed) : 0;
                        double maxG = oldGreen > 0 ? 255.0 / Math.Log10(1 + oldGreen) : 0;
                        double maxB = oldBlue > 0 ? 255.0 / Math.Log10(1 + oldBlue) : 0;*/

                        outputCurrentLine[x] = Convert.ToByte(this.lookUpTable[oldBlue] * maxB);
                        outputCurrentLine[x + 1] = Convert.ToByte(this.lookUpTable[oldGreen] * maxG);
                        outputCurrentLine[x + 2] = Convert.ToByte(this.lookUpTable[oldRed] * maxR);
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
                    Color newPixel = LogaritmicTransformation(currentPixel, maxR, maxG, maxB);
                    newImage.SetPixel(i, j, newPixel);
                }
            }*/

            this.StopTimer();
            this.LogFunctionResult();
            this.ResetTimer();

            return newImage;
        }

        private Color LogaritmicTransformation(Color pixel, double maxR, double maxG, double maxB)
        {
            return Color.FromArgb(pixel.A,
                 Convert.ToInt32(this.lookUpTable[pixel.R]*maxR),
                 Convert.ToInt32(this.lookUpTable[pixel.G]*maxG),
                 Convert.ToInt32(this.lookUpTable[pixel.B]*maxB));
        }

        private void FillLookUpTable()
        {
            for (int i = 0; i < 256; i++)
            {
                double value = Math.Log10(1 + i);
                this.lookUpTable.TryAdd((byte)i, value);
            }
        }

        private int[] GetMaxValues(Bitmap inputImage, int imageWidth, int imageHeight)
        {
            int[] maxValues = new int[3];
            maxValues[0] = 0;
            maxValues[1] = 0;
            maxValues[2] = 0;

            for (int i = 0; i < imageWidth; i++)
            {
                for (int j = 0; j < imageHeight; j++)
                {
                    Color currentPixel = inputImage.GetPixel(i, j);
                    maxValues[0] = currentPixel.R > maxValues[0] ? currentPixel.R : maxValues[0];
                    maxValues[1] = currentPixel.G > maxValues[1] ? currentPixel.G : maxValues[1];
                    maxValues[2] = currentPixel.B > maxValues[2] ? currentPixel.B : maxValues[2];
                   /* maxValues[0] = currentPixel.R + maxValues[0];
                    maxValues[1] = currentPixel.G + maxValues[1];
                    maxValues[2] = currentPixel.B + maxValues[2];*/
                }
            }

            return maxValues;
        }
    }
}
