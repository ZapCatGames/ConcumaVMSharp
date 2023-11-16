namespace ConcumaRuntimeFramework
{
    public class ReturnException : Exception
    {
        public object? Value { get; }

        public ReturnException(object? value)
        {
            Value = value;
        }
    }
}
