using System;
using System.Text;
using HatTrick.Reflection;
using HatTrick.Text.Templating;
using HatTrick.Text.Test.Models;
using Xunit;

namespace HatTrick.Text.Test
{
    public class DebugOutputTests
    {
        [Fact]
        public void Does_debug_trace_output_propogate_single_quoted_string_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            var ngin = new TemplateEngine("{@ 'Hello world!'}");

            //when
            ngin.Merge(null);

            //then
            Assert.Equal("Hello world!", sb.ToString());
        }

        [Fact]
        public void Does_debug_trace_output_propogate_numeric_literals_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            var ngin = new TemplateEngine("{@ 0.00123}{@ +0.000001}{@-0.000001}");

            //when
            ngin.Merge(null);

            //then
            Assert.Equal("0.00123+0.000001-0.000001", sb.ToString());
        }

        [Fact]
        public void Does_debug_trace_output_propogate_reflected_item_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            var ngin = new TemplateEngine("{@ $.Name.Last}{@ ', '}{@ $.Name.First}");

            var data = new { Name = new { First = "Charlie", Last = "Brown" } };

            //when
            ngin.Merge(data);

            //then
            Assert.Equal("Brown, Charlie", sb.ToString());
        }

        [Fact]
        public void Does_debug_trace_output_propogate_single_quoted_string_literal_containing_escaped_single_quote_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            var ngin = new TemplateEngine("{@ 'The King\\'s Castle'}");

            //when
            ngin.Merge(null);

            //then
            Assert.Equal("The King's Castle", sb.ToString());
        }

        [Fact]
        public void Does_debug_trace_output_propogate_double_quoted_string_literal_containing_escaped_double_quote_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            var ngin = new TemplateEngine("{@ \"The King's \\\"Castle\\\"\"}");

            //when
            ngin.Merge(null);

            //then
            Assert.Equal("The King's \"Castle\"", sb.ToString());
        }

        [Fact]
        public void Does_debug_trace_output_propogate_double_quoted_string_literal_containing_escaped_double_quote_round_tripped_through_lambda_func_to_output_window()
        {
            //given
            var listen = new DebugTraceListener();
            System.Diagnostics.Trace.Listeners.Add(listen);

            StringBuilder sb = new StringBuilder();
            listen.Push += (msg) => sb.Append(msg);

            Func<string, string> roundTrip = (val) => val;

            var ngin = new TemplateEngine("{@ (\"The King's \\\"Castle\\\"\") => roundTrip}");
            ngin.LambdaRepo.Register(nameof(roundTrip), roundTrip);

            //when
            ngin.Merge(null);

            //then
            Assert.Equal("The King's \"Castle\"", sb.ToString());
        }
    }
}
