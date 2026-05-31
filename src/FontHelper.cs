using System.Drawing.Text;

namespace DriveRecorderConverter
{
    internal static class FontHelper
    {
        private static readonly PrivateFontCollection _privateFonts = new();

        public static Font LoadFont(string fontFileName, float size, FontStyle style = FontStyle.Regular)
        {
            string fontPath = Path.Combine(AppContext.BaseDirectory, "fonts", "Eixample_Villa_Light.otf");

            if (File.Exists(fontPath))
            {
                _privateFonts.AddFontFile(fontPath);
                var family = _privateFonts.Families[^1];
                return new Font(family, size, style, GraphicsUnit.Point);
            }

            // Fallback: try system-installed font
            try
            {
                return new Font("Eixample Villa Light", size, style, GraphicsUnit.Point);
            }
            catch
            {
                return new Font("Segoe UI", size, style, GraphicsUnit.Point);
            }
        }
    }
}
