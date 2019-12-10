using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Parse12306
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateDirectory(GetRootPath());
            EnableSSL();
            OutputReadME();
            while (true)
            {
                string inputStr = Console.ReadLine().ToUpper().Trim();
                if (inputStr == "Q")
                {
                    break;
                }
                else if (inputStr == "1")
                {
                    RunStep(Step1);
                }
                else if (inputStr == "2")
                {
                    RunStep(Step2);
                }
                else if (inputStr == "3")
                {
                    RunStep(Step3);
                }
                else if (inputStr == "4")
                {
                    RunStep(Step4);
                }
                else if (inputStr == "5")
                {
                    RunStep(Step5);
                }
                else if (inputStr == "6")
                {
                    RunStep(Step6);
                }
                else if (inputStr == "7")
                {
                    RunStep(Step7);
                }
                else
                {
                    OutputReadME();
                }
            }
        }

        #region Other

        const string STEP_1 = "step_1";
        const string STEP_2 = "step_2";
        const string STEP_3 = "step_3";
        const string STEP_4 = "step_4";
        const string STEP_5 = "step_5";
        const string STEP_6 = "step_6";
        const string STEP_7 = "step_7";

        const string FILE_1 = "station_name.js";
        const string FILE_2 = "station_name.txt";
        const string FILE_3 = "train_list.js";
        const string FILE_5 = "train_list.txt";
        const string FILE_7_STATION = "station.txt";
        const string FILE_7_TRAIN = "train.txt";
        const string FILE_7_TIMETABLE = "timetable.txt";

        static void OutputReadME()
        {
            Console.WriteLine(@"1. Download station list from 12306");
            Console.WriteLine(@"2. Parse station lists");
            Console.WriteLine(@"3. Download train list from 12306");
            Console.WriteLine(@"4. Parse train list by date");
            Console.WriteLine(@"5. Parse all train list and url list");
            Console.WriteLine(@"6. Download train detail");
            Console.WriteLine(@"7. Parse train detail");
            Console.WriteLine(@"Please input number to run.");
            Console.WriteLine(@"Please input Q to quit.");
        }

        static void RunStep(Action action)
        {
            Console.WriteLine("Start");
            try
            {
                action();
                Console.WriteLine("Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Failure");
            }
            Console.WriteLine("End");
        }

        #endregion

        #region Step1

        static void Step1()
        {
            CreateDirectory(GetStepPath(STEP_1));
            Console.WriteLine("Downloading... (about 90KB)");
            DownloadFile(@"https://kyfw.12306.cn/otn/resources/js/framework/station_name.js", GetStepFile(STEP_1, FILE_1));
        }

        #endregion

        #region Step2

        static string StationNotFoundIn12306 = @"
