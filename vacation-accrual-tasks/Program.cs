using System;
using NLog;

namespace vacation_accrual_tasks
{
    class Program
    {
        static void Main(string[] args)
        {
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
