using System;
using System.Data;
using NLog;
using Npgsql;

namespace vacation_accrual_tasks
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public static string _connStr = @"Server=localhost;Port=5432;
    Database=vacation_accrual;User Id=pguser;Password=pguser0;";

        public static IDbConnection OpenConnection(string connStr)
        {
            var conn = new NpgsqlConnection(connStr);
            conn.Open();
            return conn;
        }
        
        static void Main(string[] args)
        {
            // FIXME
            // temporarily overriding the argument array
            args = new string[] { "forecastvacationdata" };

            if (args.Length == 0)
            {
                Console.WriteLine("No arguments were passed");
                Environment.Exit(0);
            }
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
