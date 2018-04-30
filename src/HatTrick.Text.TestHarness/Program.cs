using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HatTrick.Text;

namespace HatTrick.Text.TestHarness
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
            TestTemplateLastCharacterIsATag();
            TestTemplateEngineWhitespaceFormatting();

            TestTruthy();

            Console.WriteLine("processing complete, press [Enter] to exit");
            Console.ReadLine();
        }
        #endregion

        #region test template engine simple tags
        private static void TestTemplateEngineSimpleTags()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-template-1.txt");

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
            Console.WriteLine($"simple template execution avg ticks: {totalTicks / 100}");
        }
        #endregion

        #region test template engine conditions and loops
        private static void TestTemplateEngineConditionsAndLoops()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-template-2.txt");

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
            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"conditions and loops template avg ticks: {totalTicks / 100}");
        }
        #endregion

        #region test template engine lambda expressions
        private static void TestTemplateEngineLambdaExpressions()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-template-3.txt");

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

            Func<string, string> resolvePartial = (key) =>
            {
                return "..xx..xx..xx..{FirstName} {LastName}xx..xx..xx..xx..xx..";
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
            ngin.LambdaRepo.Add(nameof(resolvePartial), resolvePartial);
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
            Console.WriteLine($"lambda expression template avg ticks: {totalTicks / 100}");
        }
        #endregion

        #region test template last character is a tag
        private static void TestTemplateLastCharacterIsATag()
        {

            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-template-4.txt");

            var obj = new
            {
                Title = "Title Goes Here",
                FirstName = "Jerry",
                LastName = "Smith",
                Services = new[] { new { Name = "ABC" }, new { Name = "XYZ" } },
                IsGoldMember = true,
                Supervisor = "Yours Truly"
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);

            Func<dynamic, string> GetFullName = (o) =>
            {
                return $"{o.FirstName} {o.LastName}";
            };

            ngin.LambdaRepo.Add(nameof(GetFullName), GetFullName);

            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(obj);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"last char is tag template execution avg ticks: {totalTicks / 100}");
        }
        #endregion

        #region test template engine whitespace formatting
        private static void TestTemplateEngineWhitespaceFormatting()
        {
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\test-template-a.txt");

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
            long totalTicks = 0;
            for (int i = 0; i < 100; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"template whitespace formatting execution avg ticks: {totalTicks / 100}");
        }
        #endregion

        #region test truthy
        private static void TestTruthy()
        {
            TemplateEngine ngin = new TemplateEngine("");

            bool isTrue0 = ngin.IsTrue(null);
            bool isTrue1 = ngin.IsTrue(1.00F);
            bool isTrue2 = ngin.IsTrue(1U);
            bool isTrue3 = ngin.IsTrue(0.00F);
            bool isTrue4 = ngin.IsTrue(0);
            bool isTrue5 = ngin.IsTrue(string.Empty);
            bool isTrue6 = ngin.IsTrue(new object[0]);
            bool isTrue7 = ngin.IsTrue(new object[1]);
            bool isTrue8 = ngin.IsTrue(true);
            bool isTrue9 = ngin.IsTrue(false);
            bool isTrue10 = ngin.IsTrue('\0');
            bool isTrue11 = ngin.IsTrue('t');
            bool isTrue12 = ngin.IsTrue('f');
            bool isTrue14 = ngin.IsTrue((decimal)1.111);
            bool isTrue15 = ngin.IsTrue((decimal)0.000);
            bool isTrue16 = ngin.IsTrue("\0");
            bool isTrue17 = ngin.IsTrue("f");
            bool isTrue18 = ngin.IsTrue("t");
            bool isTrue19 = ngin.IsTrue("false");
            bool isTrue20 = ngin.IsTrue("hello");
        }
        #endregion
    }
}
