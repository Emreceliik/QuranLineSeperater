using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

// كلاس نافذة الإعدادات، يسمح للمستخدم بتخصيص معايير معالجة الصور
// Settings window class, allows the user to customize image processing parameters
public partial class SettingsForm : Form
{
    private TrackBar trkThreshold;
    private Label lblThreshold;
    private NumericUpDown nudMinLineHeight;
    private NumericUpDown nudMaxEmptySpace;
    private CheckBox chkSaveTransparent;
    private CheckBox chkAutomaticThreshold;
    private NumericUpDown nudNoiseReduction;
    private CheckBox chkRemoveNoise;
    private Button btnOK;
    private Button btnCancel;
    private ProcessingSettings settings;
    private List<NumericUpDown> nudMinHues;
    private List<NumericUpDown> nudMaxHues;

    // المُنشئ: يقوم بإعداد نافذة الإعدادات وتهيئة عناصر التحكم
    // Constructor: Sets up the settings window and initializes controls
    public SettingsForm(ProcessingSettings currentSettings)
    {
        settings = currentSettings.Clone();

        Text = "Image Processing Settings";
        Width = 400;
        Height = 600;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 11
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Automatic threshold
        chkAutomaticThreshold = new CheckBox
        {
            Text = "Automatic Threshold",
            Checked = settings.UseAutomaticThreshold,
            AutoSize = true
        };
        chkAutomaticThreshold.CheckedChanged += (s, e) => {
            trkThreshold.Enabled = !chkAutomaticThreshold.Checked;
            lblThreshold.Enabled = !chkAutomaticThreshold.Checked;
            settings.UseAutomaticThreshold = chkAutomaticThreshold.Checked;
        };

        // Ink threshold
        Label lblThresholdTitle = new Label { Text = "Ink Threshold:", AutoSize = true };
        trkThreshold = new TrackBar
        {
            Minimum = 0,
            Maximum = 255,
            Value = settings.InkThreshold,
            TickFrequency = 15,
            Enabled = !settings.UseAutomaticThreshold
        };
        trkThreshold.ValueChanged += (s, e) => {
            lblThreshold.Text = trkThreshold.Value.ToString();
            settings.InkThreshold = trkThreshold.Value;
        };

        lblThreshold = new Label
        {
            Text = settings.InkThreshold.ToString(),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = !settings.UseAutomaticThreshold
        };

        // Minimum line height
        Label lblMinLineHeight = new Label { Text = "Minimum Line Height:", AutoSize = true };
        nudMinLineHeight = new NumericUpDown
        {
            Minimum = 5,
            Maximum = 100,
            Value = settings.MinLineHeight
        };
        nudMinLineHeight.ValueChanged += (s, e) => {
            settings.MinLineHeight = (int)nudMinLineHeight.Value;
        };

        // Maximum empty space
        Label lblMaxEmptySpace = new Label { Text = "Maximum Line Spacing:", AutoSize = true };
        nudMaxEmptySpace = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 20,
            Value = settings.MaxEmptyLines
        };
        nudMaxEmptySpace.ValueChanged += (s, e) => {
            settings.MaxEmptyLines = (int)nudMaxEmptySpace.Value;
        };

        // Noise removal
        chkRemoveNoise = new CheckBox
        {
            Text = "Remove Noise",
            Checked = settings.RemoveNoise,
            AutoSize = true
        };
        chkRemoveNoise.CheckedChanged += (s, e) => {
            nudNoiseReduction.Enabled = chkRemoveNoise.Checked;
            settings.RemoveNoise = chkRemoveNoise.Checked;
        };

