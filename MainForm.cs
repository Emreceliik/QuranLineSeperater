using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System;

// الكلاس الرئيسي لنافذة التطبيق الرئيسية، يوفر واجهة المستخدم لتحميل الصور، معالجة الصفحات، وعرض النتائج
// Main application window class, provides the user interface for loading images, processing pages, and displaying results
public class MainForm : Form
{
    private Button btnLoadImage;
    private Button btnSaveLines;
    private Button btnSettings;
    private FlowLayoutPanel flowPanel;
    private List<Bitmap> extractedLines;
    private string originalFileName;
    private ProcessingSettings settings;
    private Button btnBatchProcess;
    // المُنشئ: يقوم بإعداد النافذة الرئيسية وتهيئة الأزرار ولوحة العرض
    // Constructor: Sets up the main window and initializes buttons and display panel
    public MainForm()
    {
        Text = "Advanced Quran Line Separator";
        Width = 1200;
        Height = 800;

        settings = new ProcessingSettings();

        btnLoadImage = new Button
        {
            Text = "📂 Load Image",
            Dock = DockStyle.Top,
            Height = 40
        };
        btnLoadImage.Click += BtnLoadImage_Click;

        btnSaveLines = new Button
        {
            Text = "💾 Save Lines",
            Dock = DockStyle.Top,
            Height = 40,
            Enabled = false
        };
        btnSaveLines.Click += BtnSaveLines_Click;

        btnSettings = new Button
        {
            Text = "⚙️ Settings",
            Dock = DockStyle.Top,
            Height = 40
        };
        btnSettings.Click += BtnSettings_Click;

        flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        btnBatchProcess = new Button
        {
            Text = "📁 Batch Process Folder",
            Dock = DockStyle.Top,
            Height = 40
        };
        btnBatchProcess.Click += BtnBatchProcess_Click;

        Controls.Add(btnBatchProcess);

        Controls.Add(flowPanel);
        Controls.Add(btnSaveLines);
        Controls.Add(btnSettings);
        Controls.Add(btnLoadImage);

        extractedLines = new List<Bitmap>();
    }

