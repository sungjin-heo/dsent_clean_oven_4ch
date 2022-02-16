using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JToken jsonData;


//             string RegDir = @".\conf_2";
//             string Path = string.Format(@"{0}\alert_param_setup.json", RegDir);
//             StreamReader sr = System.IO.File.OpenText(Path);
//             using (JsonTextReader j = new JsonTextReader(sr))
//             {
//                 jsonData = JToken.ReadFrom(j);
//             }

            JToken[] Items;

#if false
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_1\alert_param_setup.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            JToken[] Items = jsonData["alert_param_setup"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("D{0}", addr);
                    }
                }

                string wAddr = (string)Item["w.addr"];
                if (!string.IsNullOrEmpty(wAddr))
                {
                    Match m = Regex.Match(wAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["w.addr"] = string.Format("D{0}", addr);
                    }
                }
            }

            File.WriteAllText(@".\conf_2\alert_param_setup.json", jsonData.ToString());

#endif

            int N = 10133;
#if true
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_3\io_x.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            Items = jsonData["io_x"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("D{0}", addr);
                        Item["name"] = string.Format("X{0}", N++);
                    }
                }
            }

            File.WriteAllText(@".\conf_4\io_x.json", jsonData.ToString());

#endif

#if true
            N = 10217;
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_3\io_y.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            Items = jsonData["io_y"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("D{0}", addr);
                        Item["name"] = string.Format("Y{0}", N++);
                    }
                }
            }

            File.WriteAllText(@".\conf_4\io_y.json", jsonData.ToString());

#endif


#if false
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_1\numerics.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            Items = jsonData["numerics"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("D{0}", addr);
                    }
                }

                string wAddr = (string)Item["w.addr"];
                if (!string.IsNullOrEmpty(wAddr))
                {
                    Match m = Regex.Match(wAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["w.addr"] = string.Format("D{0}", addr);
                    }
                }
            }

            File.WriteAllText(@".\conf_2\numerics.json", jsonData.ToString());

#endif

#if false
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_1\parameter.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            string[] keys = new string[] { "temperature", "chamber_ot", "heater_ot", "different_pressure_chamber", "mfc", "different_pressure_filter",
                "motor_chamber", "motor_cooling", "inner_temp_1", "inner_temp_2", "inner_temp_3","inner_temp_4" };

            for (int i = 0; i < keys.Length; i++)
            {
                Items = jsonData[keys[i]].ToArray();
                foreach (var Item in Items)
                {
                    string rAddr = (string)Item["r.addr"];
                    if (!string.IsNullOrEmpty(rAddr))
                    {
                        Match m = Regex.Match(rAddr, @"\d+");
                        if (m.Success)
                        {
                            int addr = int.Parse(m.Value) + 15000;
                            Item["r.addr"] = string.Format("D{0}", addr);
                        }
                    }

                    string wAddr = (string)Item["w.addr"];
                    if (!string.IsNullOrEmpty(wAddr))
                    {
                        Match m = Regex.Match(wAddr, @"\d+");
                        if (m.Success)
                        {
                            int addr = int.Parse(m.Value) + 15000;
                            Item["w.addr"] = string.Format("D{0}", addr);
                        }
                    }
                }
            }
         
            File.WriteAllText(@".\conf_2\parameter.json", jsonData.ToString());

#endif

#if false

            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_1\pattern.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            Items = jsonData["conditions"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("D{0}", addr);
                    }
                }

                string wAddr = (string)Item["w.addr"];
                if (!string.IsNullOrEmpty(wAddr))
                {
                    Match m = Regex.Match(wAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["w.addr"] = string.Format("D{0}", addr);
                    }
                }
            }

            JToken Token = jsonData["segments"];
            for (int i = 0; i < Token.Count(); i++)
            {
                Items = Token[string.Format("SEG{0:D2}", i + 1)].ToArray();

                foreach (var Item in Items)
                {
                    string rAddr = (string)Item["r.addr"];
                    if (!string.IsNullOrEmpty(rAddr))
                    {
                        Match m = Regex.Match(rAddr, @"\d+");
                        if (m.Success)
                        {
                            int addr = int.Parse(m.Value) + 15000;
                            Item["r.addr"] = string.Format("D{0}", addr);
                        }
                    }

                    string wAddr = (string)Item["w.addr"];
                    if (!string.IsNullOrEmpty(wAddr))
                    {
                        Match m = Regex.Match(wAddr, @"\d+");
                        if (m.Success)
                        {
                            int addr = int.Parse(m.Value) + 15000;
                            Item["w.addr"] = string.Format("D{0}", addr);
                        }
                    }
                }
            }

            File.WriteAllText(@".\conf_2\pattern.json", jsonData.ToString());
#endif

#if false
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf_1\relay.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    jsonData = JToken.ReadFrom(Jr);
                }
            }

            Items = jsonData["relays"].ToArray();
            foreach (var Item in Items)
            {
                string rAddr = (string)Item["r.addr"];
                if (!string.IsNullOrEmpty(rAddr))
                {
                    Match m = Regex.Match(rAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["r.addr"] = string.Format("I{0}", addr);
                    }
                }

                string wAddr = (string)Item["w.addr"];
                if (!string.IsNullOrEmpty(wAddr))
                {
                    Match m = Regex.Match(wAddr, @"\d+");
                    if (m.Success)
                    {
                        int addr = int.Parse(m.Value) + 15000;
                        Item["w.addr"] = string.Format("I{0}", addr);
                    }
                }
            }

            File.WriteAllText(@".\conf_2\relay.json", jsonData.ToString());

#endif


        }
    }
}
