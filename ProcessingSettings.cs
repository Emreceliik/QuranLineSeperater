using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

public class ProcessingSettings
{
    public int InkThreshold { get; set; } = 200;
    public bool UseAutomaticThreshold { get; set; } = true;
    public int MinLineHeight { get; set; } = 15;
    public int MaxEmptyLines { get; set; } = 3;
    public bool RemoveNoise { get; set; } = false;
    public int NoiseReductionLevel { get; set; } = 3;
    public bool SaveTransparent { get; set; } = false;

    // Renk koruması için yeni ayar
    public bool PreserveOriginalColors { get; set; } = false;

    // Renk algılama için ince ayarlar
    public float MinSaturation { get; set; } = 0.2f;
    public float MaxBrightness { get; set; } = 0.8f;

    // Hedef resim boyutu
    public int TargetWidth { get; set; } = 0;
    public int TargetHeight { get; set; } = 0;

    public List<(float MinHue, float MaxHue)> InkColorRanges { get; set; } = new List<(float, float)>
    {
        (0, 10),   // Kırmızı
        (170, 180),// Kırmızı
        (80, 100), // Yeşil
        (220, 240) // Mavi
    };

    public ProcessingSettings Clone()
    {
        return new ProcessingSettings
        {
            InkThreshold = this.InkThreshold,
            UseAutomaticThreshold = this.UseAutomaticThreshold,
            MinLineHeight = this.MinLineHeight,
            MaxEmptyLines = this.MaxEmptyLines,
            RemoveNoise = this.RemoveNoise,
            NoiseReductionLevel = this.NoiseReductionLevel,
            SaveTransparent = this.SaveTransparent,
            PreserveOriginalColors = this.PreserveOriginalColors,
            MinSaturation = this.MinSaturation,
            MaxBrightness = this.MaxBrightness,
            TargetWidth = this.TargetWidth,
            TargetHeight = this.TargetHeight,
            InkColorRanges = new List<(float, float)>(this.InkColorRanges)
        };
    }
}