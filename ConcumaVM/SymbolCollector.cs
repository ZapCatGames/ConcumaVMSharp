namespace ConcumaVM
{
    internal sealed class SymbolCollector
    {
        public static readonly Dictionary<int, string> SymbolNameTable = new();

        private ConcumaEnvironment _currentEnv = new(null);

        private readonly byte[] _bytes;
        private int _current = 0;
        private int _stLoc = 0;

        public SymbolCollector(byte[] bytes)
        {
            _bytes = bytes;
        }

        public void Collect()
        {
            SymbolNameTable.Clear();

            _current += 4;
            _stLoc = BitConverter.ToInt32(_bytes, _current - 4);
            _current = _stLoc;

            while (!IsEnd())
            {
                _current += 4;
                int symbolNameLen = BitConverter.ToInt32(_bytes, _current - 4);
                string value = "";
                for (int i = 0; i < symbolNameLen; i++)
                {
                    value += (char)Advance();
                }
                _current += 4;
                int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                SymbolNameTable.Add(symbol, value);
            }

            _current = 4;

            try
            {
                while (!IsStatementEnd())
                {
                    EvaluateDeclStatement();
                }
            }
            catch (RuntimeException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"RuntimeError: {e.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void EvaluateDeclStatement()
        {
            switch (Advance())
            {
                case 0x01: // Print
                case 0x02: // If
                case 0x03: // Block
                case 0x04: // Declaration
                case 0x05: // Definition
                case 0x06: // For Loop
                case 0x07: // Break
                    SkipStatement(true);
                    break;
                case 0x08: // Function Declaration
                    _current += 4;
                    int symbol = BitConverter.ToInt32(_bytes, _current - 4);
                    _currentEnv = new ConcumaEnvironment(_currentEnv);
                    _current += 4;
                    int paramLen = BitConverter.ToInt32(_bytes, _current - 4);
                    int[] parameters = new int[paramLen];
                    for (int i = 0; i < paramLen; i++)
                    {
                        _current += 4;
                        int pSymbol = BitConverter.ToInt32(_bytes, _current - 4);
                        parameters[i] = pSymbol;
                        _currentEnv.Add(pSymbol, new Symbol.Var(false, null));
                    }
                    ConcumaEnvironment env = _currentEnv;
                    _currentEnv = _currentEnv.Exit()!;
                    _currentEnv.Add(symbol, new Symbol.Function(symbol, parameters, _current, env));
                    SkipStatement();
                    break;
                //case 0x09: //
            }
        }

        private void SkipStatement(bool previous = false)
        {
            if (previous) _current--;
            switch (Advance())
            {
                case 0x01: //Print
                    {
                        SkipExpression();
                        break;
                    }
                case 0x02: //If
                    {
                        SkipExpression();
                        SkipStatement();
                        if (Peek() == 0x00)
                        {
                            Advance();
                        }
                        else
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x03: //Block
                    {
                        _current += 4;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x04: // Declaration
                    {
                        Advance();
                        _current += 4;
                        if (Peek() != 0x00)
                        {
                            SkipExpression();
                        }
                        else
                        {
                            Advance();
                        }
                        break;
                    }
                case 0x05: //Definition
                    {
                        _current += 4;
                        SkipExpression();
                        break;
                    }
                case 0x06: //For
                    {
                        if (Peek() != 0x00) SkipStatement();
                        else Advance();
                        SkipExpression();
                        SkipStatement();
                        if (Peek() != 0x00) SkipStatement();
                        else Advance();
                        break;
                    }
                case 0x07: //Break
                    break;

                case 0x08: //Function Declaration
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        _current += len * 4;
                        SkipStatement();
                        break;
                    }
                case 0x09: //Function Call
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipExpression();
                        }
                        break;
                    }
                case 0x0A: //Return
                    {
                        if (Peek() != 0x00) SkipExpression();
                        else Advance();
                        break;
                    }
                case 0x0B: //Class
                    {
                        _current += 8;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < varLen; i++)
                        {
                            SkipStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < methodLen; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x0C: //Module
                    {
                        _current += 8;
                        int varLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < varLen; i++)
                        {
                            SkipStatement();
                        }
                        _current += 4;
                        int methodLen = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < methodLen; i++)
                        {
                            SkipStatement();
                        }
                        break;
                    }
                case 0x0D: //Import
                    {
                        _current += 4;
                        if (Peek() != 0x00) _current += 4;
                        else Advance();
                        break;
                    }
                case 0x0E: //Binary
                    {
                        _current += 4;
                        for (int i = 0; i < 2; i++)
                        {
                            _current += 4;
                        }
                        SkipStatement();
                        break;
                    }
            }
        }
        private void SkipExpression()
        {
            switch (Advance())
            {
                case 0x01: // Unary
                    Advance();
                    SkipExpression();
                    break;

                case 0x02: // Binary
                    byte b = Advance();
                    if (b == 0x00)
                    {
                        Advance();
                    }
                    else if (b == 0x01)
                    {
                        _current += 4;
                    }
                    SkipExpression();
                    SkipExpression();
                    break;

                case 0x03: // Group
                    SkipExpression();
                    break;

                case 0x04: // Literal
                    Literal();
                    break;

                case 0x05: // Var
                    _current += 4;
                    break;

                case 0x06: // Call
                    {
                        _current += 8;
                        int len = BitConverter.ToInt32(_bytes, _current - 4);
                        for (int i = 0; i < len; i++)
                        {
                            SkipExpression();
                        }
                        break;
                    }
                case 0x07: // Accessor
                    {
                        SkipExpression();
                        SkipExpression();
                        break;
                    }
                default:
                    throw new RuntimeException("Unknown expression type.");
            }
        }

        private bool IsStatementEnd() => _current >= _stLoc;

        private bool IsEnd() => _current >= _bytes.Length;

        private byte Peek() => _bytes[_current];

        private byte Advance()
        {
            if (IsEnd()) return 0x00;
            return _bytes[_current++];
        }
    }
}
