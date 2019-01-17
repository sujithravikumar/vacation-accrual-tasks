using System;
namespace vacation_accrual_tasks
{
    public class UserData
    {
        public bool Start_Date_Even_Ww { get; set; }
        public decimal Accrual { get; set; }
        public int Max_Balance { get; set; }
        public int Period { get; set; }
        public decimal Take_Days_Off { get; set; }
    }
}
