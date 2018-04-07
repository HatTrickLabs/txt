using System;

namespace HatTrick.Text.Reflection
{
    public class NoPropertyExistsException : Exception
    {
        public NoPropertyExistsException(string message) : base(message)
        {
        }
    }
}
