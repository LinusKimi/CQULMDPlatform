using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys.ViewModel
{
    public class ConfigurationData
    {
        public Dictionary<string, string> systemConfig { get; set; }
        public List<particularNodeConfig> NodeTemporaryParameters { get; set; }
    }
}
