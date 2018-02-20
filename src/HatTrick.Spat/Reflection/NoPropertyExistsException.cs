using System;

namespace HatTrick.Spat.Reflection
{
    public class NoPropertyExistsException : Exception
    {
        public NoPropertyExistsException(string message) : base(message)
        {
        }
    }
}
