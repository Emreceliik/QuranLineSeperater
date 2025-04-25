using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class QuranImagePreprocessor
{
    public static Bitmap PreprocessImage(Bitmap source, ProcessingSettings settings)
    {
        Bitmap processedImage = new Bitmap(source.Width, source.Height);

        // Otomatik eşik değeri hesaplama
        int threshold = settings.InkThreshold;
        if (settings.UseAutomaticThreshold)
        {
            threshold = CalculateOtsuThreshold(source);
        }

        using (Graphics g = Graphics.FromImage(processedImage))
        {
            g.Clear(Color.White);
        }

        for (int x = 0; x < source.Width; x++)
        {
            for (int y = 0; y < source.Height; y++)
            {
                Color pixelColor = source.GetPixel(x, y);
                if (QuranImageAnalyzer.IsInk(pixelColor, settings))
                {
                    processedImage.SetPixel(x, y, Color.Black);
                }
                else
                {
                    processedImage.SetPixel(x, y, Color.White);
                }
            }
        }

        if (settings.RemoveNoise)
        {
            processedImage = RemoveNoise(processedImage, settings.NoiseReductionLevel);
        }

        return processedImage;
    }

    private static int CalculateOtsuThreshold(Bitmap image)
    {
        int[] histogram = new int[256];
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                Color pixelColor = image.GetPixel(x, y);
                int grayValue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                histogram[grayValue]++;
            }
        }

        int totalPixels = image.Width * image.Height;
        double sum = 0;
        for (int i = 0; i < 256; i++)
            sum += i * histogram[i];

        double sumB = 0;
        int wB = 0;
        int wF = 0;
        double maxVariance = 0;
        int threshold = 0;

        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0) continue;

            wF = totalPixels - wB;
            if (wF == 0) break;

            sumB += t * histogram[t];

            double mB = sumB / wB;
            double mF = (sum - sumB) / wF;

            double variance = wB * wF * Math.Pow(mB - mF, 2);

            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = t;
            }
        }

        return threshold;
    }

    private static Bitmap RemoveNoise(Bitmap image, int level)
    {
        Bitmap result = new Bitmap(image.Width, image.Height);
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                List<bool> neighbors = new List<bool>();
                for (int dx = -level; dx <= level; dx++)
                {
                    for (int dy = -level; dy <= level; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < image.Width && ny >= 0 && ny < image.Height)
                        {
                            Color neighborColor = image.GetPixel(nx, ny);
                            neighbors.Add(neighborColor.R < 128);
                        }
                    }
                }
                bool isBlack = neighbors.Count(n => n) > neighbors.Count / 2;
                result.SetPixel(x, y, isBlack ? Color.Black : Color.White);
            }
        }
        return result;
    }
}



