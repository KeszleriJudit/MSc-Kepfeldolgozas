using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using IronOcr;
using SudokuChecker.Functionalities.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SudokuChecker.Functionalities.Implementations
{
    public class SudokuCheckerImplementation : FunctionBase, FunctionInterface
    {
        private ConcurrentDictionary<int, byte> lookUpTable;
        private int imageWidth;
        private int imageHeight;
        private int counter;

        public SudokuCheckerImplementation(Logger logger) : base(ProgramFunction.SudokuChecker, logger)
        {
            this.lookUpTable = new ConcurrentDictionary<int, byte>();
            this.counter = 0;
        }

        public Bitmap ExecuteFunction(Bitmap inputImage)
        {
            this.StartTimer();
            this.lookUpTable.Clear();
            this.FillLookUpTable();

            this.imageWidth = inputImage.Width;
            this.imageHeight = inputImage.Height;

            /**
             * Doing some pre image processing
             */
            Bitmap greyscaledImage = greyScaling(inputImage);
            Bitmap gausBlurredImage = gausBlur(greyscaledImage);
            Bitmap contrastStrechedImage = contrastStreching(gausBlurredImage);
            
            string name = $"C:/Users/Judit/Desktop/SudokuSolver/{this.counter}-sudoku.png";
            contrastStrechedImage.Save(name, ImageFormat.Png);

            /**
             * Calling the python script to detect and return with the relevant image part
             */
            RunPythonScript("contours.py", this.counter.ToString());

            string processedImagePath = $"C:/Users/Judit/Desktop/SudokuSolver/{this.counter}-sudoku-processed.png";
            Bitmap processedImage = new Bitmap(processedImagePath);
            this.counter++;

            /**
             * Cutting the image into smaller sudoku units
             */
            int processedImageHeight = processedImage.Height;
            int processedImageWidth = processedImage.Width;
            double heightValue = processedImageHeight / 9;
            double widthValue = processedImageWidth / 9;
            int cellEdgeHeight = (int)Math.Floor(heightValue);
            int cellEdgeWidth = (int)Math.Floor(widthValue);

            Mat resizedImage = new Mat();
            CvInvoke.Resize(processedImage.ToMat(), resizedImage, new System.Drawing.Size(cellEdgeWidth * 9, cellEdgeHeight * 9));
            Bitmap resizedImageBitmap = resizedImage.ToBitmap();
            int row = 0;
            int column = 0;

            List<Bitmap> units = new List<Bitmap>();
            for (int i = 0; i < resizedImageBitmap.Height; i += cellEdgeHeight)
            {
                for (int j = 0; j < resizedImageBitmap.Width; j += cellEdgeWidth)
                {
                    Bitmap unit = new Bitmap(cellEdgeWidth, cellEdgeHeight);
                    for (int k = 0; k < cellEdgeHeight; k++)
                    {
                        for (int l = 0; l < cellEdgeWidth; l++)
                        {
                            Color pixel = resizedImageBitmap.GetPixel(j + l, i + k);
                            unit.SetPixel(l, k, pixel);
                        }
                    }
                    units.Add(unit);
                    unit.Save($"C:/Users/Judit/Desktop/SudokuSolver/units/unit{row}{column}.png", ImageFormat.Png);
                    column++;
                }
                row++;
                column = 0;
            }


            /**
             * Code below is for the processed image to read the numbers
             */
            var Ocr = new IronTesseract();
            List<string> sudokuInput = new List<string>();
            Ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleChar;
            Ocr.Configuration.WhiteListCharacters = "123456789";
            Ocr.Configuration.TesseractVariables["load_system_dawg"] = false;
            Ocr.Configuration.ReadBarCodes = false;

            for (int i = 0; i < units.Count; i++)
            {
                var Input = new OcrInput(units[i]);
                Input.Deskew();
                Input.Invert();
                Input.Binarize();
                Input.SaveAsImages($"C:/Users/Judit/Desktop/SudokuSolver/AfterOcr/afterOcr{i}");
                try
                {
                    IronOcr.OcrResult Result = Ocr.Read(Input);
                    if (Result.Text == "")
                    {
                        sudokuInput.Add("0");
                    }
                    else
                    {
                        sudokuInput.Add(Result.Text);
                    }
                }
                catch (Exception e)
                {
                    sudokuInput.Add("0");
                    MessageBox.Show(e.Message);
                    logger.Log("Failed to read");
                }

            }

            /**
             * Log the values read from the image
             */
            int counter = 0;
            string line = "";
            string sudokuTable = "";

            for (int i = 0; i < sudokuInput.Count; i++)
            {
                line += sudokuInput[i] + " ";
                sudokuTable += sudokuInput[i];
                if (counter == 8)
                {
                    logger.Log(line);
                    counter = 0;
                    line = "";
                }
                else
                {
                    counter++;
                }
            }

            /**
             * Call the sudoku solver python script
             */
            RunPythonScript("solver.py", sudokuTable);
            Console.WriteLine("Finished");

            this.StopTimer();
            this.LogFunctionResult();
            this.ResetTimer();

            return resizedImageBitmap;
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

            using (Graphics graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(inputImage, new Rectangle(0, 0, this.imageWidth, this.imageHeight),
                    new Rectangle(0, 0, this.imageWidth, this.imageHeight), GraphicsUnit.Pixel);

            BitmapData blurredData = newImage.LockBits(new Rectangle(0, 0, this.imageWidth, this.imageHeight), ImageLockMode.ReadWrite, newImage.PixelFormat);
            int bitsPerPixel = Image.GetPixelFormatSize(newImage.PixelFormat);
            byte* scan0 = (byte*)blurredData.Scan0.ToPointer();

            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    for (int x = xx; (x < xx + blurSize && x < this.imageWidth); x++)
                    {
                        for (int y = yy; (y < yy + blurSize && y < this.imageHeight); y++)
                        {
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;
                            avgB += data[0];
                            avgG += data[1];
                            avgR += data[2];
                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    for (int x = xx; x < xx + blurSize && x < this.imageWidth && x < rectangle.Width; x++)
                    {
                        for (int y = yy; y < yy + blurSize && y < this.imageHeight && y < rectangle.Height; y++)
                        {
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;
                            data[0] = (byte)avgB;
                            data[1] = (byte)avgG;
                            data[2] = (byte)avgR;
                        }
                    }
                }
            }
            newImage.UnlockBits(blurredData);
            return newImage;
        }

        private Bitmap contrastStreching(Bitmap inputImage)
        {
            double blackPointPercent = 0.05;
            double whitePointPercent = 0.2;
            Bitmap newImage = new Bitmap(this.imageWidth, this.imageHeight);

            Rectangle rect = new Rectangle(0, 0, this.imageWidth, this.imageHeight);
            BitmapData srcData = inputImage.LockBits(rect, ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);


            Rectangle rect2 = new Rectangle(0, 0, newImage.Width, newImage.Height);
            BitmapData destData = newImage.LockBits(rect2, ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int stride = srcData.Stride;
            IntPtr srcScan0 = srcData.Scan0;
            IntPtr destScan0 = destData.Scan0;
            var freq = new int[256];

            unsafe
            {
                byte* src = (byte*)srcScan0;
                for (int y = 0; y < newImage.Height; ++y)
                {
                    for (int x = 0; x < this.imageWidth; ++x)
                    {
                        freq[src[y * stride + x * 4]]++;
                    }
                }
                int numPixels = this.imageWidth * this.imageHeight;
                int minI = 0;
                var blackPixels = numPixels * blackPointPercent;
                int accum = 0;
                while (minI < 255)
                {
                    accum += freq[minI];
                    if (accum > blackPixels) break;
                    minI++;
                }
                int maxI = 255;
                var whitePixels = numPixels * whitePointPercent;
                accum = 0;
                while (maxI > 0)
                {
                    accum += freq[maxI];
                    if (accum > whitePixels) break;
                    maxI--;
                }
                double spread = 255d / (maxI - minI);
                byte* dst = (byte*)destScan0;
                for (int y = 0; y < this.imageHeight; ++y)
                {
                    for (int x = 0; x < this.imageWidth; ++x)
                    {
                        int i = y * stride + x * 4;
                        double value = Math.Round((src[i] - minI) * spread);
                        byte val = (byte)(Math.Min(Math.Max(value, 0), 255));
                        dst[i] = val;
                        dst[i + 1] = val;
                        dst[i + 2] = val;
                        dst[i + 3] = 255;
                    }
                }
            }
            inputImage.UnlockBits(srcData);
            newImage.UnlockBits(destData);
            return newImage;
        }

        public void RunPythonScript(string fileName, string parameters)
        {

            Process p = new Process();
            p.StartInfo.FileName = @"C:\Users\Judit\AppData\Local\Programs\Python\Python311\python.exe";
            p.StartInfo.WorkingDirectory = $@"{Directory.GetCurrentDirectory()}";
            p.StartInfo.Arguments = $@"../../../{fileName} {parameters}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            p.WaitForExit();
        }
    }
}
