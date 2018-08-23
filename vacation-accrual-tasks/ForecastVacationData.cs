using System;
using System.Collections.Generic;
using System.Globalization;
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

            if (userDataList.Count == 0)
            {
                Console.WriteLine("The User Data is empty!");
                return;
            }

            // For each user
            foreach (UserData ud in userDataList)
            {
                DateTime currentPayPeriodStartDate =
                    GetCurrentPayPeriodStartDate(ud.Pay_Cycle_Regular);

                IList<VacationData> vacationDataList =
                    GetVacationData(ud.User_Id,
                                    currentPayPeriodStartDate.AddDays(-14));

                if(vacationDataList.Count == 0)
                {
                    Console.WriteLine("The Vacation Data is empty!");
                    return;
                }

                foreach (VacationData vd in vacationDataList)
                {
                    Console.WriteLine($"{vd.Id}\t{vd.User_Id}\t" +
                                      $"{vd.Start_Date}\t{vd.End_Date}\t" +
                                      $"{vd.Accrual}\t{vd.Take}\t" +
                                      $"{vd.Balance}\t{vd.Forefeit}");
                }
            }

        }

        static IList<UserData> GetUserData()
        {
            IList<UserData> userDataList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = "SELECT * FROM public.static_data";
                userDataList = conn.Query<UserData>(querySQL).ToList();
            }
            return userDataList;
        }

        static DateTime GetCurrentPayPeriodStartDate(bool isRegularPayCycle)
        {
            DateTime now = DateTime.Now;
            int diff = DayOfWeek.Sunday - now.DayOfWeek;
            DateTime weekBegin = now.AddDays(diff);

            GregorianCalendar calendar = new GregorianCalendar();
            int weekNumber = calendar.GetWeekOfYear(weekBegin, 
                                CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
            int biweeklyKey = weekNumber % 2;

            if (biweeklyKey == 0)
            {
                return isRegularPayCycle ? weekBegin : weekBegin.AddDays(-7);
            }
            else
            {
                return isRegularPayCycle ? weekBegin.AddDays(-7) : weekBegin;
            }
        }

        static IList<VacationData> GetVacationData(int userId, DateTime startDate)
        {
            IList<VacationData> vacationDataList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = @"
                            SELECT
                                *
                            FROM
                                public.vacation_data
                            WHERE
                                user_id = @userId
                                AND start_date >= 
                                        to_date(@startDate, 'YYYY-MM-DD')";                
                vacationDataList = conn.Query<VacationData>(querySQL,
                    new {userId, 
                        startDate = startDate.ToString("yyyy-MM-dd")}).ToList();
            }
            return vacationDataList;
        }
    }
}
