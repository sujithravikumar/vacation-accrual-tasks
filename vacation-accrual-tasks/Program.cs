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


            switch (args[0].ToLower())
            {
                case "forecastvacationdata":
                    {
                        ForecastVacationData fv = new ForecastVacationData();
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
