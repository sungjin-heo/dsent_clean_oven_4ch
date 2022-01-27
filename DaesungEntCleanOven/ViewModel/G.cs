using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven.Equipment;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DaesungEntCleanOven.Model;
using System.Net.Json;

namespace DaesungEntCleanOven.ViewModel
{
    class G
    {
        static G()
        {
            REALTIME_TREND_CAPACITY = 5;        // UNIT : HOUR.
            REALTIME_TREND_HISTORY = 24;        // UNIT : HOUR.

            // 1. LOAD DEVICE & SYSTEM INFO.
            JToken json;
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf\system.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    json = JToken.ReadFrom(Jr);
                }
            }
            CleanOvenIpAddr = (string)json["device"]["clean_oven_chamber"]["addr"];
            CleanOvenTcp = (int)json["device"]["clean_oven_chamber"]["port"];
            CleanOvenLatencyQueryInterval = (int)json["device"]["clean_oven_chamber"]["latency_query_interval"];
            CleanOvenLatencyQueryItems = (string)json["device"]["clean_oven_chamber"]["latency_query_items"];
            LatencyQueryItems = CleanOvenLatencyQueryItems.Split(',');
            AnalyzerId = (byte)json["device"]["analyzer"]["id"];
            AnalyzerPort = (string)json["device"]["analyzer"]["port"];
            AnalyzerBaudrate = (int)json["device"]["analyzer"]["baud_rate"];
            PatternStorageDir = (string)json["pattern"]["dir"];
            LastestSelectedPatternNo = (int)json["pattern"]["last_selected_pattern"];
            CsvLogStorageDir = (string)json["log"]["csv"]["dir"];
            CsvLogSaveInterval = (int)json["log"]["csv"]["intv"];
            BinaryLogStorageDir = (string)json["log"]["binary"]["dir"];
            BinaryLogSaveInterval = (int)json["log"]["binary"]["intv"];
            AlarmStorageDir = (string)json["log"]["alarm"]["dir"];

            // 2. CREATE DEVICE.
            CleanOven = new Equipment.CleanOven(CleanOvenIpAddr, CleanOvenTcp);
            O2Analyzer = new Equipment.O2Analyzer(AnalyzerPort, AnalyzerBaudrate, AnalyzerId);

            // 3. LOAD PATTERN MODEL.
            Model.Pattern model;
            string path = Path.Combine(PatternStorageDir, string.Format("{0:D3}.xml", LastestSelectedPatternNo));
            if (System.IO.File.Exists(path))
                model = Model.Pattern.LoadFrom(path);
            else
                model = new Model.Pattern() { No = LastestSelectedPatternNo };

            PatternForRun = new PatternViewModel(model);
            PatternForEdit = new PatternViewModel(model);
            PatternForEdit.PatternChanged += (s, e) =>
            {
                if (PatternForEdit.No == PatternForRun.No)
                {
                    if (CleanOven.IsConnected)
                    {
                        if (!CleanOven.IsRunning)
                        {
                            PatternForRun.Load(PatternForEdit.Model);
                            CleanOven.TransferPattern(PatternForEdit);
                        }
                        else
                        {
                            if (PatternForEdit.WaitTemperatureAfterClose != PatternForRun.WaitTemperatureAfterClose)
                            {
                                CleanOven.TransferPatternWaitTemperatureAfterClose(PatternForEdit);
                            }
                        }
                    }
                }
            };

            // 4. LOAD PATTERN METADATAS.
            using (StreamReader Sr = System.IO.File.OpenText(@".\conf\pattern_meta.json"))
            {
                using (JsonTextReader Jr = new JsonTextReader(Sr))
                {
                    json = JToken.ReadFrom(Jr);
                }
            }
            PatternMetaDatas = new List<PatternMetadata>();
            var metaDatas = json["pattern_metadata"].ToArray();
            foreach (var Data in metaDatas)
            {
                PatternMetaDatas.Add(new PatternMetadata()
                {
                    No = (int)Data["no"],
                    Name = (string)Data["name"],
                    IsAssigned = (int)Data["assigned"] == 1,
                    Description = (string)Data["description"],
                    RegisteredScanCode = (string)Data["registered_scancode"]
                });
            }

