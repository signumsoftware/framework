using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using Signum.Utilities;

namespace Signum.Windows
{
    public class GradientConverter:IValueConverter
    {
        GradientStopCollection gradientStops = new GradientStopCollection();
        public GradientStopCollection GradientStops
        {
            get { return gradientStops; }
            set { gradientStops = value; }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            float v = System.Convert.ToSingle(value);

            Color color = GetColor(v);

            return new SolidColorBrush(color); 
        }

        private Color GetColor(float v)
        {
            if (v <= gradientStops.Min(a => a.Offset))
                return gradientStops.WithMin(a => a.Offset).Color;

            if (v >= gradientStops.Max(a => a.Offset))
                return gradientStops.WithMax(a => a.Offset).Color;

            var par = GradientStops.Select((gr, i) => new { gr, i }).FirstOrDefault(p => p.gr.Offset > v);

            float v1 = (float)GradientStops[par.i - 1].Offset;

            float v2 = (float)GradientStops[par.i].Offset;

            float vx = (v - v1) / (v2 - v1);

            return GradientStops[par.i - 1].Color * (1 - vx) + GradientStops[par.i].Color * (vx);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

      
    }
}
