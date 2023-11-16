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

        public sealed class Function : Symbol
        {
            public Function(int paramLen, int[] parameters, int action, ConcumaEnvironment env) : base(null)
            {
                ParameterLength = paramLen;
                Parameters = parameters;
                Action = action;
                Environment = env;
            }

            public int ParameterLength { get; }
            public int[] Parameters { get; }
            public int Action { get; }
            public ConcumaEnvironment Environment { get; }
        }
    }
}