            // 5. CREATE SYSTEM DIRECTORY.
            if (!Directory.Exists(PatternStorageDir))
                Directory.CreateDirectory(PatternStorageDir);
            if (!Directory.Exists(CsvLogStorageDir))
                Directory.CreateDirectory(CsvLogStorageDir);
            if (!Directory.Exists(BinaryLogStorageDir))
                Directory.CreateDirectory(BinaryLogStorageDir);
            if (!Directory.Exists(AlarmStorageDir))
                Directory.CreateDirectory(AlarmStorageDir);
        }
        public static double REALTIME_TREND_CAPACITY { get; protected set; }
        public static double REALTIME_TREND_HISTORY { get; protected set; }
        public static bool REALTIME_TREND_ON_SEARCH { get; set; }
        public static string CleanOvenIpAddr { get; protected set; }
        public static int CleanOvenTcp { get; protected set; }
        public static int CleanOvenLatencyQueryInterval { get; protected set; }
        protected static string CleanOvenLatencyQueryItems { get; set; }
        public static string[] LatencyQueryItems { get; protected set; }
        public static byte AnalyzerId { get; protected set; }
        public static string AnalyzerPort { get; protected set; }
        public static int AnalyzerBaudrate { get; protected set; }
        public static string PatternStorageDir { get; protected set; }
        public static int LastestSelectedPatternNo { get; set; }
        public static string CsvLogStorageDir { get; protected set; }
        public static int CsvLogSaveInterval { get; protected set; }
        public static string BinaryLogStorageDir { get; protected set; }
        public static int BinaryLogSaveInterval { get; set; }
        public static string AlarmStorageDir { get; protected set; }
        public static Equipment.CleanOven CleanOven { get; protected set; }
        public static Equipment.O2Analyzer O2Analyzer { get; protected set; }
        public static PatternViewModel PatternForRun { get; protected set; }
        public static PatternViewModel PatternForEdit { get; protected set; }
        public static List<Model.PatternMetadata> PatternMetaDatas { get; protected set; }
        public static void SaveSystemConfig()
        {
            try
            {
                JsonObjectCollection root = new JsonObjectCollection();
                JsonObjectCollection jocDev = new JsonObjectCollection("device");
                JsonObjectCollection jocPattern = new JsonObjectCollection("pattern");
                JsonObjectCollection jocLog = new JsonObjectCollection("log");

                JsonObjectCollection joc;

                // DEVICES.
                joc = new JsonObjectCollection("clean_oven_chamber") {
                    new JsonStringValue("addr", CleanOvenIpAddr),
                    new JsonNumericValue("port", CleanOvenTcp),
                    new JsonNumericValue("latency_query_interval", CleanOvenLatencyQueryInterval),
                    new JsonStringValue("latency_query_items", CleanOvenLatencyQueryItems),
                };
                jocDev.Add(joc);

                joc = new JsonObjectCollection("analyzer") {
                    new JsonNumericValue("id", AnalyzerId),
                    new JsonStringValue("port", AnalyzerPort),
                    new JsonNumericValue("baud_rate", AnalyzerBaudrate)
                };
                jocDev.Add(joc);

                // PATTERN.
                jocPattern.Add(new JsonStringValue("dir", PatternStorageDir));
                jocPattern.Add(new JsonNumericValue("last_selected_pattern", LastestSelectedPatternNo));

                // LOG.
                joc = new JsonObjectCollection("alarm") {
                    new JsonStringValue("dir", AlarmStorageDir)
                };
                jocLog.Add(joc);
                joc = new JsonObjectCollection("csv") {
                    new JsonNumericValue("intv", CsvLogSaveInterval),
                    new JsonStringValue("dir", CsvLogStorageDir)
                };
                jocLog.Add(joc);
                joc = new JsonObjectCollection("binary") {
                    new JsonNumericValue("intv", BinaryLogSaveInterval),
                    new JsonStringValue("dir", BinaryLogStorageDir)
                };
                jocLog.Add(joc);

                root.Add(jocDev);
                root.Add(jocPattern);
                root.Add(jocLog);

                string strRoot = root.ToString();
                File.WriteAllText(@".\conf\system.json", strRoot);
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save system config : " + ex.Message);
            }
        }
        public static void SavePatternMetaData()
        {
            try
            {
                JsonObjectCollection root = new JsonObjectCollection();
                JsonArrayCollection jacServer = new JsonArrayCollection("pattern_metadata");
                foreach (var metaData in PatternMetaDatas)
                {
                    JsonObjectCollection joc = new JsonObjectCollection {
                        new JsonNumericValue("no", metaData.No),
                        new JsonStringValue("name", metaData.Name),
                        new JsonNumericValue("assigned", metaData.IsAssigned ? 1 : 0),
                        new JsonStringValue("description", metaData.Description),
                        new JsonStringValue("registered_scancode", metaData.RegisteredScanCode)
                    };
                    jacServer.Add(joc);
                    metaData.Invalidate();
                }
                root.Add(jacServer);
                string strRoot = root.ToString();
                File.WriteAllText(@".\conf\pattern_meta.json", strRoot);
            }
            catch(Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Save pattern metadata : " + ex.Message);
            }
        }
        public static void SelectRunningPattern(int No)
        {
            try
            {
                if (No == PatternForRun.No)
                    return;

                string Message = string.Format("현재 패턴번호 : {0}\r\n변경 패턴번호 : {1}\r\n패턴을 변경 하겠습니까?", PatternForRun.No, No);
                var qDlg = new View.Question(Message);
                if (!(bool)qDlg.ShowDialog())
                    return;

                Model.Pattern patt;
                string path = Path.Combine(G.PatternStorageDir, string.Format("{0:D3}.xml", No));
                if (File.Exists(path))
                    patt = Model.Pattern.LoadFrom(path);
                else
                    patt = new Model.Pattern(true) { No = No };

                if (patt != null)
                {
                    PatternForRun.Load(patt);
                    PatternForEdit.Load(patt);
                    G.LastestSelectedPatternNo = No;
                    G.SaveSystemConfig();
                    CleanOven.TransferPattern(PatternForRun);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Load pattern config : " + ex.Message);
            }
        }
        public static void SelectEditPattern(int No)
        {
            try
            {
                if (No == PatternForEdit.No)
                    return;

                string Message = string.Format("현재 패턴번호 : {0}\r\n변경 패턴번호 : {1}\r\n패턴을 변경 하겠습니까?", PatternForEdit.No, No);
                var qDlg = new View.Question(Message);
                if (!(bool)qDlg.ShowDialog())
                    return;

                Model.Pattern patt;
                string path = Path.Combine(G.PatternStorageDir, string.Format("{0:D3}.xml", No));
                if (File.Exists(path))
                    patt = Model.Pattern.LoadFrom(path);
                else
                    patt = new Model.Pattern(true) { No = No };

                if (patt != null)
                {
                    PatternForEdit.Load(patt);
                    PatternMetaDatas[No - 1].Name = PatternForEdit.Name;
                    PatternMetaDatas[No - 1].IsAssigned = true;
                    SavePatternMetaData();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Dispatch("e", "Exception is Occured while to Load Pattern Config : " + ex.Message);
            }
        }
    }
}
