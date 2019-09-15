using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiaAttendanceSchedule.Info
{
    class AttendanceLogModel
    {
        public int IndRegID { get; set; }
        public string DateTimeRecord { get; set; }
        public string DateTimeAttendance
        {
            get { return DateTime.Parse(DateTimeRecord).ToString("dd-MM-yyyy-H-mm-ss"); }
        }
        public bool Status { get; set; }
    }
}
