using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dapper;
using NLog;

namespace vacation_accrual_tasks
{
    public class ForecastVacationData
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public ForecastVacationData()
        {
            List<User> userList = GetUsers();

            foreach (User user in userList)
            {
                UserData userData = GetUserData(user.Id);

                DateTime currentPayPeriodStartDate =
                    GetCurrentPayPeriodStartDate(userData.Start_Date_Even_Ww);

                List<VacationData> vacationDataList = GetVacationData(user.Id,
                                    currentPayPeriodStartDate.AddDays(-14));

                // + 1 to account for the previous period extra row
                int numberOfRowsToInsert =
                        userData.Period + 1 > vacationDataList.Count ?
                                userData.Period + 1 - vacationDataList.Count : 0;

                DateTime lastStartDate = vacationDataList.Last().Start_Date;
                DateTime lastEndDate = vacationDataList.Last().End_Date;
                decimal lastBalance = vacationDataList.Last().Balance;

                decimal balance = vacationDataList.First().Balance;
                decimal forfeit = 0;
                decimal take = 0;

                // Update
                for (int i = 1; i < vacationDataList.Count; i++)
                {
                    forfeit = 0;
                    balance += vacationDataList[i].Accrual -
                        vacationDataList[i].Take;

                    if (balance > userData.Max_Balance)
                    {
                        forfeit = balance - userData.Max_Balance;
                        balance = userData.Max_Balance;
                    }

                    if (balance != vacationDataList[i].Balance || 
                       forfeit != vacationDataList[i].Forfeit)
                    {
                        UpdateVacationData(
                            user.Id,
                            vacationDataList[i].Start_Date,
                            vacationDataList[i].End_Date,
                            balance,
                            forfeit
                        );

                        if (i == vacationDataList.Count - 1)
                        {
                            lastBalance = balance;
                        }
                    }
                }

                // Insert
                for (int i = 1; i <= numberOfRowsToInsert; i++)
                {
                    forfeit = 0;
                    take = 0;
                    lastBalance += userData.Accrual;
                    lastStartDate = lastStartDate.AddDays(14);
                    lastEndDate = lastEndDate.AddDays(14);

                    if (lastBalance > userData.Max_Balance)
                    {
                        take = 8 * userData.Take_Days_Off;
                        lastBalance -= take;
                        if (lastBalance > userData.Max_Balance)
                        {
                            forfeit = lastBalance - userData.Max_Balance;
                            lastBalance = userData.Max_Balance;
                        }
                    }

                    InsertVacationData(user.Id, lastStartDate,
                                       lastEndDate, userData.Accrual,
                                       take, lastBalance, forfeit);
                }

            }
        }

        static DateTime GetCurrentPayPeriodStartDate(bool isPayCycleEvenWw)
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
                return isPayCycleEvenWw ? weekBegin : weekBegin.AddDays(-7);
            }
            else
            {
                return isPayCycleEvenWw ? weekBegin.AddDays(-7) : weekBegin;
            }
        }

        static List<User> GetUsers()
        {
            List<User> userList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = "SELECT \"Id\", \"Email\" FROM public.\"AspNetUsers\"";
                userList = conn.Query<User>(querySQL).ToList();
            }
            if (userList == null || userList.Count == 0)
            {
                string message = "The User List is empty";
                Console.WriteLine(message);
                logger.Error(message);
            }
            return userList;
        }

        static UserData GetUserData(string userId)
        {
            UserData userData;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = 
                    "SELECT * FROM public.user_data WHERE user_id = @userId";
                userData = conn.QuerySingle<UserData>(querySQL, new {userId});
            }

            if (userData == null)
            {
                string message = $"The User Data is empty for: {userId}";
                Console.WriteLine(message);
                logger.Error(message);
                Environment.Exit(0);
            }
            return userData;
        }

        static List<VacationData> GetVacationData(string userId, DateTime startDate)
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
                                        TO_DATE(@startDate, 'YYYY-MM-DD')
                            ORDER BY
                                start_date";
                vacationDataList = conn.Query<VacationData>(querySQL,
                    new {userId, 
                        startDate = startDate.ToString("yyyy-MM-dd")}).ToList();
            }
            if (vacationDataList == null || vacationDataList.Count == 0)
            {
                string message =
                    $"The Vacation Data is empty for: {userId} {startDate}";
                Console.WriteLine(message);
                logger.Error(message);
                Environment.Exit(0);
            }
            return vacationDataList;
        }

        static void UpdateVacationData(string userId, DateTime startDate,
            DateTime endDate, decimal balance, decimal forfeit)
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                var updateSQL = @"UPDATE
                                        public.vacation_data
                                    SET
                                        balance = @balance,
                                        forfeit = @forfeit
                                    WHERE
                                        user_id = @userId
                                        AND start_date = To_date(@startDate, 'YYYY-MM-DD')
                                        AND end_date = To_date(@endDate, 'YYYY-MM-DD')";
                var res = conn.Execute(updateSQL,
                    new {
                        userId,
                        startDate = startDate.ToString("yyyy-MM-dd"),
                        endDate = endDate.ToString("yyyy-MM-dd"),
                        balance,
                        forfeit
                        });
                if (res == 0)
                {
                    string message = 
                        $"Update failed for: {userId} {startDate} {endDate} {balance} {forfeit}";
                    Console.WriteLine(message);
                    logger.Error(message);
                }
                else
                {
                    string message = $"Update success for: {userId}";
                    Console.WriteLine(message);
                    logger.Info(message);
                }
            }
        }

        static void InsertVacationData(
            string userId, DateTime startDate, DateTime endDate, decimal accrual, 
            decimal take, decimal balance, decimal forfeit)
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                var insertSQL = @"INSERT INTO
                                        public.vacation_data
                                        (
                                            user_id,
                                            start_date,
                                            end_date,
                                            accrual,
                                            take,
                                            balance,
                                            forfeit
                                        )
                                    VALUES
                                        (
                                            @userId,
                                            TO_DATE(@startDate, 'YYYY-MM-DD'),
                                            TO_DATE(@endDate, 'YYYY-MM-DD'),
                                            @accrual,
                                            @take,
                                            @balance,
                                            @forfeit
                                        )";
                var res = conn.Execute(insertSQL, 
                   new {
                        userId,
                        startDate = startDate.ToString("yyyy-MM-dd"),
                        endDate = endDate.ToString("yyyy-MM-dd"),
                        accrual,
                        take,
                        balance,
                        forfeit 
                       }
                );
                if (res == 0)
                {
                    string message = 
                        $"Insert failed for: {userId} {startDate} {endDate} {accrual} {take} {balance} {forfeit}";
                    Console.WriteLine(message);
                    logger.Error(message);
                }
                else
                {
                    string message = $"Insert success for: {userId}";
                    Console.WriteLine(message);
                    logger.Info(message);
                }
            }
        }
    }
}
