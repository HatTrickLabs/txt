using System;

namespace HatTrick.Reflection
{
    public class NoPropertyExistsException : Exception
    {
        public NoPropertyExistsException(string message) : base(message)
        {
        }
    }
}