10001	春申	CSA	chunshen	cs	csh
10002	新桥	XQH	xinqiao	xq	xqi
10003	车墩	MIH	chedun	cd	cdu
10004	叶榭	YOH	yexie	yx	yxi
10005	亭林	TVH	tinglin	tl	tli
10006	金山园区	REH	jinshanyuanqu	jsyq	jsq
10007	金山卫	BGH	jinshanwei	jsw	fsw";

        static void Step2()
        {
            CreateDirectory(GetStepPath(STEP_2));
            //var station_names ='@bjb|北京北|VAP|beijingbei|bjb|0@bjd|北京东|BOP|beijingdong|bjd|1';
            string text = ReadFile(GetStepFile(STEP_1, FILE_1)).Trim();
            text = SubText(text, "'", "'");
            List<Station> stationList = new List<Station>();
            List<string> rowList = SplitText(text, "@", StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in rowList)
            {
                List<string> itemList = SplitText(str.Trim(), "|");
                Station station = new Station();
                station.ID = itemList[5];
                station.Name = itemList[1];
                station.TelCode = itemList[2];
                station.PinYin = itemList[3];
                station.PY = itemList[4];
                station.PYCode = itemList[0];
                stationList.Add(station);
            }
            stationList.Sort((obj1, obj2) => Convert.ToInt32(obj1.ID).CompareTo(Convert.ToInt32(obj2.ID)));
            List<string> outputList = new List<string>();
            foreach (Station station in stationList)
            {
                outputList.Add(station.ToBaseCSV());
            }
            WriteFile(GetStepFile(STEP_2, FILE_2), string.Join("\r\n", outputList.ToArray()) + StationNotFoundIn12306);
        }

        #endregion

        #region Step3

        static void Step3()
        {
            CreateDirectory(GetStepPath(STEP_3));
            Console.WriteLine("Downloading... (about 35MB)");
            DownloadFile(@"https://kyfw.12306.cn/otn/resources/js/query/train_list.js", GetStepFile(STEP_3, FILE_3));
        }

        #endregion

        #region Step4

        static void Step4()
        {
            CreateDirectory(GetStepPath(STEP_4));
            List<Station> stationList = LoadBaseStation();
            //var train_list ={"2016-01-31":{"D":[{"station_train_code":"D1(北京-沈阳)","train_no":"24000000D10R"}]}}
            string text = ReadFile(GetStepFile(STEP_3, FILE_3)).Trim();
            text = text.Substring(text.IndexOf("=") + 1);
            JObject jsonObj = JObject.Parse(text);
            List<string> dateList = jsonObj.Properties().Select(p => p.Name).ToList();
            dateList.Sort();
            foreach (string date in dateList)
            {
                List<Train> trainList = new List<Train>();
                JObject dateObj = (JObject)jsonObj[date];
                List<string> typeList = dateObj.Properties().Select(p => p.Name).ToList();
                foreach (string type in typeList)
                {
					//type = "C,D,G,K,O,T,Z", we just get high speed trains. It means "C,D,G"
					// User may also comment the 'if' clause or modify the "CDG" to your own demand.
					if ("CDG".Contains(type))
					{
						JArray dataArray = (JArray)dateObj[type];
                        foreach (JObject data in dataArray)
                        {
                            string trainCode = data["station_train_code"].ToString(); //D1(北京-沈阳)
                            string trainNO = data["train_no"].ToString();
                            List<string> itemList = trainCode.Split(new string[] { "(", "-", ")" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            Train train = new Train();
                            train.Type = type;
                            train.Name = itemList[0];
                            train.TrainNo = trainNO;
                            train.StartStation = stationList.Find(obj => obj.Name == itemList[1].Replace(" ", string.Empty));
                            train.EndStation = stationList.Find(obj => obj.Name == itemList[2].Replace(" ", string.Empty));
                            trainList.Add(train);
                        }
                    }
                }
                trainList.Sort((obj1, obj2) =>
                {
                    int c1 = obj1.Type.ToUpper().CompareTo(obj2.Type.ToUpper());
                    int c2 = Convert.ToInt32(obj1.Name.Substring(1)).CompareTo(Convert.ToInt32(obj2.Name.Substring(1)));
                    int c3 = obj1.TrainNo.ToUpper().CompareTo(obj2.TrainNo.ToUpper());
                    return (c1 != 0) ? c1 : ((c2 != 0) ? c2 : c3);
                });
                if (trainList.Count > 0)
                {
                    List<string> outputList = new List<string>();
                    foreach (Train train in trainList)
                    {
						String tmp = train.ToBaseCSV();
						if (tmp != String.Empty) outputList.Add(tmp);
                    }
                    WriteFile(GetStepFile(STEP_4, date + ".txt"), string.Join("\r\n", outputList.ToArray()));
                    Console.WriteLine("Output " + date + ".txt");
                }
            }
        }

        #endregion

        #region Step5

        static void Step5()
        {
            CreateDirectory(GetStepPath(STEP_5));
            List<string> fileList = Directory.GetFiles(GetStepPath(STEP_4)).ToList();
            List<string> allDateList = new List<string>();
            foreach (string file in fileList)
            {
                string dateStr = file.Substring(file.Length - 14, 10);
                allDateList.Add(dateStr);
            }
            Train.AllDateList = allDateList;
            Dictionary<string, Train> trainDic = new Dictionary<string, Train>();
            foreach (string file in fileList)
            {
                string dateStr = file.Substring(file.Length - 14, 10);
                Console.WriteLine("Parsing " + dateStr);
                List<Train> trainList = LoadBaseTrain(file);
                foreach (Train train in trainList)
                {
                    if (trainDic.ContainsKey(train.Key))
                    {
                        trainDic[train.Key].RunDateList.Add(dateStr);
                    }
                    else
                    {
                        trainDic.Add(train.Key, train);
                        trainDic[train.Key].RunDateList.Add(dateStr);
                    }
                }
            }
            List<Train> allTrainList = trainDic.Values.ToList();
            allTrainList.Sort((obj1, obj2) =>
            {
                int c1 = obj1.Type.ToUpper().CompareTo(obj2.Type.ToUpper());
                int c2 = Convert.ToInt32(obj1.Name.Substring(1)).CompareTo(Convert.ToInt32(obj2.Name.Substring(1)));
                int c3 = obj1.TrainNo.ToUpper().CompareTo(obj2.TrainNo.ToUpper());
                return (c1 != 0) ? c1 : ((c2 != 0) ? c2 : c3);
            });
            List<string> outputList = new List<string>();
            outputList.Add(string.Join("|", allDateList.ToArray()));
            foreach (Train train in allTrainList)
            {
                outputList.Add(train.ToRunDateCSV());
            }
            WriteFile(GetStepFile(STEP_5, FILE_5), string.Join("\r\n", outputList.ToArray()));
        }

        #endregion

        #region Step6

        static void Step6()
        {
            CreateDirectory(GetStepPath(STEP_6));
            List<Train> trainList = LoadTrain(GetStepFile(STEP_5, FILE_5));
            int total = trainList.Count;
            int ok = 0;
            int ng = 0;
            for (int i = 0; i < trainList.Count; i++)
            {
                if (File.Exists(GetStepFile(STEP_6, trainList[i].Key + ".txt")))
                {
                    ok++;
                }
                else
                {
                    string json = GetUrlString(trainList[i].TimetableUrl);
                    if (json.Contains("\"httpstatus\":200") && !json.Contains("\"data\":[]"))
                    {
                        WriteFile(GetStepFile(STEP_6, trainList[i].Key + ".txt"), json);
                        ok++;
                    }
                    else
                    {
                        ng++;
                        Console.WriteLine(trainList[i].Key);
                    }
                }
                Console.WriteLine(string.Format("Total:{0} OK:{1} NG:{2}", total, ok, ng));
            }
        }

        #endregion

        #region Step7

        static void Step7()
        {
            CreateDirectory(GetStepPath(STEP_7));
            List<Train> trainList = LoadTrain(GetStepFile(STEP_5, FILE_5));
            Dictionary<string, List<Train>> trainDic = new Dictionary<string, List<Train>>();
            foreach (Train train in trainList)
            {
                string jsonStr = ReadFile(GetStepFile(STEP_6, train.Key + ".txt"));
                train.AddTimetable(jsonStr);
                if (trainDic.ContainsKey(train.TimetableKey))
                {
                    trainDic[train.TimetableKey].Add(train);
                }
                else
                {
                    trainDic.Add(train.TimetableKey, new List<Train>());
                    trainDic[train.TimetableKey].Add(train);
                }
            }
            List<string> outputTimetableList = new List<string>();
            List<Station> outputStationList = new List<Station>();
            List<string> outputTrainList = new List<string>();
            foreach (string key in trainDic.Keys)
            {
                List<string> trainNameList = new List<string>();
                List<string> runDateList = new List<string>();
                List<string> infoList = new List<string>();
                Train outputTrain = null;
                foreach (Train train in trainDic[key])
                {
                    if (!trainNameList.Contains(train.Name)) trainNameList.Add(train.Name);
                    foreach (string date in train.RunDateList)
                    {
                        if (!runDateList.Contains(date)) runDateList.Add(date);
                    }
                    infoList.Add(string.Format("{0}|{1}|{2}", train.Name, train.TrainNo, train.RunDateStr));
                    if (train.Name == train.TrainCode) outputTrain = train;
                }
                trainNameList.Sort((obj1, obj2) =>
                {
                    return Convert.ToInt32(obj1.Substring(1)).CompareTo(Convert.ToInt32(obj2.Substring(1)));
                });
                for (int i = 0; i < outputTrain.TimetableList.Count; i++)
                {
                    List<string> rowList = new List<string>();
                    rowList.Add((i != 0) ? string.Empty : string.Join("/", trainNameList.ToArray()));//Checi
                    rowList.Add((i != 0) ? string.Empty : outputTrain.StartStation.Name);//StartStation
                    rowList.Add((i != 0) ? string.Empty : outputTrain.EndStation.Name);//EndStation
                    rowList.Add((i != 0) ? string.Empty : outputTrain.TimetableList[0].StartTime);//StartTime
                    rowList.Add((i != 0) ? string.Empty : outputTrain.TimetableList[outputTrain.TimetableList.Count - 1].ArriveTime);//EndTime
                    rowList.Add((i != 0) ? string.Empty : outputTrain.TrainClass);//TrainClass
                    rowList.Add((i != 0) ? string.Empty : outputTrain.ServiceType);//ServiceType
                    rowList.Add(outputTrain.TimetableList[i].ToCSV());//Timetable
                    rowList.Add((i != 0) ? string.Empty : (runDateList.Count == Train.AllDateList.Count) ? "ALL" : string.Join("|", runDateList.ToArray()));//RunDate
                    rowList.Add((i != 0) ? string.Empty : string.Join("_", infoList.ToArray()));//Detail
                    outputTimetableList.Add(string.Join("\t", rowList.ToArray()));
                    if (!outputStationList.Contains(outputTrain.TimetableList[i].Station))
                    {
                        outputStationList.Add(outputTrain.TimetableList[i].Station);
                    }
                    if (i == 0)
                    {
                        outputTrainList.Add(string.Join("\t", rowList.GetRange(0, 7).ToArray()));
                    }
                }
            }
            outputStationList.Sort((obj1, obj2) => { return Convert.ToInt32(obj1.ID).CompareTo(Convert.ToInt32(obj2.ID)); });
            List<string> outputTempList = new List<string>();
            foreach (Station temp in outputStationList)
            {
                outputTempList.Add(temp.ToBaseCSV());
            }
            WriteFile(GetStepFile(STEP_7, FILE_7_STATION), string.Join("\r\n", outputTempList.ToArray()));
            WriteFile(GetStepFile(STEP_7, FILE_7_TRAIN), string.Join("\r\n", outputTrainList.ToArray()));
            WriteFile(GetStepFile(STEP_7, FILE_7_TIMETABLE), string.Join("\r\n", outputTimetableList.ToArray()));
        }

        #endregion

        #region Lib

        #region Internet

        public static void EnableSSL()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        public static void DownloadFile(string url, string fileName)
        {
            using (var wc = new WebClient())
            {
                wc.DownloadFile(url, fileName);
            }
        }

        public static string GetUrlString(string url)
        {
            string result = string.Empty;
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Stream data = client.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            result = reader.ReadToEnd();
            reader.Close();
            return result;
        }

        #endregion

        #region I/O

        public static string ReadFile(string fileName)
        {
            string result = string.Empty;
            StreamReader sr = new StreamReader(fileName);
            result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        public static void WriteFile(string fileName, string contents)
        {
            StreamWriter sw = new StreamWriter(fileName);
            sw.Write(contents);
            sw.Flush();
            sw.Close();
        }

        #endregion

        #region String

        public static string SubText(string text, string startStr, string endStr)
        {
            int startIndex = text.IndexOf("'");
            int endIndex = text.LastIndexOf("'");
            return text.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        public static List<string> SplitText(string text, string separator, StringSplitOptions opt = StringSplitOptions.None)
        {
            return text.Split(new string[] { separator }, opt).ToList();
        }

        #endregion

        #region Common

        public static string GetRootPath()
        {
            return Path.Combine(System.Environment.CurrentDirectory, "output");
        }

        public static string GetStepPath(string step)
        {
            return Path.Combine(GetRootPath(), step);
        }

        public static string GetStepFile(string step, string file)
        {
            return Path.Combine(GetStepPath(step), file);
        }

        public static void CreateDirectory(string name)
        {
            if (!Directory.Exists(name))
            {
                Directory.CreateDirectory(name);
            }
        }

        #endregion

        #region Logic

        public static List<Station> LoadBaseStation()
        {
            List<Station> result = new List<Station>();
            string file = ReadFile(GetStepFile(STEP_2, FILE_2));
            List<string> rowList = SplitText(file, "\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string row in rowList)
            {
                result.Add(Station.FromBaseCSV(row));
            }
            return result;
        }

        public static List<Train> LoadBaseTrain(string fileName)
        {
            List<Train> result = new List<Train>();
            if (Train.AllStationList.Count == 0) Train.AllStationList = LoadBaseStation();
            string file = ReadFile(fileName);
            List<string> rowList = SplitText(file, "\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string row in rowList)
            {
                result.Add(Train.FromBaseCSV(row));
            }
            return result;
        }

        public static List<Train> LoadTrain(string fileName)
        {
            List<Train> result = new List<Train>();
            if (Train.AllStationList.Count == 0) Train.AllStationList = LoadBaseStation();
            string file = ReadFile(fileName);
            List<string> rowList = SplitText(file, "\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < rowList.Count; i++)
            {
                if (i == 0)
                {
                    Train.AllDateList = SplitText(rowList[i], "|", StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                else
                {
                    result.Add(Train.FromRunDateCSV(rowList[i]));
                }
            }
            return result;
        }

        #endregion

        #endregion
    }

    #region Class

    class Station
    {
        public string ID = string.Empty;
        public string Name = string.Empty;
        public string TelCode = string.Empty;
        public string PinYin = string.Empty;
        public string PY = string.Empty;
        public string PYCode = string.Empty;

        public string ToBaseCSV()
        {
            List<string> list = new List<string>();
            list.Add(ID);
            list.Add(Name);
            list.Add(TelCode);
            list.Add(PinYin);
            list.Add(PY);
            list.Add(PYCode);
            return string.Join("\t", list.ToArray());
        }

        public static Station FromBaseCSV(string row)
        {
            List<string> itemList = row.Split(new string[]{"\t"}, StringSplitOptions.None).ToList();
            Station obj = new Station();
            obj.ID = itemList[0];
            obj.Name = itemList[1];
            obj.TelCode = itemList[2];
            obj.PinYin = itemList[3];
            obj.PY = itemList[4];
            obj.PYCode = itemList[5];
            return obj;
        }
    }

    class Train
    {
        public static List<string> AllDateList = new List<string>();
        public static List<Station> AllStationList = new List<Station>();

        public string Type = string.Empty; //C D G K O T Z
        public string Name = string.Empty; //D1
        public string TrainNo = string.Empty; //24000000D10R
        public Station StartStation = null;
        public Station EndStation = null;
        public List<string> RunDateList = new List<string>();

        public string TrainCode = string.Empty;
        public string TrainClass = string.Empty;
        public string ServiceType = string.Empty;
        public List<Timetable> TimetableList = new List<Timetable>();

        public List<Train> SameTrainList = new List<Train>();

        public string TimetableKey
        {
            get
            {
                List<string> list = new List<string>();
                list.Add(TrainCode);
                list.Add(StartStation.Name);
                list.Add(EndStation.Name);
                list.Add(TrainClass);
                list.Add(ServiceType);
                foreach (Timetable timetable in TimetableList)
                {
                    list.Add(timetable.ToCSV().Replace("\t", "_"));
                }
                return string.Join("_", list.ToArray());
            }
        }

        public string Key
        {
            get { return string.Format("{0}_{1}", Name, TrainNo); }
        }

        public string RunDateStr
        {
            get
            {
                return RunDateList.Count == AllDateList.Count ? "ALL" : string.Join("|", RunDateList.ToArray());
            }
        }

        public string TimetableUrl
        {
            get { return string.Format("https://kyfw.12306.cn/otn/czxx/queryByTrainNo?train_no={0}&from_station_telecode={1}&to_station_telecode={2}&depart_date={3}", TrainNo, StartStation.TelCode, EndStation.TelCode, RunDateList[RunDateList.Count - 1]); }
        }

        public string ToBaseCSV()
        {
			if (StartStation == null || EndStation == null) return String.Empty;
			List<string> list = new List<string>();
            list.Add(Type);
            list.Add(Name);
            list.Add(TrainNo);
            list.Add(StartStation.Name);
            list.Add(EndStation.Name);
            return string.Join("\t", list.ToArray());
        }

        public string ToRunDateCSV()
        {
            List<string> list = new List<string>();
            list.Add(Type);
            list.Add(Name);
            list.Add(TrainNo);
            list.Add(StartStation.Name);
            list.Add(EndStation.Name);
            list.Add(RunDateStr);
            list.Add(TimetableUrl);
            return string.Join("\t", list.ToArray());
        }

        public string ToTimetableCSV()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < TimetableList.Count; i++)
            {
                string preInfo = string.Empty;
                if (i == 0)
                {
                    List<string> baseList = new List<string>();
                    baseList.Add(Type);
                    List<string> nameList = new List<string>();
                    List<string> trainNoList = new List<string>();
                    foreach (Train train in SameTrainList)
                    {
                        nameList.Add(Name);
                        trainNoList.Add(TrainNo);
                    }
                    baseList.Add(string.Join("/", nameList.ToArray()));
                    baseList.Add(string.Join("/", trainNoList.ToArray()));
                    baseList.Add(StartStation.Name);
                    baseList.Add(EndStation.Name);
                    preInfo = string.Format("{0}\t{1}\t{2}", string.Join("\t", baseList.ToArray()), TrainClass, ServiceType);
                }
                else
                {
                    preInfo = string.Format("{0}\t{1}\t{2}", "\t\t\t\t", string.Empty, string.Empty);
                }
                list.Add(string.Format("{0}\t{1}", preInfo, TimetableList[i].ToCSV()));
            }
            return string.Join("\r\n", list.ToArray());
        }

        public static Train FromBaseCSV(string row)
        {
            List<string> itemList = row.Split(new string[] { "\t" }, StringSplitOptions.None).ToList();
            Train obj = new Train();
            obj.Type = itemList[0];
            obj.Name = itemList[1];
            obj.TrainNo = itemList[2];
            obj.StartStation = AllStationList.Find(p => p.Name == itemList[3]);
            obj.EndStation = AllStationList.Find(p => p.Name == itemList[4]);
            return obj;
        }

        public static Train FromRunDateCSV(string row)
        {
            List<string> itemList = row.Split(new string[] { "\t" }, StringSplitOptions.None).ToList();
            Train obj = new Train();
            obj.Type = itemList[0];
            obj.Name = itemList[1];
            obj.TrainNo = itemList[2];
            obj.StartStation = AllStationList.Find(p => p.Name == itemList[3]);
            obj.EndStation = AllStationList.Find(p => p.Name == itemList[4]);
            if (itemList[5] == "ALL")
            {
                obj.RunDateList = AllDateList;
            }
            else
            {
                obj.RunDateList = itemList[5].Split(new string[]{"|"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            return obj;
        }

        public void AddTimetable(string jsonStr)
        {
            try
            {
                JObject jsonObj = JObject.Parse(jsonStr);
                JArray data = (JArray)jsonObj["data"]["data"];
                for (int i = 0; i < data.Count; i++)
                {
                    if (i == 0)
                    {
                        string StationTrainCode = data[i]["station_train_code"].ToString();
                        string StartStationName = data[i]["start_station_name"].ToString().Replace(" ", string.Empty);
                        string EndStationName = data[i]["end_station_name"].ToString().Replace(" ", string.Empty);
                        string TrainClassName = data[i]["train_class_name"].ToString();
                        string ServiceType = data[i]["service_type"].ToString();
                        this.TrainCode = StationTrainCode.ToUpper();
                        if (StartStationName != StartStation.Name) throw new Exception(StartStationName);
                        if (EndStationName != EndStation.Name) throw new Exception(EndStationName);
                        this.TrainClass = TrainClassName;
                        this.ServiceType = ServiceType;
                    }
                    TimetableList.Add(new Timetable((JObject)data[i]));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Key);
                Console.WriteLine(ex.Message);
            }
        }
    }

    class Timetable
    {
        public string StationNO = string.Empty;
        public Station Station = null;
        public string ArriveTime = string.Empty;
        public string StartTime = string.Empty;
        public string StopoverTime = string.Empty;
        public string IsEnabled = string.Empty;

        public Timetable(JObject jsonObj)
        {
            StationNO = jsonObj["station_no"].ToString();
            Station = Train.AllStationList.Find(p => p.Name == jsonObj["station_name"].ToString().Replace(" ", string.Empty));
            ArriveTime = jsonObj["arrive_time"].ToString();
            StartTime = jsonObj["start_time"].ToString();
            StopoverTime = jsonObj["stopover_time"].ToString();
            IsEnabled = jsonObj["isEnabled"].ToString();
        }

        public string ToCSV()
        {
            List<string> list = new List<string>();
            list.Add(StationNO);
            list.Add(Station.Name);
            list.Add(ArriveTime);
            list.Add(StartTime);
            list.Add(StopoverTime);
            list.Add(IsEnabled);
            return string.Join("\t", list.ToArray());
        }
    }

    #endregion
}
