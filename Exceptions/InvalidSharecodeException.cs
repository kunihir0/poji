using System;

namespace poji.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid share code is encountered.
    /// </summary>
    public class InvalidSharecodeException : Exception
    {
        public InvalidSharecodeException(string message = "Invalid share code") : base(message) { }
    }
}