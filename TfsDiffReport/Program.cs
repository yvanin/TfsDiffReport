using System;

namespace TfsDiffReport
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var options = CommandLineArgsParser.Parse(args);
                new DiffReportRunner(options).GenerateReport();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("TfsDiffReport failed. Exception: {0}", ex.Message);
            }

#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
