namespace TfsDiffReport
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var options = CommandLineArgsParser.Parse(args);
            new DiffReportRunner(options).GenerateReport();
        }
    }
}
