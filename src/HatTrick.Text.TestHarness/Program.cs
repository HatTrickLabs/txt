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

            SimpleTags();
            BracketEscaping();
            ComplexBindExpressions();
            BindObjectSupport();
            ThrowOnNoItemExists();
            TruthyFalsey();

            Console.WriteLine("processing complete, press [Enter] to exit");
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
            Console.WriteLine($"{name}\t\t{(passed ? "Success" : "Failed")}\t\t{context}");
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
            catch (HatTrick.Text.Reflection.NoPropertyExistsException npe)
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
            TemplateEngine ngin = new TemplateEngine("");
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

            bool passed = isTrue.All(b => b);

            RenderOutput("truthy-falsey", passed);
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
                Spouse = default(object),//null

            };

            TemplateEngine ngin = new TemplateEngine(template);
            string result = ngin.Merge(data);

            string expected = ResolveTemplateOutput(name);

            bool passed = string.Compare(result, expected, false) == 0;

            RenderOutput(name, passed);
        }
        #endregion
    }

    #region person
    public class Person
    {
        public Name Name { get; set; }
        public Address Address { get; set; }
    }
    #endregion

    #region name classe
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
