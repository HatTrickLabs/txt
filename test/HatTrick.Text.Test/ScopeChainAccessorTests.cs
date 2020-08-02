using HatTrick.Reflection;
using HatTrick.Text.Templating;
using HatTrick.Text.Test.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HatTrick.Text.Test
{
	public class ScopeChainAccessorTests
	{
        [Fact]
        public void Does_multi_link_scope_chain_reflect_correct_context_item()
        {
            //given
            ScopeChain chain = new ScopeChain();
            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);
            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);
            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);
            var p3 = new Name { First = "Spider", Last = "Man" };
            chain.Push(p3);

            //when
            var actual = ReflectionHelper.Expression.ReflectItem(chain.Peek(), "First");

            //then
            Assert.Equal("Spider", actual);
        }

        [Fact]
        public void Does_multi_link_scope_chain_return_closest_scoped_variable_by_name()
        {
            //given
            ScopeChain chain = new ScopeChain();
            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);
            chain.SetVariable("test", "t1");
            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);
            chain.SetVariable("test", "t2");
            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);
            chain.SetVariable("test", "t3");
            var p3 = new Name { First = "Spider", Last = "Man" };
            chain.Push(p3);
            chain.SetVariable("test", "t4");

            //when
            var actual = chain.AccessVariable("test");

            //then
            Assert.Equal("t4", actual);
        }

        [Fact]
        public void Does_multi_link_scope_chain_return_correct_item_on_reach_back()
        {
            //given
            ScopeChain chain = new ScopeChain();
            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);
            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);
            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);
            var p3 = new Name { First = "Spider", Last = "Man" };
            chain.Push(p3);
            var p4 = new Name { First = "Luke", Last = "Skywalker" };
            chain.Push(p4);
            var p5 = new { Name = new { First = "Peter", Last = "Pan" } };
            chain.Push(p5);
            var p6 = new Name { First = "Duke", Last = "Caboom" };
            chain.Push(p6);
            var p7 = new Name { First = "Buzz", Last = "Lightyear" };
            chain.Push(p7);

            //when
            var actual = ReflectionHelper.Expression.ReflectItem(chain.Peek(5), "City");


            //then
            Assert.Equal("Dallas", actual);
        }

        [Fact]
        public void Does_variable_accessor_crawl_a_multi_link_scope_chain_correctly()
        {
            //given
            ScopeChain chain = new ScopeChain();
            var p = new Person() { Name = new Name() { First = "Charlie", Last = "Brown" } };
            chain.Push(p);
            chain.SetVariable("test", "t1");
            var p2 = new { Name = new { First = "Susie", Last = "Derkins" } };
            chain.Push(p2);
            chain.SetVariable("test", "t2");
            var a = new Address() { Line1 = "111 Main St.", Line2 = "", City = "Dallas", State = "TX", Zip = "75075" };
            chain.Push(a);
            var p3 = new Name { First = "Spider", Last = "Man" };
            chain.Push(p3);
            var p4 = new Name { First = "Luke", Last = "Skywalker" };
            chain.Push(p4);
            var p5 = new { Name = new { First = "Peter", Last = "Pan" } };
            chain.Push(p5);
            var p6 = new Name { First = "Duke", Last = "Caboom" };
            chain.Push(p6);
            var p7 = new Name { First = "Buzz", Last = "Lightyear" };
            chain.Push(p7);

            //when
            var actual = chain.AccessVariable("test");

            //then
            Assert.Equal("t2", actual);
        }

        [Fact]
        public void Does_scope_chain_pop_properly_dispose_chain_links()
        {
            //given
            ScopeChain chain = new ScopeChain();
            int cnt = 20;
            for (int i = 0; i < cnt; i++)
            {
                chain.Push(new { Index = i });
            }

            //when
            int[] actual = new int[cnt];
            for (int i = (cnt - 1); i > -1; i--)
            {
                actual[i] = (int)ReflectionHelper.Expression.ReflectItem(chain.Peek(), "Index");
                chain.Pop();
            }

            //then
            int[] expected = new int[cnt];
            for (int i = 0; i < cnt; i++)
            {
                expected[i] = i;
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Does_overreaching_back_into_scope_chain_throw_argument_exception()
        {
            //given
            var data1 = new { Name = new { First = "Charlie", Last = "Brown" } };
            var data2 = new { Name = new { First = "Super", Last = "Man" } };

            ScopeChain chain = new ScopeChain();
            chain.Push(data1);
            chain.Push(data2);

            //when
            Exception ex = Assert.Throws<ArgumentException>(() => chain.Peek(2));

            //then
            Assert.Equal("value must be < ScopeChain.Depth (Parameter 'back')", ex.Message);
        }
    }
}
