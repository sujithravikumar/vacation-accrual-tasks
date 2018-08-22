using System;
namespace vacation_accrual_tasks
{
    public class UserData
    {
        public int User_Id { get; set; }
        public bool Pay_Cycle_Regular { get; set; }
        public decimal Accrual { get; set; }
        public decimal Max_Balance { get; set; }
        public int Period { get; set; }
    }
}
