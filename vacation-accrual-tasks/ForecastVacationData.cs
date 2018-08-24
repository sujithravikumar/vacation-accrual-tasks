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

            List<User> userList = GetUsers();

            foreach (User user in userList)
            {
                // FIXME 
                // temporarily enabled business logic only for one user
                if (user.Id != 1)
                {
                    continue;
                }

                UserData userData = GetUserData(user.Id);

                if (userData == null)
                {
                    Console.WriteLine("The User Data is empty!");
                    return;
                }

                DateTime currentPayPeriodStartDate =
                    GetCurrentPayPeriodStartDate(userData.Pay_Cycle_Regular);

                List<VacationData> vacationDataList =
                    GetVacationData(user.Id,
                                    currentPayPeriodStartDate.AddDays(-14));

                if (vacationDataList == null || vacationDataList.Count == 0)
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

        static List<User> GetUsers()
        {
            List<User> userList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = "SELECT * FROM public.user";
                userList = conn.Query<User>(querySQL).ToList();
            }
            return userList;
        }

        static UserData GetUserData(int userId)
        {
            List<UserData> userDataList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = 
                    "SELECT * FROM public.user_data WHERE user_id = @userId";
                userDataList = conn.Query<UserData>(querySQL, new {userId}).ToList();
            }

            if(userDataList == null || userDataList.Count == 0)
            {
                return null;
            }
            // user_id is unique on database so it should return only one row
            return userDataList[0];
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

        static List<VacationData> GetVacationData(int userId, DateTime startDate)
        {
            List<VacationData> vacationDataList;

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
