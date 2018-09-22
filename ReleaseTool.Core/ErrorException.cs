using System;

namespace ReleaseTool
{
    public class ErrorException : Exception
    {
        public ErrorException(string message) : base(message) { }
    }
}
