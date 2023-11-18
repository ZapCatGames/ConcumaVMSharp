namespace ConcumaVM
{
    public class BreakException : Exception
    {
        public BreakException() : base()
        {
        }

        public BreakException(string? message) : base(message)
        {
        }

        public BreakException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
