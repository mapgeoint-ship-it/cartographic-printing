using Microsoft.Win32;
using System.Globalization;
using System.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CartographicPrinting;

public partial class MainWindow : Window
{
    private BitmapSource? _bitmap;
    private string? _filePath;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Відкрити картографічний файл",
            Filter = "Зображення|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|PNG|*.png|JPEG|*.jpg;*.jpeg|TIFF|*.tif;*.tiff|BMP|*.bmp"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            using var stream = File.OpenRead(dialog.FileName);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            _bitmap = decoder.Frames[0];
            if (_bitmap.CanFreeze) _bitmap.Freeze();

            _filePath = dialog.FileName;
            MapImage.Source = _bitmap;
            EmptyHint.Visibility = Visibility.Collapsed;
            FilePathBox.Text = _filePath;

            var dpiX = _bitmap.DpiX > 1 ? _bitmap.DpiX : 96;
            var dpiY = _bitmap.DpiY > 1 ? _bitmap.DpiY : 96;
            StatusText.Text = $"{Path.GetFileName(_filePath)}  |  {_bitmap.PixelWidth} × {_bitmap.PixelHeight} px  |  {dpiX:0} × {dpiY:0} DPI";
            UpdatePrintInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося відкрити файл.\n\n{ex.Message}", "Картографічний друк",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PaperPresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        var name = (PaperPresetBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        switch (name)
        {
            case "A4": SetPaper(210, 297); break;
            case "A3": SetPaper(297, 420); break;
            case "A2": SetPaper(420, 594); break;
            case "A1": SetPaper(594, 841); break;
            case "A0": SetPaper(841, 1189); break;
            case "Рулон 610 мм": SetPaper(610, 900); break;
        }
        UpdatePrintInfo();
    }

    private void ScaleModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        var exact = ScaleModeBox.SelectedIndex == 1;
        ExactScalePanel.IsEnabled = exact;
        UpdatePrintInfo();
    }

    private void SetPaper(double width, double height)
    {
        WidthMmBox.Text = width.ToString(CultureInfo.InvariantCulture);
        HeightMmBox.Text = height.ToString(CultureInfo.InvariantCulture);
    }

    private static double MmToDip(double mm) => mm * 96.0 / 25.4;

    private static double ParsePositive(string? text, double fallback)
    {
        if (double.TryParse(text?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0)
            return value;
        return fallback;
    }

    private double GetTargetScale()
    {
        var raw = TargetScaleBox.Text;
        if (string.IsNullOrWhiteSpace(raw) && TargetScaleBox.SelectedItem is ComboBoxItem item)
            raw = item.Content?.ToString();
        return ParsePositive(raw, 50000);
    }

    private (double widthMm, double heightMm) GetOrientedPaper()
    {
        var w = ParsePositive(WidthMmBox.Text, 210);
        var h = ParsePositive(HeightMmBox.Text, 297);
        var landscape = OrientationBox.SelectedIndex == 1;
        if (landscape && h > w) (w, h) = (h, w);
        if (!landscape && w > h) (w, h) = (h, w);
        return (w, h);
    }

    private void UpdatePrintInfo()
    {
        if (_bitmap == null) return;

        var dpiX = _bitmap.DpiX > 1 ? _bitmap.DpiX : 96;
        var dpiY = _bitmap.DpiY > 1 ? _bitmap.DpiY : 96;
        var physicalWmm = _bitmap.PixelWidth / dpiX * 25.4;
        var physicalHmm = _bitmap.PixelHeight / dpiY * 25.4;

        if (ScaleModeBox.SelectedIndex == 1)
        {
            var source = ParsePositive(SourceScaleBox.Text, 50000);
            var target = GetTargetScale();
            var factor = source / target;
            physicalWmm *= factor;
            physicalHmm *= factor;
            PrintInfoText.Text = $"Точний масштаб 1:{target:0}. Розмір карти на папері приблизно {physicalWmm:0.0} × {physicalHmm:0.0} мм.";
        }
        else
        {
            PrintInfoText.Text = "Режим «Вмістити в аркуш»: карта буде пропорційно зменшена або збільшена до доступної області.";
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_bitmap == null)
        {
            MessageBox.Show("Спочатку відкрийте картографічний файл.", "Картографічний друк",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true) return;

        try
        {
            var (paperWmm, paperHmm) = GetOrientedPaper();
            var pageW = MmToDip(paperWmm);
            var pageH = MmToDip(paperHmm);

            var ticket = printDialog.PrintTicket ?? new PrintTicket();
            ticket.PageOrientation = PageOrientation.Portrait;
            ticket.PageMediaSize = new PageMediaSize(pageW, pageH);
            printDialog.PrintTicket = ticket;

            var left = MmToDip(ParsePositive(LeftMarginBox.Text, 10));
            var right = MmToDip(ParsePositive(RightMarginBox.Text, 10));
            var top = MmToDip(ParsePositive(TopMarginBox.Text, 10));
            var bottom = MmToDip(ParsePositive(BottomMarginBox.Text, 10));

            var availableW = Math.Max(1, pageW - left - right);
            var availableH = Math.Max(1, pageH - top - bottom);

            var dpiX = _bitmap.DpiX > 1 ? _bitmap.DpiX : 96;
            var dpiY = _bitmap.DpiY > 1 ? _bitmap.DpiY : 96;
            var naturalW = _bitmap.PixelWidth / dpiX * 96.0;
            var naturalH = _bitmap.PixelHeight / dpiY * 96.0;

            double scale;
            if (ScaleModeBox.SelectedIndex == 1)
            {
                var source = ParsePositive(SourceScaleBox.Text, 50000);
                var target = GetTargetScale();
                scale = source / target;
            }
            else
            {
                scale = Math.Min(availableW / naturalW, availableH / naturalH);
            }

            var drawW = naturalW * scale;
            var drawH = naturalH * scale;

            if (ScaleModeBox.SelectedIndex == 1 && (drawW > availableW || drawH > availableH))
            {
                var result = MessageBox.Show(
                    "У вибраному точному масштабі карта більша за доступну область аркуша. Продовжити друк із обрізанням?",
                    "Точний масштаб", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            var x = left + Math.Max(0, (availableW - drawW) / 2.0);
            var y = top + Math.Max(0, (availableH - drawH) / 2.0);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageW, pageH));
                dc.DrawImage(_bitmap, new Rect(x, y, drawW, drawH));
            }

            printDialog.PrintVisual(visual, $"Картографічний друк — {Path.GetFileName(_filePath)}");
            StatusText.Text = "Завдання передано до принтера.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка друку.\n\n{ex.Message}", "Картографічний друк",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
