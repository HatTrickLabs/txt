using System;

namespace HatTrick.Linx.Reflection
{
    public class NoPropertyExistsException : Exception
    {
        public NoPropertyExistsException(string message) : base(message)
        {
        }
    }
}
