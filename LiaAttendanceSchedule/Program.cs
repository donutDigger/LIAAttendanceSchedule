using LiaAttendanceSchedule.Info;
using LiaAttendanceSchedule.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace LiaAttendanceSchedule
{
    class Program
    {
        public static SettingModel setting = new SettingModel();
        static void Main(string[] args)
        {
            DeviceManipulator manipulator = new DeviceManipulator();
            ZkemClient objZkeeper = new ZkemClient();

            setting = ReadSetting();

            SendEmail("hey tayo hey tayo bus kecil ramah lingkungan");

            try
            {
                objZkeeper.Connect_Net(setting.IpAddress, Int32.Parse(setting.Port));
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with Connect_net()", ex.ToString());
            }

            var machineLogs = GetMachineLog(manipulator, objZkeeper);

            var attendanceTempLog = ReadAttendanceTempLog();

            attendanceTempLog = AddNewMachineLogToTemp(machineLogs, attendanceTempLog);

            attendanceTempLog = GenerateAttendanceLogToHit(attendanceTempLog);

            WriteAttendanceTempLog(attendanceTempLog);

            try
            {
                objZkeeper.Disconnect();
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with Disconnect()", ex.ToString());
                //WriteToErrorLog(ex.ToString());
            }
        }

        static ICollection<MachineInfo> GetMachineLog(DeviceManipulator manipulator, ZkemClient objZkeeper)
        {
            var machineLogs = new List<MachineInfo>();

            try
            {
                machineLogs = manipulator.GetLogData(objZkeeper, 1).ToList();
                machineLogs = machineLogs.Where(i => i.DateOnlyRecord.Date >= DateTime.Now.Date && i.DateOnlyRecord.Date <= DateTime.Now.Date).ToList();
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with GetMachineLog()", ex.ToString());
            }
            return machineLogs;
        }

        static List<AttendanceLogModel> AddNewMachineLogToTemp (ICollection<MachineInfo> machineInfos, List<AttendanceLogModel> attendanceTempLog)
        {
            try
            {
                foreach (var info in machineInfos)
                {
                    if (!attendanceTempLog.Any(l => l.DateTimeAttendance == info.DateTimeAttendance))
                    {
                        attendanceTempLog.Add(
                            new AttendanceLogModel()
                            {
                                IndRegID = info.RegID,
                                DateTimeRecord = info.DateTimeRecord,
                                Status = false
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with AddNewMachineLogToTemp()", ex.ToString());
            }
            return attendanceTempLog;
        }

        static List<AttendanceLogModel> GenerateAttendanceLogToHit (List<AttendanceLogModel> attendanceTempLog)
        {
            try
            {
                foreach (var attendance in attendanceTempLog.Where(a => a.Status == false).ToList())
                {
                    var url = $"{ Resources.Setting.HostAttendance }&fingerprintid={ attendance.IndRegID }&dateTimeRecord={ attendance.DateTimeAttendance }";
                    var htmlReponse = GetReq(url);

                    if (htmlReponse.Equals("null"))
                    {
                        attendance.Status = true;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with GenerateAttendanceLogToHit()", ex.ToString());
            }
            return attendanceTempLog;
        }

        static List<AttendanceLogModel> ReadAttendanceTempLog ()
        {
            var attendanceTempLog = new List<AttendanceLogModel>();
            try
            {
                string jsonText;
                string jsonFileName = $"{AppDomain.CurrentDomain.BaseDirectory}attendanceLog{ DateTime.Now.Date.ToString("-yyyy-MM-dd") }.json";
                if (File.Exists(jsonFileName))
                {
                    jsonText = File.ReadAllText(jsonFileName);
                }
                else
                {
                    var file = File.CreateText(jsonFileName);
                    file.Close();
                    File.WriteAllText(jsonFileName, "[]");
                    file.Close();
                    jsonText = File.ReadAllText(jsonFileName);
                }

                attendanceTempLog = JsonConvert.DeserializeObject<IList<AttendanceLogModel>>(jsonText).ToList();
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with ReadAttendanceTempLog()", ex.ToString());
            }
            return attendanceTempLog;
        }

        static void WriteAttendanceTempLog (List<AttendanceLogModel> attendanceTempLog)
        {
            try
            {
                string jsonFileName = $"{AppDomain.CurrentDomain.BaseDirectory}attendanceLog{ DateTime.Now.Date.ToString("-yyyy-MM-dd") }.json";
                string output = JsonConvert.SerializeObject(attendanceTempLog);
                using (StreamWriter file = (File.Exists(jsonFileName)) ? new StreamWriter(jsonFileName, false) : File.CreateText(jsonFileName))
                {
                    file.Write(output);
                }
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with WriteAttendanceTempLog()", ex.ToString());
            }
        }

        static string GetReq(string url)
        {
            string htmlResponse = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    htmlResponse = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with GetReq()", ex.ToString());

                //var doesConnectionExist = UniversalStatic.PingTheDevice(setting.IpAddress);

                if (UniversalStatic.PingTheDevice(setting.IpAddress)) {
                    Stopwatch sw1 = new Stopwatch();
                    sw1.Start();
                    while (true)
                    {
                        if (sw1.ElapsedMilliseconds > 5000)
                        {
                            sw1.Stop();
                            if (!UniversalStatic.PingTheDevice(setting.IpAddress))
                            {
                                sw1.Start();
                                while (true)
                                {
                                    if (sw1.ElapsedMilliseconds > 5000)
                                    {
                                        sw1.Stop();
                                        if (!UniversalStatic.PingTheDevice(setting.IpAddress))
                                        {
                                            WriteToErrorLog("Something wrong with GetReq() \r\n Probably doen\'t connect to fingerprint machine", ex.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return htmlResponse;
        }

        static SettingModel ReadSetting ()
        {
            var settingModel = new SettingModel();
                        
            try
            {
                using (StreamReader file = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}Setting.txt"))
                {
                    string ln;

                    while ((ln = file.ReadLine()) != null)
                    {
                        var setting = ln.Split('=');

                        if (setting[0] == "ip_machine")
                        {
                            settingModel.IpAddress = setting[1];
                        }
                        else if (setting[0] == "port")
                        {
                            settingModel.Port = setting[1];
                        }
                        else if (setting[0] == "email_receivers")
                        {
                            settingModel.EmailReceivers = setting[1];
                        }
                    }
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                WriteToErrorLog("Something wrong with ReadSetting()", ex.ToString());
            }
            return settingModel;
        }

        static void WriteToErrorLog (string message, string exception)
        {
            using (StreamWriter w = File.AppendText($"{AppDomain.CurrentDomain.BaseDirectory}ErrorLog.txt"))
            {
                w.WriteLine("\r\n" + message + "\r\n" + exception);
            }

            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(exception, EventLogEntryType.Error, 999);
            }

            SendEmail($"{message} \r\n {exception}");
        }

        static void SendEmail (string message)
        {
            var responseMailConfiguration = GetReq(Resources.Setting.HostMail);
            var mailConfiguration = JObject.Parse(responseMailConfiguration);
            var enumerations = JsonConvert.DeserializeObject<List<Enumeration>>(mailConfiguration["Data"].ToString());

            var mailEnableSSL = enumerations.Single(e => e.Key.Equals("EnableSsl")).Value;
            var mailPort = enumerations.Single(e => e.Key.Equals("SmtpPort")).Value;
            var mailHost = enumerations.Single(e => e.Key.Equals("SmtpHost")).Value;
            var mailUserName = enumerations.Single(e => e.Key.Equals("UserName")).Value;
            var mailPassword = enumerations.Single(e => e.Key.Equals("Password")).Value;
            var mailFullName = enumerations.Single(e => e.Key.Equals("FullName")).Value;

            var receivers = ReadSetting().EmailReceivers.Split(',').ToList();

            foreach (var receiver in receivers)
            {
                MailMessage mail = new MailMessage(new MailAddress(mailUserName, mailFullName), new MailAddress(receiver, "Someone"))
                {
                    IsBodyHtml = true,
                    Subject = "Error LIA Attendance",
                    Body = message
                };
                try
                {
                    SmtpClient client = new SmtpClient()
                    {
                        Credentials = new NetworkCredential(mailUserName, mailPassword),
                        Host = mailHost,
                        Port = Convert.ToInt32(mailPort),
                        EnableSsl = Convert.ToBoolean(mailEnableSSL),
                    };
                    client.Send(mail);
                }
                catch (Exception ex)
                {
                    //throw new Exception(ex.Message);
                }
            }
        }
    }
}
