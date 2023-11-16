namespace ConcumaVM
{
    public sealed class ConcumaEnvironment
    {
        private readonly Dictionary<int, Symbol> _symbols = new();
        private readonly ConcumaEnvironment? _parent;

        public ConcumaEnvironment(ConcumaEnvironment? parent)
        {
            _parent = parent;
        }

        public void Add(int addr, Symbol symbol) => _symbols.Add(addr, symbol);
        public Symbol Find(int addr)
        {
            if (_symbols.ContainsKey(addr))
            {
                return _symbols[addr];
            }

            if (_parent is null) throw new RuntimeException("Tried to access unknown variable.");

            return _parent.Find(addr);
        }
        public ConcumaEnvironment? Exit() => _parent;
    }
}
