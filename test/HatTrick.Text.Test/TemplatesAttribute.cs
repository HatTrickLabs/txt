using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace HatTrick.Text.Test
{
    public sealed class TemplatesAttribute : ClassDataAttribute
    {
        private readonly List<string> templates;
        private readonly string output;
        private readonly Type exceptionType;

        public TemplatesAttribute(string template, Type exceptionType) : base(typeof(TemplatesAttribute))
        {
            this.templates = new List<string>() { template };
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"{nameof(exceptionType)} must be of type Exception.");
            this.exceptionType = exceptionType;
        }

        public TemplatesAttribute(string template, string expected) : base(typeof(TemplatesAttribute))
        {
            this.templates = new List<string>() { template };
            this.output = expected;
        }

        public TemplatesAttribute(string template, string partial, string expected) : base(typeof(TemplatesAttribute))
        {
            this.templates = new List<string>() { template, partial };
            this.output = expected;
        }

        public TemplatesAttribute(string template, string partial1, string partial2, string expected) : base(typeof(TemplatesAttribute))
        {
            this.templates = new List<string>() { template, partial1, partial2 };
            this.output = expected;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var o = new object[templates.Count + 1];
            for (var i = 0; i < templates.Count; i++)
            {
                o[i] = File.ReadAllText(Path.Combine(ConfigurationProvider.InputPath, templates[i]));
            }
            if (!string.IsNullOrWhiteSpace(output))
            {
                o[^1] = File.ReadAllText(Path.Combine(ConfigurationProvider.OutputPath, output));
            }
            else if (exceptionType != null)
            {
                o[^1] = exceptionType;
            }

            yield return o;
        }
    }
}
