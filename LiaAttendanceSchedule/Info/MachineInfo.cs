using System;

namespace LiaAttendanceSchedule.Info
{
    class MachineInfo
    {
        public int No { get; set; }
        public int RegID { get; set; }
        public string DateTimeRecord { get; set; }
        public DateTime DateOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("yyyy-MM-dd")); }
        }
        public DateTime TimeOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("hh:mm:ss tt")); }
        }
        public string DateTimeAttendance
        {
            get { return DateTime.Parse(DateTimeRecord).ToString("dd-MM-yyyy-H-mm-ss"); }
        }
    }
}
