using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using NLog;
using Npgsql;

namespace vacation_accrual_tasks
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public static string _connStr { get; private set; }
        public static string Smtp_Host { get; private set; }
        public static int Smtp_Port { get; private set; }
        public static string Smtp_Username { get; private set; }
        public static string Smtp_Password { get; private set; }

        public static IDbConnection OpenConnection(string connStr)
        {
            var conn = new NpgsqlConnection(connStr);
            conn.Open();
            return conn;
        }
        
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments were passed");
                Environment.Exit(0);
            }

            IConfigurationRoot Configuration = new ConfigurationBuilder()
                                .AddUserSecrets<Program>().Build();
            _connStr = Configuration["ConnectionString"];
            Smtp_Host = Configuration["Smtp_Host"];
            Smtp_Port = Convert.ToInt32(Configuration["Smtp_Port"]);
            Smtp_Username = Configuration["Smtp_Username"];
            Smtp_Password = Configuration["Smtp_Password"];


            switch (args[0].ToLower())
            {
                case "forecastvacationdata":
                    {
                        Forecast.ForecastVacationData();
                        Email.SendEmails();
                        break;
                    }
                default:
                    {
                        string message = $"Invalid arguments were passed: {args[0]}";
                        Console.WriteLine(message);
                        logger.Error(message);
                        break;
                    }
            }
        }
    }
}
