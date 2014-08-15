using System;

namespace TfsDiffReport
{
    public static class CommandLineArgsParser
    {
        public static Options Parse(string[] args)
        {
            Options options = new Options();

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "/u")
                    {
                        options.ServerUrl = args[i + 1].Trim('"');
                        i++;
                    }
                    else if (args[i] == "/v")
                    {
                        var versions = args[i + 1];
                        if (versions.IndexOf('~') > 0)
                        {
                            options.FirstChangeset = Convert.ToInt32(versions.Substring(0, versions.IndexOf('~')));
                            options.LastChangeset = Convert.ToInt32(versions.Substring(versions.IndexOf('~') + 1));
                        }
                        else
                        {
                            options.FirstChangeset = Convert.ToInt32(versions);
                            options.LastChangeset = options.FirstChangeset;
                        }
                        i++;
                    }
                    else if (args[i] == "/p")
                    {
                        options.Paths = args[i + 1].Trim('"').Split(';');
                        i++;
                    }
                    else if (args[i] == "/e")
                    {
                        options.Extensions = args[i + 1].Trim('"').Split(';');
                        i++;
                    }
                }
                return options;
            }
            catch (Exception)
            {
                throw new ArgumentException("Command line arguments are invalid.");
            }
        }
    }
}
