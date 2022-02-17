using coordinateCtrlSys.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coordinateCtrlSys
{
    public class ReadConfiguration : IConfigReader
    {
        public ConfigurationData ReadFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                using (var streamReader = new StreamReader(fs, Encoding.GetEncoding("gb2312")))
                {
                    var jsonData = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<ConfigurationData>(jsonData);
                }
            }
        }

    }
}
