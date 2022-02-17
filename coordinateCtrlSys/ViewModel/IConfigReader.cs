using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys.ViewModel
{
    public interface IConfigReader
    {
        ConfigurationData ReadFile(string path);

    }
}
