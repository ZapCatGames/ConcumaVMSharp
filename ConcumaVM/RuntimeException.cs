namespace ConcumaVM
{
    public class RuntimeException : Exception
    {
        public RuntimeException() : base()
        {
        }

        public RuntimeException(string? message) : base(message)
        {
        }

        public RuntimeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
