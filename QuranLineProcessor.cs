using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public static class QuranLineProcessor
{
    public static List<Bitmap> ExtractLines(Bitmap source, ProcessingSettings settings)
    {
        List<Bitmap> lines = new List<Bitmap>();
        int width = source.Width;
        int height = source.Height;

        // Mürekkep haritasýný oluþtur
        bool[,] inkMap = CreateInkMap(source, settings);

        // Baðlý bileþenleri tespit et
        List<Rectangle> components = FindConnectedComponents(inkMap, width, height);

        // Bileþenleri satýrlara grupla
        List<List<Rectangle>> lineGroups = GroupComponentsIntoLines(components, settings);

        // Her satýr için görüntü çýkar
        foreach (var lineGroup in lineGroups)
        {
            if (lineGroup.Count == 0) continue;

            int minY = lineGroup.Min(c => c.Top);
            int maxY = lineGroup.Max(c => c.Bottom);
            int lineHeight = maxY - minY + 1;

            if (lineHeight < settings.MinLineHeight) continue;

            // Geçerli sýnýrlarý kontrol et
            minY = Math.Max(0, minY);
            maxY = Math.Min(height - 1, maxY);

            if (minY >= maxY) continue; // Geçersiz satýr

            Bitmap lineImage = ExtractLineImage(source, minY, maxY, width, settings);
            lines.Add(lineImage);
        }

        return lines;
    }

    private static bool[,] CreateInkMap(Bitmap source, ProcessingSettings settings)
    {
        int width = source.Width;
        int height = source.Height;
        bool[,] inkMap = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = source.GetPixel(x, y);
                inkMap[x, y] = QuranImageAnalyzer.IsInk(pixelColor, settings);
            }
        }

        return inkMap;
    }

    private static List<Rectangle> FindConnectedComponents(bool[,] inkMap, int width, int height)
    {
        List<Rectangle> components = new List<Rectangle>();
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inkMap[x, y] && !visited[x, y])
                {
                    Rectangle component = FloodFill(inkMap, visited, x, y, width, height);
                    components.Add(component);
                }
            }
        }

        return components;
    }

    private static Rectangle FloodFill(bool[,] inkMap, bool[,] visited, int startX, int startY, int width, int height)
    {
        int minX = startX, maxX = startX, minY = startY, maxY = startY;
        Stack<Point> stack = new Stack<Point>();
        stack.Push(new Point(startX, startY));

        while (stack.Count > 0)
        {
            Point p = stack.Pop();
            if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height || visited[p.X, p.Y] || !inkMap[p.X, p.Y])
                continue;

            visited[p.X, p.Y] = true;
            minX = Math.Min(minX, p.X);
            maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y);
            maxY = Math.Max(maxY, p.Y);

            stack.Push(new Point(p.X - 1, p.Y));
            stack.Push(new Point(p.X + 1, p.Y));
            stack.Push(new Point(p.X, p.Y - 1));
            stack.Push(new Point(p.X, p.Y + 1));
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static List<List<Rectangle>> GroupComponentsIntoLines(List<Rectangle> components, ProcessingSettings settings)
    {
        components.Sort((a, b) => a.Top.CompareTo(b.Top));
        List<List<Rectangle>> lines = new List<List<Rectangle>>();
        List<Rectangle> currentLine = new List<Rectangle>();

        foreach (var component in components)
        {
            if (currentLine.Count == 0)
            {
                currentLine.Add(component);
                continue;
            }

            int lastBottom = currentLine.Max(c => c.Bottom);
            if (component.Top - lastBottom <= settings.MaxEmptyLines)
            {
                currentLine.Add(component);
            }
            else
            {
                lines.Add(currentLine);
                currentLine = new List<Rectangle> { component };
            }
        }

        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    private static Bitmap ExtractLineImage(Bitmap source, int startY, int endY, int width, ProcessingSettings settings)
    {
        try
        {
            // Geçerli sýnýrlarý kontrol et
            startY = Math.Max(0, startY);
            endY = Math.Min(source.Height - 1, endY);

            if (startY >= endY || startY < 0 || endY >= source.Height)
            {
                // Geçersiz sýnýrlar durumunda 1x1 boþ bir bitmap döndür
                return new Bitmap(1, 1);
            }

            int lineHeight = endY - startY + 1;

            // Satýrýn yatay sýnýrlarýný tespit et (boþluklarý kýrp)
            int leftBorder = width - 1;
            int rightBorder = 0;

            for (int y = startY; y <= endY; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixelColor = source.GetPixel(x, y);
                    if (QuranImageAnalyzer.IsInk(pixelColor, settings))
                    {
                        leftBorder = Math.Min(leftBorder, x);
                        rightBorder = Math.Max(rightBorder, x);
                    }
                }
            }

            int margin = 5; // Kenar boþluðu piksel sayýsý
            leftBorder = Math.Max(0, leftBorder - margin);
            rightBorder = Math.Min(width - 1, rightBorder + margin);

            if (leftBorder >= rightBorder)
            {
                leftBorder = 0;
                rightBorder = width - 1;
            }

            int finalWidth = rightBorder - leftBorder + 1;

            // Satýr görüntüsünü oluþtur
            Bitmap lineImage = new Bitmap(finalWidth, lineHeight);
            using (Graphics g = Graphics.FromImage(lineImage))
            {
                if (!settings.SaveTransparent)
                {
                    g.Clear(Color.White); // Arka planý beyaz yap
                }
                else
                {
                    // Þeffaf arka plan için ayarla
                    lineImage = new Bitmap(finalWidth, lineHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    g.Clear(Color.Transparent);
                }
            }

            // Piksel bazýnda iþlem
            for (int y = startY; y <= endY; y++)
            {
                for (int x = leftBorder; x <= rightBorder; x++)
                {
                    Color pixelColor = source.GetPixel(x, y);
                    // Yeni ProcessInkColor metodu ile renkler korunur
                    Color processedColor = QuranImageAnalyzer.ProcessInkColor(pixelColor, settings);

                    // Þeffaf olmayan pikselleri aktar
                    if (processedColor.A > 0)
                    {
                        lineImage.SetPixel(x - leftBorder, y - startY, processedColor);
                    }
                }
            }

            return lineImage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Satýr iþlenirken hata: {ex.Message}");
            // Hata durumunda boþ bir bitmap döndür
            return new Bitmap(1, 1);
        }
    }
}