using System;
namespace vacation_accrual_tasks
{
    public class VacationData
    {
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public decimal Accrual { get; set; }
        public int Take { get; set; }
        public decimal Balance { get; set; }
        public decimal Forfeit { get; set; }
    }
}
