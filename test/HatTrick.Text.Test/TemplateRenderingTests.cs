using HatTrick.Reflection;
using HatTrick.Text.Templating;
using HatTrick.Text.Test.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HatTrick.Text.Test
{
    public class TemplateRenderingTests
    {
        [Theory]
        [Templates("simple-tags-in.txt", "simple-tags-out.txt")]
        public void Can_a_template_containing_only_simple_tags_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                First = "Charlie",
                Last = "Brown"
            };

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("bracket-escaping-in.txt", "bracket-escaping-out.txt")]
        public void Can_a_template_with_escaped_brackets_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = "Charlie Brown",
            };

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("complex-bind-expressions-in.txt", "complex-bind-expressions-out.txt")]
        public void Can_a_template_with_complex_bind_expressions_render_correctly(string template, string expected)
        {
            //given
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

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("bind-object-support-in.txt", "bind-object-support-out.txt")]
        public void Can_engine_bind_correctly_to_plain_old_clr_object(string template, string expected)
        {
            //given (clr type}
            var data = new Person()
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

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("bind-object-support-in.txt", "bind-object-support-out.txt")]
        public void Can_engine_bind_correctly_to_nested_dictionary_object(string template, string expected)
        {
            //given (nested dictionary}
            var data = new Dictionary<string, object>()
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

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("bind-object-support-in.txt", "bind-object-support-out.txt")]
        public void Can_engine_bind_correctly_to_anonymous_types(string template, string expected)
        {
            //given (anonymous type}
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

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("throw-on-no-item-exists-in.txt", typeof(NoItemExistsException))]
        public void Does_rendering_throw_exception_when_object_does_not_contain_a_property_for_a_template_tag(string template, Type expected)
        {
            //given
            var data = new
            {
                First = "First",
                Second = "Second",
                Third = "Third"
            };

            //when
            void merge() => new TemplateEngine(template).Merge(data);

            //then
            Assert.Throws(expected, merge);
        }

        [Theory]
        [Templates("simple-conditional-blocks-in.txt", "simple-conditional-blocks-out.txt")]
        public void Can_a_template_with_conditional_blocks_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = false;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("negated-conditional-blocks-in.txt", "negated-conditional-blocks-out.txt")]
        public void Can_a_template_with_negated_conditional_blocks_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = false;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("simple-whitespace-control-in.txt", "simple-whitespace-control-out.txt")]
        public void Do_whitespace_trim_flags_function_properly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            //when
            string actual = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("global-whitespace-control-in.txt", "global-whitespace-control-out.txt")]
        public void Does_global_whitespace_trim_function_properly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = true,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("complex-whitespace-control-in.txt", "complex-whitespace-control-out.txt")]
        public void Do_whitespace_retain_flags_function_properly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
                IsEmployed = false,
                CurrentEmployer = "Hat Trick Labs",
                Spouse = default(object),//null is falsey
                Certifications = new string[] { },//empty array is falsey
                PreviousEmployers = new[] { "Microsoft", "Cisco", "FB" }
            };
            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("simple-iteration-blocks-in.txt", "simple-iteration-blocks-out.txt")]
        public void Do_simple_iteration_block_tags_render_correctly(string template, string expected)
        {
            //given
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

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("throw-on-non-ienumerable-iteration-target-in.txt", typeof(MergeException))]
        public void Does_rendering_throw_exception_when_iteration_target_is_not_ienumerable(string template, Type expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
            };

            //when
            void merge() => new TemplateEngine(template).Merge(data);

            //then
            Assert.Throws(expected, merge);
        }

        [Theory]
        [Templates("walking-the-scope-chain-in.txt", "walking-the-scope-chain-out.txt")]
        public void Can_binding_walk_the_scope_chain(string template, string expected)
        {
            //given
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

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("simple-partial-blocks-in.txt", "simple-partial-blocks-out.txt")]
        public void Do_simple_partial_blocks_render_correctly(string template, string expected)
        {
            //given
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

            var ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("simple-template-comments-in.txt", "simple-template-comments-out.txt")]
        public void Do_simple_template_comments_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                Name = new { First = "Charlie", Last = "Brown" },
            };

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("multi-line-template-comments-in.txt", "multi-line-template-comments-out.txt")]
        public void Do_simple_multi_line_template_comments_render_correctly(string template, string expected)
        {
            //given
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
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("simple-lambda-expressions-in.txt", "simple-lambda-expressions-out.txt")]
        public void Do_simple_lambda_expressions_render_correctly(string template, string expected)
        {
            //given
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

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("complex-lambda-expressions-in.txt", "complex-lambda-expressions-out.txt")]
        public void Do_complex_lambda_expressions_render_correctly(string template, string expected)
        {
            //given
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
            ngin.TrimWhitespace = true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("lambda-numeric-literals-in.txt", "lambda-numeric-literals-out.txt")]
        public void Do_lambda_numeric_literals_render_correctly(string template, string expected)
        {
            //given
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

            //when
            string actual = ngin.Merge(null);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("truthy-falsy-in.txt", "truthy-falsy-out.txt")]
        public void Does_truthy_falsy_conditional_logic_evaluate_correctly(string template, string expected)
        {
            //given
            TemplateEngine ngin = new TemplateEngine(template);
            ngin.TrimWhitespace = true;

            int idx = 0;
            Func<int> GetNextIndex = () => idx++;

            Action ResetIndex = () => idx = 0;

            ngin.LambdaRepo.Register(nameof(GetNextIndex), GetNextIndex);
            ngin.LambdaRepo.Register(nameof(ResetIndex), ResetIndex);

            bool[] data = new bool[20];
            data[0] = ngin.IsTrue(null);           //false;
            data[1] = ngin.IsTrue(1.00F);          //true;
            data[2] = ngin.IsTrue(1U);             //true;
            data[3] = ngin.IsTrue(0.00F);          //false;
            data[4] = ngin.IsTrue(0);              //false;
            data[5] = ngin.IsTrue(string.Empty);   //false;
            data[6] = ngin.IsTrue(new object[0]);  //false;
            data[7] = ngin.IsTrue(new object[1]);  //true;
            data[8] = ngin.IsTrue(true);           //true;
            data[9] = ngin.IsTrue(false);          //false;
            data[10] = ngin.IsTrue('\0');          //false;
            data[11] = ngin.IsTrue('t');           //true;
            data[12] = ngin.IsTrue('f');           //true;
            data[13] = ngin.IsTrue((decimal)1.111);//true;
            data[14] = ngin.IsTrue((decimal)0.000);//false;
            data[15] = ngin.IsTrue("\0");          //false;
            data[16] = ngin.IsTrue("f");           //true;
            data[17] = ngin.IsTrue("t");           //true;
            data[18] = ngin.IsTrue("false");       //true;
            data[19] = ngin.IsTrue("hello");       //true;

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("lambda-expression-driven-blocks-in.txt", "address-partial-in.txt", "lambda-expression-driven-blocks-out.txt")]
        public void Do_lambda_expression_driven_blocks_render_correctly(string template, string partial, string expected)
        {
            //given
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
                if (nameKey == "address-partial")
                    return partial;
                else
                    return "broke";
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

            //when
            string actual = ngin.Merge(data);

            expected = expected.Replace("####now-1####", now.AddDays(-1).ToString("MM-dd-yyyy hh:mm"))
                .Replace("####now-5####", now.AddDays(-5).ToString("MM-dd-yyyy hh:mm"))
                .Replace("####now-14####", now.AddDays(-14).ToString("MM-dd-yyyy hh:mm"));

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("with-scope-change-in.txt", "address-partial-scoped-in.txt", "with-scope-change-out.txt")]
        public void Does_a_scope_change_with_subtemplate_render_correctly(string template, string partial, string expected)
        {
            //given
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
                    Address = new
                    {
                        Line1 = "123 Main St.",
                        Line2 = "Suite 200",
                        City = "Dallas",
                        State = "TX",
                        Zip = "77777"
                    }
                }
            };

            Func<string> GetAddressPartial = () => partial;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(GetAddressPartial), GetAddressPartial);
            ngin.TrimWhitespace = true; //global flag for whitespace control...

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("code-gen-in.txt", "code-gen-out.txt")]
        public void Does_sample_code_generation_render_correctly(string template, string expected)
        {
            //given
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

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [Templates("variable-declarations-in.txt", "address-partial-scoped-in.txt", "variable-declarations-out.txt")]
        public void Does_variable_declaration_and_usage_render_correctly(string template, string partial, string expected)
        {
            //given
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
                return partial;
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

            //when
            string actual = ngin.Merge(data);

            //then
            Assert.Equal(expected, actual);
        }
    }
}
