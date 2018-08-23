using System;
namespace vacation_accrual_tasks
{
    public class VacationData
    {
        public int Id { get; set; }
        public int User_Id { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public decimal Accrual { get; set; }
        public decimal Take { get; set; }
        public decimal Balance { get; set; }
        public decimal Forefeit { get; set; }
    }
}
