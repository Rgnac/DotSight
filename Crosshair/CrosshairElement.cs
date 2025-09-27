// Add this class to a new file: CustomCrosshair.cs
using System.Collections.Generic;
using System.Windows.Media;

namespace Crosshair
{
    public class CrosshairElement
    {
        public string ElementType { get; set; } // "Line", "Circle", "Rectangle"
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; } // For lines
        public double Y2 { get; set; } // For lines
        public double Width { get; set; } // For circles/rectangles
        public double Height { get; set; } // For circles/rectangles
        public double Thickness { get; set; }
        public string Color { get; set; } // Store as string, can be converted to/from MediaColor
        public bool IsFilled { get; set; } // For shapes that can be filled
    }

    public class CustomCrosshair
    {
        public string Name { get; set; } = "Custom";
        public List<CrosshairElement> Elements { get; set; } = new List<CrosshairElement>();
    }
}