﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HatTrick.Text;
using System.Linq;

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
            ThrowOnOverreachIntoScopeChain();
            WalkingTheScopeChain();
            SimplePartialBlocks();
            SimpleTemplateComments();
            MultiLineTemplateComments();
            SimpleLambdaExpressions();
            LambdaNumericLiterals();
            ComplexLambdaExpressions();
            LambdaExpressionDrivenBlocks();
            WithTagScopeChangeBlocks();
            CodeGen();
            DeclaringAndUsingVariables();
            LiteralVariableDeclarations();
            ReAssigningVariableValues();
            ComplexReAssignmingVariableValues();
            SingleLinkScopeChainReference();
            TwoLinkScopeChainReference();
            ThreeLinkScopeChainReference();
            EightLinkScopeChainReference();
            PushEightPopFourLinksScopeChainReference();
            ScopeChainVariableReference();
            VariableScopeMarkerOnNullStack();
            VariableScopeMarkerOnNonNullStack();
            VariableReAssignment();
            OuterScopeVariableReAssignment();
            LargeScopeChainAndVariableStacks();

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
            string path = $"..{dsc}..{dsc}..{dsc}sample-templates{dsc}{name}-{context}.txt";
            return path;
        }
        #endregion

        #region output result
        static void RenderOutput(string name, bool passed, string context = null)
        {
            Console.WriteLine(string.Format("{0,-45} {1,-10} {2,-20}", name, passed ? "Success" : "Failed", context));
        }
        #endregion

        #region simple tags x
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

        #region bracket escaping x
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

        #region complex bind expressions x
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

        #region bind object support x
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

        #region throw on no item exists x
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
            catch// (Exception npe)
            {
                passed = true;
            }

            //string expected = ResolveTemplateOutput(name);

            RenderOutput(name, passed);
        }
        #endregion

        #region simple conditional blocks x
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

        #region negated conditionals blocks x
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

        #region simple whitespace control x
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

        #region global whitespace control x
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

        #region complex whitespace control x
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

        #region simple iteration blocks x
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

        #region throw on non ienumerable iteration target x
        static void ThrowOnNonIEnumerableIterationTarget()
        {
            string name = "throw-on-non-ienumerable-iteration-target";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
            };

            TemplateEngine ngin = new TemplateEngine(template);

            bool passed = false;
            try
            {
                string result = ngin.Merge(data);
            }
            catch// (MergeException mex)
            {
                passed = true;
            }

            RenderOutput(name, passed);
        }
        #endregion

        #region throw on overreach into scope chain
        static void ThrowOnOverreachIntoScopeChain()
        {
            string name = "throw-on-overreach-into-scopechain";

            var data1 = new { Name = new { First = "Charlie", Last = "Brown" } };
            var data2 = new { Name = new { First = "Super", Last = "Man" } };

            ScopeChain chain = new ScopeChain();
            chain.Push(data1);
            chain.Push(data2);


            bool passed = false;
            try
            {
                chain.Peek(2);
            }
            catch (ArgumentException ex)
            {
                passed = ex.Message == "value must be < ScopeChain.Depth\r\nParameter name: back";
            }

            RenderOutput(name, passed);
        }
        #endregion

        #region walking the scope chain x
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

        #region simple partial blocks x
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
                AddressTemplate = @"{$.Line1}{..\NewLine}{$.Line2}{..\$.NewLine}{$.City}, {$.State} {$.Zip}{..\$.NewLine}"
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region simple template comments x
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

        #region multi line template comments x
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

        #region simple lambda expressions x
        static void SimpleLambdaExpressions()
        {
            string name = "simple-lambda-expressions";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Spanish = new List<string> { "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve", "Diez" },
                English = new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" }
            };

            int at = 0;
            Func<string> GetAlternatingClass = () =>
            {
                return (at++ % 2) == 1 ? "dark" : "light";
            };

            Func<string, string> ResolveEnglishTranslation = (value) =>
            {
                int idx = data.Spanish.FindIndex(s => string.Compare(s, value, true) == 0);
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

        #region lambda numeric literals x
        static void LambdaNumericLiterals()
        {
            string name = "lambda-numeric-literals";
            string template = ResolveTemplateInput(name);

            Func<int, int, int> SumTwoIntegers = (int x, int y) => x + y;

            Func<double, double, double> SumTwoDoubles = (double x, double y) => x + y;

            Func<decimal, decimal, decimal> SumTwoDecimals = (decimal x, decimal y) => x + y;

            Func<int, double, decimal, decimal> SumIntDoubleDecimal = (int x, double y, decimal z) => (decimal)x + (decimal)y + (decimal)z;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;//global flag for whitespace control...
            ngin.LambdaRepo.Register(nameof(SumTwoIntegers), SumTwoIntegers);
            ngin.LambdaRepo.Register(nameof(SumTwoDoubles), SumTwoDoubles);
            ngin.LambdaRepo.Register(nameof(SumTwoDecimals), SumTwoDecimals);
            ngin.LambdaRepo.Register(nameof(SumIntDoubleDecimal), SumIntDoubleDecimal);

            string result = ngin.Merge(null);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region complex lambda expressions x
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

        #region truthy falsey x
        private static void TruthyFalsey()
        {
            string name = "truthy-falsy";
            string template = ResolveTemplateInput(name);
            TemplateEngine ngin = new TemplateEngine(template);

            bool[] isTrue = new bool[20];
            isTrue[0] = ngin.IsTrue(null);           //false;
            isTrue[1] = ngin.IsTrue(1.00F);          //true;
            isTrue[2] = ngin.IsTrue(1U);             //true;
            isTrue[3] = ngin.IsTrue(0.00F);          //false;
            isTrue[4] = ngin.IsTrue(0);              //false;
            isTrue[5] = ngin.IsTrue(string.Empty);   //false;
            isTrue[6] = ngin.IsTrue(new object[0]);  //false;
            isTrue[7] = ngin.IsTrue(new object[1]);  //true;
            isTrue[8] = ngin.IsTrue(true);           //true;
            isTrue[9] = ngin.IsTrue(false);          //false;
            isTrue[10] = ngin.IsTrue('\0');          //false;
            isTrue[11] = ngin.IsTrue('t');           //true;
            isTrue[12] = ngin.IsTrue('f');           //true;
            isTrue[13] = ngin.IsTrue((decimal)1.111);//true;
            isTrue[14] = ngin.IsTrue((decimal)0.000);//false;
            isTrue[15] = ngin.IsTrue("\0");          //false;
            isTrue[16] = ngin.IsTrue("f");           //true;
            isTrue[17] = ngin.IsTrue("t");           //true;
            isTrue[18] = ngin.IsTrue("false");       //true;
            isTrue[19] = ngin.IsTrue("hello");       //true;

            int idx = 0;
            Func<int> GetNextIndex = () => idx++;
            Action ResetIndex = () => idx = 0;
            ngin.LambdaRepo.Register(nameof(GetNextIndex), GetNextIndex);
            ngin.LambdaRepo.Register(nameof(ResetIndex), ResetIndex);

            ngin.TrimWhitespace = true;

            string result = ngin.Merge(isTrue);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region lambda expression driven blocks x
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
                .Replace("####now-14####", now.AddDays(-14).ToString("MM-dd-yyyy hh:mm"));

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region with tag scope change blocks x
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

        #region code gen x
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
            ngin.TrimWhitespace = true; //global flag for whitespace control...
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region declaring and using variables x
        static void DeclaringAndUsingVariables()
        {
            string name = "variable-declarations";
            string template = ResolveTemplateInput(name);

            var data = new
            {
                Contact = new
                {
                    Name = new { First = "Charlie", Last = "Brown" },
                },
                Structure = new
                {
                    Type = "School",
                    YearBuilt = 1925,
                    Address = new Address
                    {
                        Line1 = "123 Main St.",
                        Line2 = "Suite 200",
                        City = "Dallas",
                        State = "TX",
                        Zip = "77777"
                    }
                },
                Inspectors = new[]
                {
                    new { Name = "John Doe", YearsOfService = 13, Expertise = new[] { "Gothic", "Neoclassical" } },
                    new { Name = "Jane Doe", YearsOfService = 10, Expertise = new[] { "Modern", "Victorian" }  },
                }
            };

            Func<string> GetAddressPartial = () =>
            {
                return ResolveTemplateInput("address-partial-scoped");
            };

            Func<string> GetSomeVal = () => "some val";

            Func<Address, string> GetCityAndState = (a) => $"{a.City}, {a.State}";

            Func<int, int> IncrementAndReturn = (counter) => ++counter;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(GetAddressPartial), GetAddressPartial);
            ngin.LambdaRepo.Register(nameof(GetSomeVal), GetSomeVal);
            ngin.LambdaRepo.Register(nameof(GetCityAndState), GetCityAndState);
            ngin.LambdaRepo.Register(nameof(IncrementAndReturn), IncrementAndReturn);
            ngin.TrimWhitespace = true;
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region literal variable declarations
        static void LiteralVariableDeclarations()
        {
            string name = "literal-variable-declarations";
            string template = ResolveTemplateInput(name);


            Func<int, double, decimal, decimal> sumIntDoubleDecimal = (v1, v2, v3) => v1 + (decimal)v2 + v3;

            Func<string, string, string> concat = (v1, v2) => v1 + v2;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;
            ngin.LambdaRepo.Register(nameof(sumIntDoubleDecimal), sumIntDoubleDecimal);
            ngin.LambdaRepo.Register(nameof(concat), concat);

            string result = ngin.Merge(null);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region re assigning variable values
        static void ReAssigningVariableValues()
        {
            string name = "variable-re-assignment";
            string template = ResolveTemplateInput(name);

            Func<string, string, string> concat = (v1, v2) => v1 + v2;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;
            ngin.LambdaRepo.Register(nameof(concat), concat);

            string result = ngin.Merge(null);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion

        #region complex re assigning variable values
        static void ComplexReAssignmingVariableValues()
        {
            string name = "complex-variable-re-assignment";
            string template = ResolveTemplateInput(name);

            Func<string, string, string> concat = (v1, v2) => v1 + v2;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;
            ngin.LambdaRepo.Register(nameof(concat), concat);

            string result = ngin.Merge(true);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
		#endregion

		#region single link scope chain reference
		static void SingleLinkScopeChainReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);

            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Name.First");
            string expected = "Charlie";

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(nameof(SingleLinkScopeChainReference), passed);
        }
        #endregion

        #region two link scope chain reference
        static void TwoLinkScopeChainReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);

            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);

            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Name.First");
            string expected = "Susie";

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(nameof(TwoLinkScopeChainReference), passed);
        }
        #endregion

        #region three link scope chain reference
        static void ThreeLinkScopeChainReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);

            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);

            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);

            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Zip");
            string expected = "75075";

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(nameof(ThreeLinkScopeChainReference), passed);
        }
        #endregion

        #region eight link scope chain reference
        static void EightLinkScopeChainReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);

            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);

            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);

            var p3 = new { Name = new { First = "Spider", Last = "Man" } };
            chain.Push(p3);

            var p4 = new { Name = new { First = "Luke", Last = "Skywalker" } };
            chain.Push(p4);

            var p5 = new { Name = new { First = "Peter", Last = "Pan" } };
            chain.Push(p5);

            var p6 = new { Name = new { First = "Duke", Last = "Caboom" } };
            chain.Push(p6);

            var p7 = new { Name = new { First = "Buzz", Last = "Lightyear" } };
            chain.Push(p7);

            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Name.Last");
            string expected = "Lightyear";

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(nameof(EightLinkScopeChainReference), passed);
        }
        #endregion

        #region push eight pop four links scope chain reference
        static void PushEightPopFourLinksScopeChainReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);

            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);

            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);

            var p3 = new { Name = new { First = "Spider", Last = "Man" } };
            chain.Push(p3);

            var p4 = new { Name = new { First = "Luke", Last = "Skywalker" } };
            chain.Push(p4);

            var p5 = new { Name = new { First = "Peter", Last = "Pan" } };
            chain.Push(p5);

            var p6 = new { Name = new { First = "Duke", Last = "Caboom" } };
            chain.Push(p6);

            var p7 = new { Name = new { First = "Buzz", Last = "Lightyear" } };
            chain.Push(p7);

            chain.Pop();
            chain.Pop();
            chain.Pop();
            chain.Pop();

            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Name.First");
            string expected = "Spider";

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(nameof(PushEightPopFourLinksScopeChainReference), passed);
        }
        #endregion

        #region ensure scope chain variable reference
        static void ScopeChainVariableReference()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);
            chain.SetVariable("var1", "uno");
            chain.SetVariable("var2", "dos");

            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);
            chain.SetVariable("var3", "tres");

            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);
            chain.SetVariable("firstPerson", p);
            chain.SetVariable("var4", "cuatro");
            chain.SetVariable("var5", "cinco");
            chain.SetVariable("secondPerson", p2);

            var p3 = new { Name = new { First = "Spider", Last = "Man" } };
            chain.Push(p3);
            chain.SetVariable("var6", "seis");
            chain.SetVariable("address", a);

            var p4 = new { Name = new { First = "Luke", Last = "Skywalker" } };
            chain.Push(p4);
            chain.SetVariable("var7", "siete");
            chain.SetVariable("var6", "xxx"); //override var6 in newer chain link...

            var p5 = new { Name = new { First = "Peter", Last = "Pan" } };
            chain.Push(p5);
            chain.SetVariable("var8", "ocho");

            var p6 = new { Name = new { First = "Duke", Last = "Caboom" } };
            chain.Push(p6);
            chain.SetVariable("var9", "nueve");

            var p7 = new { Name = new { First = "Buzz", Last = "Lightyear" } };
            chain.Push(p7);
            chain.SetVariable("var10", "dias");


            string result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Name.First");
            string expected = "Buzz";
            bool passed = true;
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = (chain.AccessVariable("firstPerson") as Person).Name.First;
            expected = "Charlie";
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = (chain.AccessVariable("address") as Address).City;
            expected = "Dallas";
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = (string)chain.AccessVariable("var10");
            expected = "dias";
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = (string)chain.AccessVariable("var6");
            expected = "xxx";
            passed = passed && (string.Compare(result, expected, false) == 0);

            chain.Pop();
            chain.Pop();

            result = (chain.AccessVariable("address") as Address).State;
            expected = "TX";
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = (string)Reflection.ReflectionHelper.Expression.ReflectItem(chain.AccessVariable("secondPerson"), "Name.Last");
            expected = "Derkins";
            passed = passed && (string.Compare(result, expected, false) == 0);

            result = string.Empty;
            try { var o = chain.AccessVariable("var9"); }
            catch (MergeException mex) { result = mex.Message; }
            expected = "Attempted access of unknown variable: var9";
            passed = passed && (string.Compare(result, expected, false) == 0);

            RenderOutput(nameof(ScopeChainVariableReference), passed);
        }
        #endregion

        #region variable scope marker on null stack
        static void VariableScopeMarkerOnNullStack()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person { Name = new Name { First = "James", Last = "Doe" } };
            chain.Push(p);
            chain.ApplyVariableScopeMarker();
            chain.SetVariable("city", "Dallas");
            chain.SetVariable("zip", "75075");

            var p2 = new Person { Name = new Name { First = "Charlie", Last = "Brown" } };
            chain.Push(p2);
            chain.SetVariable("test", "t1");
            chain.SetVariable("test2", "t2");

            string test = (string)chain.AccessVariable("test");

            chain.Pop(); //should pop the entire link including all variables set on it (test, test2)

            string city = (string)chain.AccessVariable("city");
            string zip = (string)chain.AccessVariable("zip");

            chain.DereferenceVariableScope();

            bool passed = false;
            try
            {
                test = (string)chain.AccessVariable("zip");
            }
            catch
            {
                passed = true;
            }

            RenderOutput(nameof(VariableScopeMarkerOnNullStack), passed);
        }
        #endregion

        #region variable scope marker on non null stack
        static void VariableScopeMarkerOnNonNullStack()
        {
            ScopeChain chain = new ScopeChain();

            var p = new Person { Name = new Name { First = "James", Last = "Doe" } };
            chain.Push(p);
            chain.SetVariable("city", "Dallas");
            chain.SetVariable("zip", "75075");
            chain.ApplyVariableScopeMarker();
            chain.SetVariable("isEmployed", true);
            chain.SetVariable("employer", "Hat Trick Labs");

            var p2 = new Person { Name = new Name { First = "Charlie", Last = "Brown" } };
            chain.Push(p2);
            chain.SetVariable("test", "t1");
            chain.SetVariable("test2", "t2");

            string test = (string)chain.AccessVariable("test");

            chain.Pop(); //should pop the entire link including all variables set on it (test, test2)

            string city = (string)chain.AccessVariable("city");
            string zip = (string)chain.AccessVariable("zip");

            chain.DereferenceVariableScope();

            city = (string)chain.AccessVariable("city");
            zip = (string)chain.AccessVariable("zip");

            chain.DereferenceVariableScope();

            bool passed = false;
            try
            {
                test = (string)chain.AccessVariable("zip");
            }
            catch
            {
                passed = true;
            }

            RenderOutput(nameof(VariableScopeMarkerOnNullStack), passed);
        }
        #endregion

        #region variable re-assignment
        private static void VariableReAssignment()
        {
            ScopeChain chain = new ScopeChain();

            chain.Push("link1");
            chain.Push("link2");
            chain.Push("link3");

            //create a couple jnk vars
            chain.SetVariable(":firstName", "Jerrod");
            chain.SetVariable(":lastName", "Eiman");

            int age = 99;
            chain.SetVariable(":age", age);
            chain.UpdateVariable(":age", ++age); //increment age to 100

            bool passed = 100 == (int)chain.AccessVariable(":age");

            RenderOutput(nameof(VariableReAssignment), passed);
        }
        #endregion

        #region variable re-assignment
        private static void OuterScopeVariableReAssignment()
        {
            ScopeChain chain = new ScopeChain();

            chain.Push("link1");
            chain.SetVariable(":p1FirstName", "Charlie");
            chain.SetVariable(":p1LastName", "Brown");
            chain.SetVariable(":p1Age", 8);

            chain.Push("link2");
            chain.SetVariable(":p2FirstName", "Susie");
            chain.SetVariable(":p2LastName", "Derkins");
            chain.SetVariable(":p2Age", 6);

            chain.Push("link3");
            chain.SetVariable(":p3FirstName", "GI");
            chain.SetVariable(":p3LastName", "Joe");
            chain.SetVariable(":p3Age", 32);

            chain.Push("link4");
            chain.Push("link5");
            chain.Push("link6");
            chain.Push("link7");
            chain.Push("link8");
            chain.Push("link9");
            chain.Push("link10");

            chain.UpdateVariable(":p1Age", ((int)chain.AccessVariable(":p1Age")) + 1); //increment age to 9
            chain.UpdateVariable(":p2Age", ((int)chain.AccessVariable(":p2Age")) + 1); //increment age to 7
            chain.UpdateVariable(":p3Age", ((int)chain.AccessVariable(":p3Age")) + 1); //increment age to 33

            bool passed = 9 == (int)chain.AccessVariable(":p1Age")
                       && 7 == (int)chain.AccessVariable(":p2Age")
                       && 33 == (int)chain.AccessVariable(":p3Age");

            RenderOutput(nameof(OuterScopeVariableReAssignment), passed);
        }
        #endregion

        #region large scope chain and variable stacks
        private static void LargeScopeChainAndVariableStacks()
        {
            ScopeChain chain = new ScopeChain();
            
			for (int i = 0; i < 50; i++)
			{
                chain.Push(i.ToString());
				for (int j = 0; j < 50; j++)
				{
                    chain.SetVariable(i.ToString() + j.ToString(), j);
				}
			}

            bool passed = true;
			for (int i = 49; i > -1; i--)
			{
                for (int j = 0; j < 50; j++)
                {
                    int xxx = (int)chain.AccessVariable(i.ToString() + j.ToString());
                    passed = passed & xxx == j;
                    if (!passed)
                        break;
                }
                chain.Pop();
            }

            RenderOutput(nameof(LargeScopeChainAndVariableStacks), passed);
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
