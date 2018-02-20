using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HatTrick.Spat;

namespace HatTrick.Spat.TestHarness
{
    class Program
    {
        #region internals
        private static System.Diagnostics.Stopwatch _sw;
        #endregion

        #region main
        static void Main(string[] args)
        {
            _sw = new System.Diagnostics.Stopwatch();

            TestTemplateEngineSimpleTags();
            TestTemplateEngineConditionsAndLoops();
            TestTemplateEngineLambdaExpressions();

            Console.WriteLine("processing complete, press [Enter] to exit");
            Console.ReadLine();
        }
        #endregion

        #region test template engine simple tags
        private static void TestTemplateEngineSimpleTags()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-spat-template-1.txt");

            var obj = new { DocTitle = "Title Goes Here", DocBody = "Document Body Goes Here" };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(obj);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"simple template execution avg ticks: {totalTicks / 10}");
        }
        #endregion

        #region test template engine conditions and loops
        private static void TestTemplateEngineConditionsAndLoops()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-spat-template-2.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                Dob = DateTime.Parse("1975-03-03"),
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new[] { "MCSE", "MCITP", "MCTS" },
                PreviousEmployers = default(object), //null
                SubContent = "Hi {FirstName} {LastName}, this is just a sub content merge test..."
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            ngin.SuppressNewline = true;
            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"conditions and loops template avg ticks: {totalTicks / 10}");
        }
        #endregion

        #region test template engine conditions and loops
        private static void TestTemplateEngineLambdaExpressions()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-spat-template-3.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                Dob = DateTime.Parse("1975-03-03"),
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new[] { "mcse", "mcitp", "mcts" },
                PreviousEmployers = default(object) //null
            };

            Func<DateTime, string> formatBirthDate = (date) =>
            {
                return date.ToString("yyyy-MM-dd");
            };

            Func<string, string> toUpper = (val) =>
            {
                return val.ToUpper();
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Add(nameof(formatBirthDate), formatBirthDate);
            ngin.LambdaRepo.Add(nameof(toUpper), toUpper);
            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"lambda expression template avg ticks: {totalTicks / 10}");
        }
        #endregion
    }
}
