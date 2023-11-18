namespace ConcumaVM
{
    public class ConcumaFunction
    {
        private readonly int[] _parameters;
        private readonly int _action;

        public ConcumaFunction(int[] parameters, int action)
        {
            _parameters = parameters;
            _action = action;
        }

        public int Call(object?[] parameters, ConcumaEnvironment env)
        {
            if (parameters.Length != _parameters.Length) throw new RuntimeException("Invalid number of function arguments.");

            for (int i = 0; i < _parameters.Length; i++)
            {
                env.Assign(_parameters[i], parameters[i] is Symbol s ? s : new Symbol.Var(false, parameters[i]));
            }

            return _action;
        }
    }
}
