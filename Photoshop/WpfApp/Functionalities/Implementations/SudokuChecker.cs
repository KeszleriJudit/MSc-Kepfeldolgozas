﻿using IronOcr;
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
    public class SudokuChecker : FunctionBase, FunctionInterface
    {
        private ConcurrentDictionary<int, byte> lookUpTable;
        private int imageWidth;
        private int imageHeight;

        public SudokuChecker(Logger logger): base(ProgramFunction.SudokuChecker, logger)
        {
            this.lookUpTable = new ConcurrentDictionary<int, byte>();
        }

        public Bitmap ExecuteFunction(Bitmap inputImage)
        {
            this.StartTimer();
            this.lookUpTable.Clear();
            this.FillLookUpTable();

            this.imageWidth = inputImage.Width;
            this.imageHeight = inputImage.Height;
            Bitmap newImage = greyScaling(inputImage);
            newImage = gausBlur(newImage);
            //körbevágás sarokpont + éldetektor
            newImage = contrastStreching(newImage);

            var Ocr = new IronTesseract();
            // Hundreds of languages available 
            Ocr.Language = OcrLanguage.English;
            using (var Input = new OcrInput())
            {
                Input.AddImage("C:\\Users\\Judit\\Pictures\\Sudoku\\Image5.jpg");
                // Input.DeNoise();  optional  
                // Input.Deskew();   optional  
                IronOcr.OcrResult Result = Ocr.Read(Input);
                String result = Result.Text;
                Console.WriteLine(Result.Text);
                // Explore the OcrResult using IntelliSense
                Console.WriteLine("Finished");
            }

            this.StopTimer();
            this.LogFunctionResult();
            this.ResetTimer();

            return newImage;
        }

        private void FillLookUpTable()
        {
            for (int i = 0; i < 766; i++)
            {
                int value = i / 3;
                this.lookUpTable.TryAdd(i, (byte)value);
            }
        }

        private Bitmap greyScaling(Bitmap inputImage)
        {
            Bitmap newImage = new Bitmap(this.imageWidth, this.imageHeight);

            unsafe
            {
                BitmapData inputBitmapData = inputImage.LockBits(new Rectangle(0, 0, this.imageWidth, this.imageHeight), ImageLockMode.ReadOnly, inputImage.PixelFormat);
                BitmapData outputBitmapData = newImage.LockBits(new Rectangle(0, 0, this.imageWidth, this.imageHeight), ImageLockMode.WriteOnly, inputImage.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(inputImage.PixelFormat) / 8;
                int widthInBytes = this.imageWidth * bytesPerPixel;
                byte* inputPtrFirstPixel = (byte*)inputBitmapData.Scan0;
                byte* outputPtrFirstPixel = (byte*)outputBitmapData.Scan0;

                Parallel.For(0, this.imageHeight, y =>
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

            return newImage;
        }

        private unsafe Bitmap gausBlur(Bitmap inputImage)
        {
            Bitmap newImage = new Bitmap(this.imageWidth, this.imageHeight);
            Rectangle rectangle = new Rectangle(0, 0, this.imageWidth, this.imageHeight);
            int blurSize = 1;

            // make an exact copy of the bitmap provided
            using (Graphics graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(inputImage, new Rectangle(0, 0, this.imageWidth, this.imageHeight),
                    new Rectangle(0, 0, this.imageWidth, this.imageHeight), GraphicsUnit.Pixel);

            // Lock the bitmap's bits
            BitmapData blurredData = newImage.LockBits(new Rectangle(0, 0, this.imageWidth, this.imageHeight), ImageLockMode.ReadWrite, newImage.PixelFormat);

            // Get bits per pixel for current PixelFormat
            int bitsPerPixel = Image.GetPixelFormatSize(newImage.PixelFormat);

            // Get pointer to first line
            byte* scan0 = (byte*)blurredData.Scan0.ToPointer();

            // look at every pixel in the blur rectangle
            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx; (x < xx + blurSize && x < this.imageWidth); x++)
                    {
                        for (int y = yy; (y < yy + blurSize && y < this.imageHeight); y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            avgB += data[0]; // Blue
                            avgG += data[1]; // Green
                            avgR += data[2]; // Red

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (int x = xx; x < xx + blurSize && x < this.imageWidth && x < rectangle.Width; x++)
                    {
                        for (int y = yy; y < yy + blurSize && y < this.imageHeight && y < rectangle.Height; y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            // Change values
                            data[0] = (byte)avgB;
                            data[1] = (byte)avgG;
                            data[2] = (byte)avgR;
                        }
                    }
                }
            }

            // Unlock the bits
            newImage.UnlockBits(blurredData);

            return newImage;
        }

        private Bitmap contrastStreching(Bitmap inputImage)
        {
            double blackPointPercent = 0.05;
            double whitePointPercent = 0.2;
            Bitmap newImage = new Bitmap(this.imageWidth, this.imageHeight);
            //Lock bits for your source image into system memory
            Rectangle rect = new Rectangle(0, 0, this.imageWidth, this.imageHeight);
            BitmapData srcData = inputImage.LockBits(rect, ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            //Lock bits for your writable bitmap into system memory
            Rectangle rect2 = new Rectangle(0, 0, newImage.Width, newImage.Height);
            BitmapData destData = newImage.LockBits(rect2, ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            //Get the width of a single row of pixels in the bitmap
            int stride = srcData.Stride;

            //Scan for the first pixel data in bitmaps
            IntPtr srcScan0 = srcData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            var freq = new int[256];

            unsafe
            {
                //Create an array of pixel data from source image
                byte* src = (byte*)srcScan0;

                //Get the number of pixels for each intensity value
                for (int y = 0; y < newImage.Height; ++y)
                {
                    for (int x = 0; x < this.imageWidth; ++x)
                    {
                        freq[src[y * stride + x * 4]]++;
                    }
                }

                //Get the total number of pixels in the image
                int numPixels = this.imageWidth * this.imageHeight;

                //Set the minimum intensity value of an image (0 = black)
                int minI = 0;

                //Get the total number of black pixels
                var blackPixels = numPixels * blackPointPercent;

                //Set a variable for summing up the pixels that will turn black
                int accum = 0;

                //Sum up the darkest shades until you reach the total of black pixels
                while (minI < 255)
                {
                    accum += freq[minI];
                    if (accum > blackPixels) break;
                    minI++;
                }

                //Set the maximum intensity of an image (255 = white)
                int maxI = 255;

                //Set the total number of white pixels
                var whitePixels = numPixels * whitePointPercent;

                //Reset the summing variable back to 0
                accum = 0;

                //Sum up the pixels that are the lightest which will turn white
                while (maxI > 0)
                {
                    accum += freq[maxI];
                    if (accum > whitePixels) break;
                    maxI--;
                }

                //Part of a normalization equation that doesn't vary with each pixel
                double spread = 255d / (maxI - minI);

                //Create an array of pixel data from created image
                byte* dst = (byte*)destScan0;

                //Write new pixel data to the image
                for (int y = 0; y < this.imageHeight; ++y)
                {
                    for (int x = 0; x < this.imageWidth; ++x)
                    {
                        int i = y * stride + x * 4;

                        //Part of equation that varies with each pixel
                        double value = Math.Round((src[i] - minI) * spread);

                        byte val = (byte)(Math.Min(Math.Max(value, 0), 255));
                        dst[i] = val;
                        dst[i + 1] = val;
                        dst[i + 2] = val;
                        dst[i + 3] = 255;
                    }
                }
            }

            //Unlock bits from system memory
            inputImage.UnlockBits(srcData);
            newImage.UnlockBits(destData);

            return newImage;
        }
    }
}