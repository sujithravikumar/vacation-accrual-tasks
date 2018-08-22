using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace vacation_accrual_tasks
{
    public class ForecastVacationData
    {
        public ForecastVacationData()
        {
            Console.WriteLine("Hello from Forecast Vacation");

            IList<UserData> userDataList = GetUserData();

            if (userDataList.Count > 0)
            {
                foreach (var item in userDataList)
                {
                    Console.WriteLine($"{item.User_Id}\t{item.Pay_Cycle_Regular}" +
                                      $"\t{item.Accrual}\t{item.Max_Balance}" +
                                      $"\t{item.Period}");
                }
            }
            else
            {
                Console.WriteLine("The User Data table is empty!");
            }
        }

        static IList<UserData> GetUserData()
        {
            IList<UserData> userDataList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                var querySQL = "SELECT * FROM public.static_data";
                userDataList = conn.Query<UserData>(querySQL).ToList();
            }
            return userDataList;
        }
    }
}
