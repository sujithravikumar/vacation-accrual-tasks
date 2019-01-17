using System;
namespace vacation_accrual_tasks
{
    public class UserEmailData
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool Start_Date_Even_Ww { get; set; }
        public int Email_Alert_Days_Before { get; set; }
    }
}
