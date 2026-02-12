using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenRdpGuard.Converters
{
    public class CountToMaxRowsHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int n ? n : 0;

            // Keep natural height for up to 5 items to avoid blank area.
            if (count <= 5)
            {
                return double.PositiveInfinity;
            }

            var rowHeight = 38.0;
            if (parameter != null &&
                double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) &&
                parsed > 0)
            {
                rowHeight = parsed;
            }

            return rowHeight * 5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
