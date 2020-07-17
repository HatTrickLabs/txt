﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HatTrick.Text;

namespace HatTrick.Text.Templating.TestHarness
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
            _sw.Start();

            SimpleTags();
            BracketEscaping();
            ComplexBindExpressions();
            BindObjectSupport();
            ThrowOnNoItemExists();
            TruthyFalsey();
            SimpleConditionalBlocks();
            NegatedConditionalBlocks();
            SimpleWhitespaceControl();
            GlobalWhitespaceControl();
            ComplexWhitespaceControl();
            SimpleIterationBlocks();
            ThrowOnNonIEnumerableIterationTarget();
            WalkingTheScopeChain();
            SimplePartialBlocks();
            SimpleTemplateComments();
            MultiLineTemplateComments();
            SimpleLambdaExpressions();
            ComplexLambdaExpressions();
            LambdaExpressionDrivenBlocks();
            WithTagScopeChangeBlocks();
            CodeGen();

            _sw.Stop();
            Console.WriteLine($"processing completed @ {_sw.ElapsedMilliseconds} milliseconds, press [Enter] to exit");
            Console.ReadLine();
        }
        #endregion

        #region get file
        static string GetFile(string path)
        {
            byte[] buffer = null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
            }
            string content = Encoding.UTF8.GetString(buffer);

            return content;
        }
        #endregion

        #region resolve template input
        static string ResolveTemplateInput(string name)
        {
            string path = AssemblePath(name, "in");
            return GetFile(path);
        }
        #endregion

        #region resolve template output
        static string ResolveTemplateOutput(string name)
        {
            string path = AssemblePath(name, "out");
            return GetFile(path);
        }
        #endregion

        #region assemble path
        static string AssemblePath(string name, string context)
        {
            char dsc = Path.DirectorySeparatorChar;
            string path = $"..{dsc}..{dsc}..{dsc}..{dsc}sample-templates{dsc}{name}-{context}.txt";
            return path;
        }
        #endregion

        #region output result
        static void RenderOutput(string name, bool passed, string context = null)
        {
            Console.WriteLine(string.Format("{0,-45} {1,-10} {2,-20}", name, passed ? "Success" : "Failed", context));
        }
        #endregion

        #region simple tags
        static void SimpleTags()
        {
            string name = "simple-tags";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                First = "Charlie",
                Last = "Brown"
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region bracket escaping
        static void BracketEscaping()
        {
            string name = "bracket-escaping";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = "Charlie Brown",
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region complex bind expressions
        static void ComplexBindExpressions()
        {
            string name = "complex-bind-expressions";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                Address = new
                {
                    Line1 = "123 Main St.",
                    Line2 = "Suite 200",
                    City = "Dallas",
                    State = "TX",
                    Zip = "77777"
                }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region bind object support
        static void BindObjectSupport()
        {
            string name = "bind-object-support";
            string template = ResolveTemplateInput(name);

            string expected = ResolveTemplateOutput(name);

            TemplateEngine ngin = new TemplateEngine(template);

            /************ Anonymous ************/
            var data = new
            {
                Age = 25,
                Name = new { First = "Charlie", Last = "Brown" },
                Address = new
                {
                    Line1 = "123 Main St.",
                    Line2 = "Suite 200",
                    City = "Dallas",
                    State = "TX",
                    Zip = "77777"
                }
            };

            string result1 = ngin.Merge(data);
            bool passed = string.Compare(result1, expected, false) == 0;
            RenderOutput(name, passed, "Anonymous");


            /************ POCO ************/
            Person data2 = new Person()
            {
                Age = 25,
                Name = new Name() { First = "Charlie", Last = "Brown" },
                Address = new Address()
                {
                    Line1 = "123 Main St.",
                    Line2 = "Suite 200",
                    City = "Dallas",
                    State = "TX",
                    Zip = "77777"
                }
            };

            string result2 = ngin.Merge(data2);

            bool passed2 = string.Compare(result2, expected, false) == 0;
            RenderOutput(name, passed2, "POCO");


            /************ Dictionary ************/
            Dictionary<string, object> data3 = new Dictionary<string, object>()
            {
                { "Age", 25 },
                { "Name", new Dictionary<string, string> { { "First", "Charlie" }, { "Last", "Brown" } } },
                { "Address", new Dictionary<string, string> {
                    { "Line1", "123 Main St." },
                    { "Line2", "Suite 200" },
                    { "City", "Dallas" },
                    { "State", "TX" },
                    { "Zip", "77777" },
                    }
                }
            };

            string result3 = ngin.Merge(data3);

            bool passed3 = string.Compare(result3, expected, false) == 0;
            RenderOutput(name, passed2, "Dictionary");
        }
        #endregion

        #region throw on no item exists
        static void ThrowOnNoItemExists()
        {
            string name = "throw-on-no-item-exists";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                First = "First",
                Second = "Second",
                Third = "Third"
            };

            bool passed = false;
            TemplateEngine ngin = new TemplateEngine(template);
            try
            {
                string result = ngin.Merge(data);
            }
            catch (Exception npe)
            {
                passed = true;
            }

            //string expected = ResolveTemplateOutput(name);

            RenderOutput(name, passed);
        }
        #endregion

        #region truthy falsey
        private static void TruthyFalsey()
        {
            string name = "truthy-falsey";
            string template = ResolveTemplateInput(name);
            TemplateEngine ngin = new TemplateEngine(template);

            bool[] isTrue = new bool[20];
            isTrue[0] = ngin.IsTrue(null) == false;
            isTrue[1] = ngin.IsTrue(1.00F) == true;
            isTrue[2] = ngin.IsTrue(1U) == true;
            isTrue[3] = ngin.IsTrue(0.00F) == false;
            isTrue[4] = ngin.IsTrue(0) == false;
            isTrue[5] = ngin.IsTrue(string.Empty) == false;
            isTrue[6] = ngin.IsTrue(new object[0]) == false;
            isTrue[7] = ngin.IsTrue(new object[1]) == true;
            isTrue[8] = ngin.IsTrue(true) == true;
            isTrue[9] = ngin.IsTrue(false) == false;
            isTrue[10] = ngin.IsTrue('\0') == false;
            isTrue[11] = ngin.IsTrue('t') == true;
            isTrue[12] = ngin.IsTrue('f') == true;
            isTrue[13] = ngin.IsTrue((decimal)1.111) == true;
            isTrue[14] = ngin.IsTrue((decimal)0.000) == false;
            isTrue[15] = ngin.IsTrue("\0") == false;
            isTrue[16] = ngin.IsTrue("f") == true;
            isTrue[17] = ngin.IsTrue("t") == true;
            isTrue[18] = ngin.IsTrue("false") == true;
            isTrue[19] = ngin.IsTrue("hello") == true;

            int idx = 0;
            Func<int> GetNextIndex = () => idx++;
            ngin.LambdaRepo.Register(nameof(GetNextIndex), GetNextIndex);

            ngin.TrimWhitespace = true;

            string result = ngin.Merge(isTrue);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple conditional blocks
        static void SimpleConditionalBlocks()
        {
            string name = "simple-conditional-blocks";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region negated conditionals blocks
        static void NegatedConditionalBlocks()
        {
            string name = "negated-conditional-blocks";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple whitespace control
        static void SimpleWhitespaceControl()
        {
            string name = "simple-whitespace-control";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region global whitespace control
        static void GlobalWhitespaceControl()
        {
            string name = "global-whitespace-control";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region complex whitespace control
        static void ComplexWhitespaceControl()
        {
            string name = "complex-whitespace-control";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = false,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple iteration blocks
        static void SimpleIterationBlocks()
        {
            string name = "simple-iteration-blocks";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                Certifications = new[] 
                {
                    new { Cert = "Microsoft Certified Architect", Abbr = "MCA", AttainedAt = DateTime.Parse("2016-05-01").ToString("MM/dd/yyyy") },
                    new { Cert = "Red Hat Certified Engineer", Abbr = "RHCE", AttainedAt = DateTime.Parse("2017-06-01").ToString("MM/dd/yyyy") },
                    new { Cert = "Linux Professional Institute Certification", Abbr = "LPIC", AttainedAt = DateTime.Parse("2018-04-16").ToString("MM/dd/yyyy") },
                },
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region throw on non ienumerable iteration target
        static void ThrowOnNonIEnumerableIterationTarget()
        {
            string name = "throw-on-non-ienumerable-iteration-target";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...

            bool passed = false;
            try
            {
                string result = ngin.Merge(data);
            }
            catch (MergeException mex)
            {
                passed = true;
            }

            RenderOutput(name, passed);
        }
        #endregion

        #region walking the scope chain
        static void WalkingTheScopeChain()
        {
            string name = "walking-the-scope-chain";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                Favorite = new
                {
                   Things = new object[]
                   {
                       new { Name = "Local", Values = new[] { "El Paso", "Houston", "Austin" } },
                       new { Name = "Junk Food", Values = new[] { "Black Licorice", "Candy Corn", "Good & Plenty" } }
                   },
                }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple partial blocks
        static void SimplePartialBlocks()
        {
            string name = "simple-partial-blocks";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                Addresses = new Address[]
                {
                    new Address { Line1 = "123 Main St", Line2 = "Apt 200", City = "Dallas", State = "TX", Zip = "75001" },
                    new Address { Line1 = "321 Main St", Line2 = "Apt 210", City = "San Antonio", State = "TX", Zip = "78006" },
                    new Address { Line1 = "400 W. 4th", Line2 = "Apt 198", City = "Lubbock", State = "TX", Zip = "79401" },
                    new Address { Line1 = "W 66th", Line2 = "Apt 222", City = "Austin", State = "TX", Zip = "73301" },
                },
                NewLine = Environment.NewLine,
                AddressTemplate = @"{$.Line1}{..\$.NewLine}{$.Line2}{..\$.NewLine}{$.City}, {$.State} {$.Zip}{..\$.NewLine}"
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple template comments
        static void SimpleTemplateComments()
        {
            string name = "simple-template-comments";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region multi line template comments
        static void MultiLineTemplateComments()
        {
            string name = "multi-line-template-comments";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Classes = new[]
                {
                    new
                    {
                        Name = "Person",
                        Properties = new[]
                        {
                            new { TypeShorthand = "string", Name = "FirstName" },
                            new { TypeShorthand = "string", Name = "LastName" },
                            new { TypeShorthand = "int", Name = "Age" },
                        }
                    },
                     new
                    {
                        Name = "Address",
                        Properties = new[]
                        {
                            new { TypeShorthand = "string", Name = "Line1" },
                            new { TypeShorthand = "string", Name = "Line2" },
                            new { TypeShorthand = "string", Name = "City" },
                            new { TypeShorthand = "string", Name = "State" },
                            new { TypeShorthand = "string", Name = "Zip" },
                        }
                    }
                }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple lambda expressions
        static void SimpleLambdaExpressions()
        {
            string name = "simple-lambda-expressions";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Spanish = new[] { "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve", "Diez" },
                English = new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" }
            };

            int at = 0;
            Func<string> GetAlternatingClass = () =>
            {
                return (at++ % 2) == 1 ? "dark" : "light";
            };

            Func<string, string> ResolveEnglishTranslation = (value) =>
            {
                int idx = data.Spanish.ToList().FindIndex(s => string.Compare(s, value, true) == 0);
                return (data.English.Length > idx) ? data.English[idx] : "N/A";
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;//global flag for whitespace control...
            ngin.LambdaRepo.Register(nameof(GetAlternatingClass), GetAlternatingClass);
            ngin.LambdaRepo.Register(nameof(ResolveEnglishTranslation), ResolveEnglishTranslation);

            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region complex lambda expressions
        static void ComplexLambdaExpressions()
        {
            string name = "complex-lambda-expressions";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                Certifications = new[]
                {
                    new { Cert = "Microsoft Certified Architect", Abbr = "MCA", AttainedAt = DateTime.Parse("2016-05-01").ToString("MM/dd/yyyy") },
                    new { Cert = "Red Hat Certified Engineer", Abbr = "RHCE", AttainedAt = DateTime.Parse("2017-06-01").ToString("MM/dd/yyyy") },
                    new { Cert = "Linux Professional Institute Certification", Abbr = "LPIC", AttainedAt = DateTime.Parse("2018-04-16").ToString("MM/dd/yyyy") },
                },
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            Func<string, int, string> TrimTo = (value, len) =>
            {
                if (string.IsNullOrEmpty(value) || value.Length <= len)
                    return value;
                else
                    return value.Substring(0, (len - 3)) + "...";
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(string.Join), (Func<string, object[], string>)string.Join);
            ngin.LambdaRepo.Register(nameof(TrimTo), TrimTo);
            ngin.TrimWhitespace = true;//global flag for whitespace control...

            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region lambda expression driven blocks
        static void LambdaExpressionDrivenBlocks()
        {
            string name = "lambda-expression-driven-blocks";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                User = new
                {
                    Id = 11111,
                    UserName = "cbrown",
                    Name = new { First = "Charlie", Last = "Brown" },
                    BirthDate = DateTime.Parse("5-10-1998"),
                    Address = new
                    {
                        Line1 = "123 Main St.",
                        Line2 = "Suite 200",
                        City = "Dallas",
                        State = "TX",
                        Zip = "77777"
                    }
                },
                LoginAttempts = new[]
                {
                    new { At = DateTime.Now.AddDays(-1), Success = true, SessionDurationMinutes = 23 },
                    new { At = DateTime.Now.AddDays(-3), Success = false, SessionDurationMinutes = 0 },
                    new { At = DateTime.Now.AddDays(-5), Success = true, SessionDurationMinutes = 24 },
                    new { At = DateTime.Now.AddDays(-9), Success = true, SessionDurationMinutes = 7 },
                    new { At = DateTime.Now.AddDays(-14), Success = true, SessionDurationMinutes = 39 },
                    new { At = DateTime.Now.AddDays(-17), Success = true, SessionDurationMinutes = 9 },
                    new { At = DateTime.Now.AddDays(-32), Success = true, SessionDurationMinutes = 18 },
                    new { At = DateTime.Now.AddDays(-41), Success = true, SessionDurationMinutes = 24 },
                }
            };

            Func<string, string> ResolvePartial = (nameKey) =>
            {
                string partial = ResolveTemplateInput(nameKey);
                return partial;
            };

            Func<DateTime, bool> IsMinor = (birthDate) =>
            {
                int yrs = (int)Math.Floor((DateTime.Now - birthDate).TotalDays / 365);
                return yrs < 18;
            };

            DateTime now = DateTime.Now;
            Func<Array, int, Array> Resolve30DayLoginAttemptsOfMinDuration = (attempts, minimumDuration) =>
            {
                var set = attempts.OfType<dynamic>().ToList().FindAll
                (
                    a => a.Success && a.SessionDurationMinutes > minimumDuration && (now - (DateTime)a.At).TotalDays <= 30
                );
                return set.ToArray();
            };

            Func<DateTime, string, string> FormatDateTime = (dt, format) =>
            {
                return dt.ToString(format);
            };


            TemplateEngine ngin = new TemplateEngine(template);

            ngin.LambdaRepo.Register(nameof(ResolvePartial), ResolvePartial);
            ngin.LambdaRepo.Register(nameof(IsMinor), IsMinor);
            ngin.LambdaRepo.Register(nameof(Resolve30DayLoginAttemptsOfMinDuration), Resolve30DayLoginAttemptsOfMinDuration);
            ngin.LambdaRepo.Register(nameof(FormatDateTime), FormatDateTime);

            string result = ngin.Merge(data);
               

            string expected = ResolveTemplateOutput(name)
                .Replace("####now-1####", now.AddDays(-1).ToString("MM-dd-yyyy hh:mm"))
                .Replace("####now-5####", now.AddDays(-5).ToString("MM-dd-yyyy hh:mm"))
                .Replace("####now-14####", now.AddDays(-14).ToString("MM-dd-yyyy hh:mm")); ;

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region with tag scope change blocks
        static void WithTagScopeChangeBlocks()
        {
            string name = "with-scope-change";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Contact = new {
                    Name = new { First = "Charlie", Last = "Brown" },
                },
                Structure = new
                {
                    Type = "School",
                    YearBuilt = 1925,
                    Address = new
                    {
                        Line1 = "123 Main St.",
                        Line2 = "Suite 200",
                        City = "Dallas",
                        State = "TX",
                        Zip = "77777"
                    }
                },
            };

            Func<string> GetAddressPartial = () =>
            {
                return ResolveTemplateInput("address-partial-scoped");
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(GetAddressPartial), GetAddressPartial);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region code gen
        static void CodeGen()
        {
            string name = "code-gen";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                NamespaceRoot = "HatTrick.Common",
                Model = new
                {
                    Enums = new object[]
                    {
                        new
                        {
                            Name = "AddressType",
                            Items = new[]
                            {
                                new { FriendlyName = "Physical", Description = "Physical Address", Key = "0", Value = "Physical" },
                                new { FriendlyName = "Mailing", Description = "Mailing Address", Key = "1", Value = "Mailing" },
                                new { FriendlyName = "Secondary", Description = "Secondary Address", Key = "2", Value = "Secondary" },
                            }
                        },
                        new
                        {
                            Name = "CardType",
                            Items = new[]
                            {
                                new { FriendlyName = "Visa", Description = "Visa", Key = "0", Value = "Visa" },
                                new { FriendlyName = "Master Card", Description = "Master Card", Key = "1", Value = "MC" },
                                new { FriendlyName = "Amex", Description = "American Express", Key = "2", Value = "Amex" },
                            }
                        }
                    }
                }
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = false; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion
	}

	#region person class
	public class Person
    {
        public int Age;

        public Name Name { get; set; }
        public Address Address { get; set; }
    }
    #endregion

    #region name class
    public class Name
    {
        public string First { get; set; }
        public string Last { get; set; }
    }
    #endregion

    #region address class
    public class Address
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
    #endregion
}
