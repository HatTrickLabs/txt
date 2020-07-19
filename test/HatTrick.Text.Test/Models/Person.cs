using System;
using System.Collections.Generic;
using System.Text;

namespace HatTrick.Text.Test.Models
{
    public class Person
    {
        public int Age;

        public Name Name { get; set; }
        public Address Address { get; set; }
    }
}
