using System;
namespace vacation_accrual_tasks
{
    public class UserData
    {
        public int User_Id { get; set; }
        public bool Is_Pay_Cycle_Even_Ww { get; set; }
        public decimal Accrual { get; set; }
        public decimal Max_Balance { get; set; }
        public int Period { get; set; }
    }
}
