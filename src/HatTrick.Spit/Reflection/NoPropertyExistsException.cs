using System;

namespace HatTrick.Spit.Reflection
{
    public class NoPropertyExistsException : Exception
    {
        public NoPropertyExistsException(string message) : base(message)
        {
        }
    }
}
