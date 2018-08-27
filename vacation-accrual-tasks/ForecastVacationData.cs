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
                // FIXME 
                // temporarily enabled business logic only for one user
                if (user.Id != 1)
                {
                    continue;
                }

                UserData userData = GetUserData(user.Id);

                DateTime currentPayPeriodStartDate =
                    GetCurrentPayPeriodStartDate(userData.Is_Pay_Cycle_Even_Ww);

                List<VacationData> vacationDataList = GetVacationData(user.Id,
                                    currentPayPeriodStartDate.AddDays(-14));

                int numberOfRowsToInsert =
                        userData.Period > vacationDataList.Count ?
                                userData.Period - vacationDataList.Count : 0;

                DateTime lastStartDate = vacationDataList.Last().Start_Date;
                DateTime lastEndDate = vacationDataList.Last().End_Date;
                decimal lastBalance = vacationDataList.Last().Balance;
                decimal lastForfeit = vacationDataList.Last().Forefeit;

                decimal balance = vacationDataList.First().Balance;
                decimal forfeit = 0;

                // Update
                for (int i = 1; i < vacationDataList.Count; i++)
                {
                    balance += vacationDataList[i].Accrual -
                        vacationDataList[i].Take;

                    forfeit += balance > userData.Max_Balance ?
                                          balance - userData.Max_Balance : 0;

                    if (balance != vacationDataList[i].Balance || 
                       forfeit != vacationDataList[i].Forefeit)
                    {
                        UpdateVacationData(
                            vacationDataList[i].Id,
                            balance,
                            forfeit
                        );

                        if (i == vacationDataList.Count - 1)
                        {
                            lastBalance = balance;
                            lastForfeit = forfeit;
                        }
                    }
                }

                // Insert
                for (int i = 1; i <= numberOfRowsToInsert; i++)
                {
                    decimal take = 0;
                    lastBalance += userData.Accrual;
                    lastStartDate = lastStartDate.AddDays(14);
                    lastEndDate = lastEndDate.AddDays(14);

                    if (lastBalance > userData.Max_Balance)
                    {
                        take = 8;
                        lastBalance -= take;
                        if (lastBalance > userData.Max_Balance)
                        {
                            lastForfeit += lastBalance - userData.Max_Balance;
                            lastBalance = userData.Max_Balance;
                        }
                    }

                    InsertVacationData(user.Id, lastStartDate,
                                       lastEndDate, userData.Accrual,
                                       take, lastBalance, lastForfeit);
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
                string querySQL = "SELECT * FROM public.user";
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

        static UserData GetUserData(int userId)
        {
            List<UserData> userDataList;

            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = 
                    "SELECT * FROM public.user_data WHERE user_id = @userId";
                userDataList = conn.Query<UserData>(querySQL, new {userId}).ToList();
            }

            if (userDataList == null || userDataList.Count == 0)
            {
                return null;
            }

            if (userDataList[0] == null)
            {
                string message = $"The User Data is empty for: {userId}";
                Console.WriteLine(message);
                logger.Error(message);
            }
            // user_id is unique on database so it should return only one row
            return userDataList[0];
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
                                        TO_DATE(@startDate, 'YYYY-MM-DD')
                            ORDER BY
                                id";
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
            }
            return vacationDataList;
        }

        static void UpdateVacationData(int id, decimal balance, decimal forfeit)
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                var updateSQL = @"UPDATE
                                        public.vacation_data
                                    SET
                                        balance = @balance,
                                        forfeit = @forfeit
                                    WHERE
                                        id = @id";
                var res = conn.Execute(updateSQL, new {id, balance, forfeit});
                if (res == 0)
                {
                    string message = 
                        $"Update failed for: {id} {balance} {forfeit}";
                    Console.WriteLine(message);
                    logger.Error(message);
                }
                else
                {
                    string message = $"Update success for: {id}";
                    Console.WriteLine(message);
                    logger.Info(message);
                }
            }
        }

        static void InsertVacationData(
            int userId, DateTime startDate, DateTime endDate, decimal accrual, 
            decimal take, decimal balance, decimal forfeit)
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                var insertSQL = @"INSERT INTO
                                        public.vacation_data
                                        (
                                            id,
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
                                            nextval('vacation_data_id_seq'),
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