        Label lblNoiseReduction = new Label { Text = "Noise Reduction Level:", AutoSize = true };
        nudNoiseReduction = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10,
            Value = settings.NoiseReductionLevel,
            Enabled = settings.RemoveNoise
        };
        nudNoiseReduction.ValueChanged += (s, e) => {
            settings.NoiseReductionLevel = (int)nudNoiseReduction.Value;
        };

        // Transparent save option
        chkSaveTransparent = new CheckBox
        {
            Text = "Save Lines with Transparent Background",
            Checked = settings.SaveTransparent,
            AutoSize = true
        };
        chkSaveTransparent.CheckedChanged += (s, e) => {
            settings.SaveTransparent = chkSaveTransparent.Checked;
        };

        // Color range controls
        Label lblColorRanges = new Label { Text = "Ink Color Ranges (Hue):", AutoSize = true };
        nudMinHues = new List<NumericUpDown>();
        nudMaxHues = new List<NumericUpDown>();

        Panel colorPanel = new Panel { Dock = DockStyle.Fill, Height = 100 };
        TableLayoutPanel colorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = settings.InkColorRanges.Count + 1
        };

        colorLayout.Controls.Add(new Label { Text = "Min Hue", AutoSize = true }, 0, 0);
        colorLayout.Controls.Add(new Label { Text = "Max Hue", AutoSize = true }, 1, 0);

        for (int i = 0; i < settings.InkColorRanges.Count; i++)
        {
            var (minHue, maxHue) = settings.InkColorRanges[i];
            NumericUpDown nudMinHue = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 360,
                Value = (decimal)minHue
            };
            NumericUpDown nudMaxHue = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 360,
                Value = (decimal)maxHue
            };

            int index = i;
            nudMinHue.ValueChanged += (s, e) => UpdateColorRange(index);
            nudMaxHue.ValueChanged += (s, e) => UpdateColorRange(index);

            nudMinHues.Add(nudMinHue);
            nudMaxHues.Add(nudMaxHue);

            colorLayout.Controls.Add(nudMinHue, 0, i + 1);
            colorLayout.Controls.Add(nudMaxHue, 1, i + 1);
        }
        colorPanel.Controls.Add(colorLayout);

        // Buttons
        btnOK = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Fill
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Dock = DockStyle.Fill
        };

        // Build layout
        layout.Controls.Add(chkAutomaticThreshold, 0, 0);
        layout.SetColumnSpan(chkAutomaticThreshold, 2);

        layout.Controls.Add(lblThresholdTitle, 0, 1);

        Panel thresholdPanel = new Panel { Dock = DockStyle.Fill };
        thresholdPanel.Controls.Add(lblThreshold);
        thresholdPanel.Controls.Add(trkThreshold);
        lblThreshold.Location = new Point(trkThreshold.Width - 30, 0);
        trkThreshold.Dock = DockStyle.Left;
        trkThreshold.Width = thresholdPanel.Width - 40;

        layout.Controls.Add(thresholdPanel, 1, 1);

        layout.Controls.Add(lblMinLineHeight, 0, 2);
        layout.Controls.Add(nudMinLineHeight, 1, 2);

        layout.Controls.Add(lblMaxEmptySpace, 0, 3);
        layout.Controls.Add(nudMaxEmptySpace, 1, 3);

        layout.Controls.Add(chkRemoveNoise, 0, 4);
        layout.SetColumnSpan(chkRemoveNoise, 2);

        layout.Controls.Add(lblNoiseReduction, 0, 5);
        layout.Controls.Add(nudNoiseReduction, 1, 5);

        layout.Controls.Add(chkSaveTransparent, 0, 6);
        layout.SetColumnSpan(chkSaveTransparent, 2);


        layout.Controls.Add(lblColorRanges, 0, 7);
        layout.Controls.Add(colorPanel, 0, 8);
        layout.SetColumnSpan(colorPanel, 2);

        TableLayoutPanel buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            ColumnCount = 2
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttonPanel.Controls.Add(btnOK, 0, 0);
        buttonPanel.Controls.Add(btnCancel, 1, 0);

        Controls.Add(layout);
        Controls.Add(buttonPanel);

        AcceptButton = btnOK;
        CancelButton = btnCancel;
    }

    // يحدّث نطاق الألوان بناءً على إدخال المستخدم
    // Updates the color range based on user input
    private void UpdateColorRange(int index)
    {
        settings.InkColorRanges[index] = ((float)nudMinHues[index].Value, (float)nudMaxHues[index].Value);
    }

    // يعيد إعدادات المعالجة المحدثة
    // Returns the updated processing settings
    public ProcessingSettings GetSettings()
    {
        return settings;
    }
}