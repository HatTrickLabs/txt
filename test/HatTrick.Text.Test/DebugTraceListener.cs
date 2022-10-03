using System;

namespace HatTrick.Text.Test
{
    public class DebugTraceListener : System.Diagnostics.TraceListener
    {
        public Action<string> Push;

        public override void Write(string message)
        {
            this.Push?.Invoke(message);
        }

        public override void WriteLine(string message)
        {
            this.Push?.Invoke(message);
        }
    }
}
