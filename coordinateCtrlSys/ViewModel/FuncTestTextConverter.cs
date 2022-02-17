using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace coordinateCtrlSys.ViewModel
{
    public class FuncTestTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string connect = "未测试";

            switch ((int)value)
            {
                case 0:
                    connect = "未测试";
                    break;

                case 1:
                    connect = "通过";
                    break;

                case 2:
                    connect = "不通过";
                    break;

                default:
                    break;
            }

            return connect;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
