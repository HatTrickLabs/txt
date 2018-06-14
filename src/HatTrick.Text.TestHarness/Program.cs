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
        //basic tags
        //conditional blocks
        //each blocks
        //whitespace control, escaping { and }
        //chars and comments
        //lambda expressions
        //partials(sub templates)
        static void Main(string[] args)
        {
            _sw = new System.Diagnostics.Stopwatch();

            //TestTruthy();
            //TestSimpleTags();
            //TestConditionalBlocks();
            TestEachBlocks();
            //TestWhitespaceControl();
            //TestLambdaExpressions();
            //TestPartials();
            //TestComplexConditions();
            //TestAbsoluteChaos();


            Console.WriteLine("processing complete, press [Enter] to exit");
            Console.ReadLine();
        }
        #endregion

        #region test simple tags
        private static void TestSimpleTags()
        {
            //this template demonstrates the most basic template tags (simple text injection)...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\simple-tags.txt");

            var obj = new { FirstName = "Jerrod", LastName = "Eiman", Title = "Code Monkey" };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(obj);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"simple template execution avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test conditional blocks
        private static void TestConditionalBlocks()
        {
            //this template demostrates basic conditional blocks {#if } and the negated {#if !}
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\conditional-blocks.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                IsEmployed = true,
                Employer = "Microsoft",
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"conditional block template avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test each blocks
        private static void TestEachBlocks()
        {
            //this template demonstrates block iteration via the {#each } tag...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\each-blocks.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new[] { "mcse", "mcitp", "mcts" },
                Localities = new[]
                {
                    new { City = "Dallas", State = "TX" },
                    new { City = "Plano", State = "TX" },
                    new { City = "Durango", State = "CO" },
                }
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register("Test", () => "{FirstName} {LastName}");

            ngin.ProgressListener += (l, t) => { Console.WriteLine($"Progress: @ {l}\t{t}"); };

            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"each block template avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test whitespace control
        private static void TestWhitespaceControl()
        {
            //this template demonstrates whitespace control and escaping of { and } chars...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\whitespace-control.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new[] { "MCSE", "MCITP", "MCTS" },
                Localities = new[]
                {
                    new { City = "Dallas", State = "TX" },
                    new { City = "Plano", State = "TX" },
                    new { City = "Durango", State = "CO" },
                },
                PreviousEmployers = default(object), //null,
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }
            Console.WriteLine($"whitespace formatting template execution avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test lambda expressions
        public static void TestLambdaExpressions()
        {
            //this template demonstrates delegation back into code via lambda expressions...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\lambda-expressions.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                CurrentAddress = new
                {
                    Line1 = "111 Main St.",
                    Line2 = "Suite 211",
                    City = "Dallas",
                    State = "TX",
                    Zip = "75201"
                },
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new string[] { "MCSE", "MCITP", "MCTS" },
                Localities = new[]
                {
                    new { City = "Dallas", State = "TX" },
                    new { City = "Plano", State = "TX" },
                    new { City = "Durango", State = "CO" },
                    new { City = "Portland", State = "OR" },
                    new { City = "Salt Lake City", State = "Utah" },
                    new { City = "Austin", State = "TX" },
                },
            };

            //anon func to return a formatted address...
            Func<dynamic, string> formatAddress = (address) =>
            {
                string nl = Environment.NewLine;
                string formatted = $"{address.Line1}{nl}{address.Line2}{nl}{address.City}, {address.State} {address.Zip}";
                return formatted;
            };

            //anon func to join a collection of strings...
            Func<string[], string, string> join = (values, separator) =>
            {
                string s = string.Join(separator, values);
                return s;
            };

            bool cssToggle = false;
            Func<string> getRowCssClass = () =>
            {
                cssToggle = !cssToggle;
                return cssToggle ? "light" : "dark";
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            //register lambdas...
            ngin.LambdaRepo.Register(nameof(formatAddress), formatAddress);
            ngin.LambdaRepo.Register(nameof(join), join);
            ngin.LambdaRepo.Register(nameof(getRowCssClass), getRowCssClass);

            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }

            Console.WriteLine($"lambda expression template execution avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test partials
        public static void TestPartials()
        {
            //this template demonstrates delegation back into code via lambda expressions...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\partials.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                CurrentAddress = new
                {
                    Line1 = "111 Main St.",
                    Line2 = "Suite 211",
                    City = "Dallas",
                    State = "TX",
                    Zip = "75201"
                },
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new string[] { "MCSE", "MCITP", "MCTS" },
                Localities = new[]
                {
                    new { City = "Dallas", State = "TX" },
                    new { City = "Plano", State = "TX" },
                    new { City = "Durango", State = "CO" },
                    new { City = "Portland", State = "OR" },
                    new { City = "Salt Lake City", State = "Utah" },
                    new { City = "Austin", State = "TX" },
                },
                CertsListTemplate = "<ul>{#each Certifications}<li>{$}</li>{/each}</ul>",
                LocalitiesTemplate = File.ReadAllText(@"..\..\..\..\sample-templates\partials-chunk.txt")
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);

            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }

            Console.WriteLine($"partials template execution avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test template complex conditions
        private static void TestComplexConditions()
        {
            //see test Truthy below...  Also conditions can be driven by bool, enumerable, null/not null, and lambda results...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\complex-conditions.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                NameSuffix = default(object),
                Dob = DateTime.Parse("1975-03-03"),
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new[] { "mcse", "mcitp", "mcts" },
                PreviousEmployers = new string[0], //null
                HasTransportation = true,
            };


            TemplateEngine ngin = new TemplateEngine(template);

            bool isAARPMember = false;
            Func<bool> isRetired = () => { return isAARPMember; };
            ngin.LambdaRepo.Register(nameof(isRetired), isRetired);

            Func<string> getObjective = () =>
            {
                return "I want a dev job with massive salary.";
            };
            ngin.LambdaRepo.Register(nameof(getObjective), getObjective);

            string result;
            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }

            Console.WriteLine($"complex conditions template execution avg ticks: {totalTicks / 25}");
        }
        #endregion

        #region test absolute chaos
        private static void TestAbsoluteChaos()
        {
            //this template demonstrates delegation back into code via lambda expressions...
            string template = File.ReadAllText(@"..\..\..\..\sample-templates\absolute-chaos.txt");

            var person = new
            {
                FirstName = "James",
                LastName = "Doe",
                CurrentAddress = new
                {
                    Line1 = "111 Main St.",
                    Line2 = "Suite 211",
                    City = "Dallas",
                    State = "TX",
                    Zip = "75201"
                },
                IsEmployed = true,
                Employer = "Microsoft",
                Certifications = new string[] { "MCSE", "MCITP", "MCTS" },
                Localities = new[]
                {
                    new { City = "Dallas", State = "TX" },
                    new { City = "Plano", State = "TX" },
                    new { City = "Durango", State = "CO" },
                    new { City = "Portland", State = "OR" },
                    new { City = "Salt Lake City", State = "Utah" },
                    new { City = "Austin", State = "TX" },
                },
                CertsListTemplate = "<ul>{#each Certifications}<li>{$}</li>{/each}</ul>",
                LocalitiesTemplate = File.ReadAllText(@"..\..\..\..\sample-templates\partials-chunk.txt"),
                ExcitedCityTemplate = "-> {$.City} !!!"
            };

            Func<Array, string, bool> workedIn = (theLocalities, city) =>
            {
                foreach (var local in theLocalities)
                {
                    if (string.Compare(((dynamic)local).City, city, true) == 0)
                    {
                        return true;
                    }
                }
                return false;
            };

            Func<string, bool> isPlano = (city) =>
            {
                return string.Compare(city, "plano", true) == 0;
            };

            string result;
            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(workedIn), workedIn);
            ngin.LambdaRepo.Register(nameof(isPlano), isPlano);

            long totalTicks = 0;
            for (int i = 0; i < 25; i++)
            {
                _sw.Restart();
                result = ngin.Merge(person);
                _sw.Stop();
                totalTicks += _sw.ElapsedTicks;
            }

            
Console.WriteLine($"absolute chaos execution avg ticks: {totalTicks / 25}");
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
