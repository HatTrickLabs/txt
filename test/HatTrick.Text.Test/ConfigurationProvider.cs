using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HatTrick.Text.Test
{
    public static class ConfigurationProvider
    {
        public static IConfiguration Configuration { get; private set; }
        public static string InputPath { get; private set; }
        public static string OutputPath { get; private set; }

        static ConfigurationProvider()
        {
            Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .Build();

            InputPath = Path.Combine(GetCurrentPath(), "..\\", Configuration.GetValue("templatePath", string.Empty));
            OutputPath = Path.Combine(GetCurrentPath(), "..\\", Configuration.GetValue("expectedResultsPath", string.Empty));
        }

        private static string GetCurrentPath()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            var appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }
    }
}
