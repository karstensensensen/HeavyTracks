using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace HeavyTracks.Converters
{
    internal class ListboxMargin : IValueConverter
    {
        static readonly Thickness default_margin_thickness = new Thickness(6, 0, 6, 0);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Trace.WriteLine("ATTEMPT CONVERT");

            Thickness thickness = (Thickness)value;

            thickness.Left -= default_margin_thickness.Left;
            thickness.Top -= default_margin_thickness.Top;
            thickness.Right -= default_margin_thickness.Right;
            thickness.Bottom -= default_margin_thickness.Bottom;

            return "5, 100, 2, 1000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness thickness = (Thickness)value;

            thickness.Left += default_margin_thickness.Left;
            thickness.Top += default_margin_thickness.Top;
            thickness.Right += default_margin_thickness.Right;
            thickness.Bottom += default_margin_thickness.Bottom;

            return thickness;
        }
    }
}
