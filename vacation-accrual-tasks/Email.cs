using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NLog;

namespace vacation_accrual_tasks
{
    public static class Email
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public static void SendEmails()
        {
            List<UserEmailData> userEmailDataList = GetEmailAlertEnabledUsers();

            logger.Info($"Email alert enabled users count: {userEmailDataList.Count}");

            if (userEmailDataList.Count == 0)
            {
                return;
            }

            foreach (UserEmailData userEmailData in userEmailDataList)
            {
                DateTime currentPayPeriodStartDate =
                    Forecast.GetCurrentPayPeriodStartDate(userEmailData.Start_Date_Even_Ww);

                int daysBeforeEndDate = 
                    (currentPayPeriodStartDate.AddDays(13) - DateTime.Today).Days;

                if (daysBeforeEndDate != userEmailData.Email_Alert_Days_Before)
                {
                    continue;
                }

                VacationData vacationData = GetVacationData(
                                                userEmailData.Id,
                                                currentPayPeriodStartDate,
                                                currentPayPeriodStartDate.AddDays(13));

                if (vacationData == null || vacationData.Take == 0)
                {
                    continue;
                }

                EmailSender.SendEmail(
                    userEmailData.Email,
                    "Reminder to take vacation",
                    $@"Hello {userEmailData.Email}!<br><br>
                    According to our records, you're due to take <span style=""font-size: 2em;"">{vacationData.Take}</span> hours of vacation during the current pay period,
                    {currentPayPeriodStartDate.ToString("yyyy-MM-dd")} to {currentPayPeriodStartDate.AddDays(13).ToString("yyyy-MM-dd")}.<br><br>" +
                    @"Please <a href='https://vacation-accrual-buddy.azurewebsites.net/Home/Preferences' target='_blank'>click here</a> to update your email alert preferences.<br><br>
                    If you have any additional questions, 
                    please contact Vacation Accrual Buddy at <a href='mailto:vacation.accrual.buddy@gmail.com' target='_blank'>vacation.accrual.buddy@gmail.com</a>."
                );

                string message = $"Sent reminder email to {userEmailData.Email}";
                Console.WriteLine(message);
                logger.Info(message);
            }
        }

        static List<UserEmailData> GetEmailAlertEnabledUsers()
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = @"SELECT
                                        u.""Id"",
                                        u.""Email"",
                                        ud.start_date_even_ww,
                                        ud.email_alert_days_before
                                    FROM
                                        public.""AspNetUsers"" u,
                                        public.user_data ud
                                    WHERE
                                        u.""Id"" = ud.user_id
                                        AND ud.email_alert_enabled = true";
                return conn.Query<UserEmailData>(querySQL).ToList();
            }
        }

        static VacationData GetVacationData(string userId, 
            DateTime startDate, DateTime endDate)
        {
            using (var conn = Program.OpenConnection(Program._connStr))
            {
                string querySQL = @"
                            SELECT
                                *
                            FROM
                                public.vacation_data
                            WHERE
                                user_id = @userId
                                AND start_date = 
                                        TO_DATE(@startDate, 'YYYY-MM-DD')
                                AND end_date = 
                                        TO_DATE(@endDate, 'YYYY-MM-DD')";
                return conn.QuerySingle<VacationData>(querySQL,
                    new
                    {
                        userId,
                        startDate = startDate.ToString("yyyy-MM-dd"),
                        endDate = endDate.ToString("yyyy-MM-dd")
                    });
            }
        }
    }
}
