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
        public ConfigurationData ReadFile()
        {
            using (var fs = new FileStream("./Settings/config.json", FileMode.Open))
            {
                using (var streamReader = new StreamReader(fs, Encoding.GetEncoding("gb2312")))
                {
                    var jsonData = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<ConfigurationData>(jsonData);
                }
            }
        }

        public void WriteFile(ConfigurationData s)
        {
            string ss = JsonConvert.SerializeObject(s, Formatting.Indented);
         
            File.WriteAllText("./Settings/config.json", ss, Encoding.UTF8);
        }
    }
}
