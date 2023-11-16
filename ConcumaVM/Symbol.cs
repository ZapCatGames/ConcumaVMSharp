namespace ConcumaVM
{
    public abstract class Symbol
    {
        public object? Value { get; set; }

        public Symbol(object? value)
        {
            Value = value;
        }

        public sealed class Var : Symbol
        {
            public Var(bool @const, object? value) : base(value)
            {
                Const = @const;
            }

            public bool Const { get; }
        }
    }
}
