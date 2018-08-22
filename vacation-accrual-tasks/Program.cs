using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using NLog;
using Npgsql;

namespace vacation_accrual_tasks
{
    class Program
    {
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
            //FIXME
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
                        Console.WriteLine("Invalid arguments were passed");
                        break;
                    }

            }
        }
    }
}
