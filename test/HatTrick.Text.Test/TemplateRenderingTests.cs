using HatTrick.Reflection;
using HatTrick.Text.Templating;
using System;
using Xunit;

namespace HatTrick.Text.Test
{
    public class TemplateRenderingTests
    {
        [Theory]
        [Templates("with-scope-change-in.txt", "address-partial-scoped-in.txt", "with-scope-change-out.txt")]
        public void Can_a_scope_change_with_subtemplate_render_correctly(string template, string partial, string expected)
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
                },
            };

            Func<string> GetAddressPartial = () => partial;

            TemplateEngine ngin = new TemplateEngine(template);
            ngin.LambdaRepo.Register(nameof(GetAddressPartial), GetAddressPartial);
            ngin.TrimWhitespace = true; //global flag for whitespace control...

            //when
            string result = ngin.Merge(data);

            //then
            Assert.Equal(result, expected);
        }

        [Theory]
        [Templates("simple-tags-in.txt", "simple-tags-out.txt")]
        public void Can_a_dynamic_object_render_correctly(string template, string expected)
        {
            //given
            var data = new
            {
                First = "Charlie",
                Last = "Brown"
            };

            //when
            string result = new TemplateEngine(template).Merge(data);

            //then
            Assert.Equal(result, expected);
        }

        [Theory]
        [Templates("bracket-escaping-in.txt", "bracket-escaping-out.txt")]
        public void Can_a_template_with_escaped_brackets_render_correctly(string templateWithEscapedBrackets, string expected)
        {
            //given
            var data = new
            {
                Name = "Charlie Brown",
            };

            //when
            string result = new TemplateEngine(templateWithEscapedBrackets).Merge(data);

            //then
            Assert.Equal(result, expected);
        }

        [Theory]
        [Templates("throw-on-no-item-exists-in.txt", typeof(NoItemExistsException))]
        public void Does_rendering_throw_exception_when_dynamic_object_does_not_contain_a_property_for_a_template_tag(string template, Type expected)
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
    }
}
