using System.Drawing;

namespace QueensProblem.Service.ImageProcessing
{
    /// <summary>
    /// Handles color analysis and classification
    /// </summary>
    public class ColorAnalyzer
    {
        private readonly double _similarityThreshold;
        private readonly List<Tuple<Color, string>> _representativeColors;

        public ColorAnalyzer(double similarityThreshold = 30.0)
        {
            _similarityThreshold = similarityThreshold;
            _representativeColors = new List<Tuple<Color, string>>();
        }

        public string GetColorLabel(Color avgColor)
        {
            foreach (var rep in _representativeColors)
            {
                if (ColorDistance(rep.Item1, avgColor) < _similarityThreshold)
                    return rep.Item2;
            }
            string newLabel = "color" + (_representativeColors.Count + 1);
            _representativeColors.Add(new Tuple<Color, string>(avgColor, newLabel));
            return newLabel;
        }

        public double ColorDistance(Color c1, Color c2)
        {
            return Math.Sqrt(Math.Pow(c1.R - c2.R, 2) +
                             Math.Pow(c1.G - c2.G, 2) +
                             Math.Pow(c1.B - c2.B, 2));
        }

        public Color GetAverageColor(Bitmap bmp, int startX, int startY, int width, int height)
        {
            int r = 0, g = 0, b = 0, count = 0;
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    count++;
                }
            }
            return Color.FromArgb(r / count, g / count, b / count);
        }
    }
}
