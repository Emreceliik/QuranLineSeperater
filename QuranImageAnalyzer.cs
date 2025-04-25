using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class QuranImageAnalyzer
{
    // Mevcut metod - sadece mürekkep olup olmadığını belirler
    public static bool IsInk(Color color, ProcessingSettings settings)
    {
        // Gri tonlama ile siyah kontrolü
        int grayValue = (color.R + color.G + color.B) / 3;
        if (grayValue < settings.InkThreshold)
        {
            return true;
        }
        
        // Renkli mürekkep kontrolü (HSV)
        float hue = color.GetHue();
        float saturation = color.GetSaturation();
        float brightness = color.GetBrightness();
        
        // Belirli bir doygunluk ve parlaklık seviyesinin üzerinde olmalı
        if (saturation > settings.MinSaturation && brightness < settings.MaxBrightness)
        {
            foreach (var (minHue, maxHue) in settings.InkColorRanges)
            {
                if ((minHue <= maxHue && hue >= minHue && hue <= maxHue) ||
                    (minHue > maxHue && (hue >= minHue || hue <= maxHue)))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // İlave metod - mürekkep piksel rengini işleyerek döndürür
    public static Color ProcessInkColor(Color originalColor, ProcessingSettings settings)
    {
        // Eğer piksel mürekkepse
        if (IsInk(originalColor, settings))
        {
            if (settings.PreserveOriginalColors)
            {
                // Orijinal rengi koru
                return originalColor;
            }
            else
            {
                // İstenirse siyaha dönüştür (eski davranış)
                return Color.Black;
            }
        }
        
        // Mürekkep olmayan piksel - arka plan rengi
        if (settings.SaveTransparent)
        {
            return Color.Transparent;
        }
        else
        {
            return Color.White;
        }
    }
    
    // Yardımcı metod - renk analizi yaparak mürekkebin türünü belirler
    public static InkType GetInkType(Color color, ProcessingSettings settings)
    {
        int grayValue = (color.R + color.G + color.B) / 3;
        float hue = color.GetHue();
        float saturation = color.GetSaturation();
        float brightness = color.GetBrightness();
        
        // Siyah mürekkep kontrolü
        if (grayValue < settings.InkThreshold)
        {
            return InkType.Black;
        }
        
        // Renkli mürekkep kontrolü
        if (saturation > settings.MinSaturation && brightness < settings.MaxBrightness)
        {
            // Renk aralıklarını kontrol et ve renk türünü belirle
            if ((hue >= 0 && hue <= 30) || (hue >= 330 && hue <= 360))
                return InkType.Red;
            else if (hue >= 31 && hue <= 90)
                return InkType.Yellow;
            else if (hue >= 91 && hue <= 150)
                return InkType.Green;
            else if (hue >= 151 && hue <= 270)
                return InkType.Blue;
            else if (hue >= 271 && hue <= 329)
                return InkType.Purple;
        }
        
        return InkType.None;
    }
}

// Mürekkep türünü belirten enum
public enum InkType
{
    None,
    Black,
    Red,
    Green,
    Blue,
    Yellow,
    Purple
}