    // يفتح نافذة الإعدادات لتخصيص معالجة الصور
    // Opens the settings window to customize image processing
    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using SettingsForm settingsForm = new SettingsForm(settings);
        if (settingsForm.ShowDialog() == DialogResult.OK)
        {
            settings = settingsForm.GetSettings();
        }
    }
    private async void BtnBatchProcess_Click(object sender, EventArgs e)
    {
        using (var ofd = new FolderBrowserDialog { Description = "Select folder with Quran page images" })
        using (var fbd = new FolderBrowserDialog { Description = "Select output folder for extracted lines" })
        {
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (fbd.ShowDialog() != DialogResult.OK) return;

            string inputFolder = ofd.SelectedPath;
            string outputFolder = fbd.SelectedPath;
            string[] extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff" };

            btnBatchProcess.Enabled = btnLoadImage.Enabled = btnSaveLines.Enabled = btnSettings.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                int totalPages = 0, totalLines = 0;
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(inputFolder))
                    {
                        if (!extensions.Contains(Path.GetExtension(file).ToLower())) continue;
                        totalPages++;
                        string baseName = Path.GetFileNameWithoutExtension(file);
                        var src = new Bitmap(file);

                        // Orijinal renkleri korumak için
                        settings.PreserveOriginalColors = true;
                        settings.SaveTransparent = true;

                        // Satırları ayıkla
                        var lines = QuranLineProcessor.ExtractLines(src, settings);
                        src.Dispose();

                        // Her bir sayfayı kendi alt klasörüne kaydet
                        var pageOutDir = Path.Combine(outputFolder, baseName);
                        Directory.CreateDirectory(pageOutDir);

                        for (int i = 0; i < lines.Count; i++)
                        {
                            string outPath = Path.Combine(pageOutDir, $"{baseName}_line_{i + 1:D2}.png");
                            SaveWithTransparency(lines[i], outPath);
                            lines[i].Dispose();
                            totalLines++;
                        }
                    }
                });

                MessageBox.Show(
                    $"Processed {totalPages} pages.\nExtracted {totalLines} lines in total.\nOutput: {outputFolder}",
                    "Batch Processing Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch processing error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBatchProcess.Enabled = btnLoadImage.Enabled = btnSaveLines.Enabled = btnSettings.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }

    // يقوم بتحميل صورة ومعالجتها لاستخراج الأسطر بشكل غير متزامن
    // Loads an image and processes it to extract lines asynchronously
    private async void BtnLoadImage_Click(object sender, EventArgs e)
    {
        using OpenFileDialog ofd = new()
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff",
            Title = "Select Quran Page"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                btnLoadImage.Enabled = false;
                btnSaveLines.Enabled = false;
                btnSettings.Enabled = false;

                originalFileName = Path.GetFileNameWithoutExtension(ofd.FileName);

                // Clear previous images and free memory
                foreach (var oldLine in extractedLines)
                {
                    oldLine.Dispose();
                }
                extractedLines.Clear();
                flowPanel.Controls.Clear();

                // Process in background
                await Task.Run(() => {
                    using (Bitmap image = new(ofd.FileName))
                    {
                        // Prepare image for processing
                        Bitmap processedImage = QuranImagePreprocessor.PreprocessImage(image, settings);

                        // Extract lines
                        extractedLines = QuranLineProcessor.ExtractLines(processedImage, settings);
                    }
                });

                // Show lines in UI thread
                ShowExtractedLines();

                btnSaveLines.Enabled = extractedLines.Count > 0;
                MessageBox.Show($"Found {extractedLines.Count} lines.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnLoadImage.Enabled = true;
                btnSaveLines.Enabled = extractedLines.Count > 0;
                btnSettings.Enabled = true;
            }
        }
    }

    // يعرض الأسطر المستخرجة في لوحة العرض
    // Displays the extracted lines in the display panel
    private void ShowExtractedLines()
    {
        for (int i = 0; i < extractedLines.Count; i++)
        {
            Bitmap line = extractedLines[i];
            Panel linePanel = new Panel
            {
                Width = flowPanel.ClientSize.Width - 30,
                Height = line.Height + 60,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblIndex = new Label
            {
                Text = $"Line {i + 1}",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20
            };

            PictureBox pb = new PictureBox
            {
                Image = line,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Button btnRemove = new Button
            {
                Text = "❌ Remove This Line",
                Dock = DockStyle.Bottom,
                Height = 30,
                Tag = i
            };
            btnRemove.Click += (sender, e) => {
                if (sender is Button btn && btn.Tag is int index)
                {
                    RemoveLine(index);
                }
            };

            linePanel.Controls.Add(pb);
            linePanel.Controls.Add(lblIndex);
            linePanel.Controls.Add(btnRemove);
            flowPanel.Controls.Add(linePanel);
        }
    }

    // يزيل سطرًا محددًا من القائمة ويعيد بناء لوحة العرض
    // Removes a specific line from the list and rebuilds the display panel
    private void RemoveLine(int index)
    {
        if (index >= 0 && index < extractedLines.Count)
        {
            extractedLines[index].Dispose();
            extractedLines.RemoveAt(index);

            // Rebuild panel
            flowPanel.Controls.Clear();
            ShowExtractedLines();

            btnSaveLines.Enabled = extractedLines.Count > 0;
        }
    }

    // يحفظ الأسطر المستخرجة في مجلد محدد
    // Saves the extracted lines to a specified folder
    private void BtnSaveLines_Click(object sender, EventArgs e)
    {
        if (extractedLines.Count == 0)
        {
            MessageBox.Show("No lines to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using FolderBrowserDialog fbd = new FolderBrowserDialog
        {
            Description = "Select the folder to save lines",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (fbd.ShowDialog() == DialogResult.OK)
        {
            string outputFolder = fbd.SelectedPath;

            try
            {
                Cursor = Cursors.WaitCursor;
                btnSaveLines.Enabled = false;

                for (int i = 0; i < extractedLines.Count; i++)
                {
                    string fileName = $"{originalFileName}_line_{(i + 1):D2}.png";
                    string filePath = Path.Combine(outputFolder, fileName);

                    // Save with transparent background option
                    if (settings.SaveTransparent)
                    {
                        SaveWithTransparency(extractedLines[i], filePath);
                    }
                    else
                    {
                        extractedLines[i].Save(filePath, ImageFormat.Png);
                    }
                }

                MessageBox.Show($"{extractedLines.Count} lines saved successfully.\nLocation: {outputFolder}",
                                "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving lines: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnSaveLines.Enabled = true;
            }
        }
    }

    // يحفظ الصورة مع خلفية شفافة بناءً على إعدادات الحبر
    // Saves the image with a transparent background based on ink settings
    private void SaveWithTransparency(Bitmap source, string filePath)
    {
        Bitmap transparent = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(transparent))
        {
            g.Clear(Color.Transparent);
        }

        // Check each pixel
        for (int x = 0; x < source.Width; x++)
        {
            for (int y = 0; y < source.Height; y++)
            {
                Color pixelColor = source.GetPixel(x, y);

                // Copy non-background (ink) pixels to the new image
                if (QuranImageAnalyzer.IsInk(pixelColor, settings))
                {
                    transparent.SetPixel(x, y, pixelColor);
                }
            }
        }

        transparent.Save(filePath, ImageFormat.Png);
        transparent.Dispose();
    }

    // ينظف الموارد عند إغلاق النافذة
    // Cleans up resources when the window is closed
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        foreach (var line in extractedLines)
        {
            line.Dispose();
        }

        base.OnFormClosing(e);
    }
}