using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace coordinateCtrlSys.ViewModel
{
    public class DevConnectForgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = Brushes.Blue;

            switch ((int)value)
            {
                case 0:
                    color = Brushes.Blue;
                    break;

                case 1:
                    color = Brushes.Red;
                    break;

                case 2:
                    color = Brushes.Green;
                    break;

                default:
                    break;
            }

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
