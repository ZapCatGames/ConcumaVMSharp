namespace ConcumaRuntimeFramework
{
    public sealed class VM
    {
        private readonly byte[] _bytes;
        private int _current = 0;

        public VM(byte[] bytes)
        {
            _bytes = bytes;
        }

        public void Run()
        {
            while (!IsEnd())
            {
                EvaluateStatement();
            }
        }

        private void EvaluateStatement()
        {
            switch (Advance())
            {
                case 0x01: //Print
                    {
                        Console.WriteLine(EvaluateExpression());
                        break;
                    }
            }
        }

        private object? EvaluateExpression()
        {
            switch (Advance())
            {
                case 0x01: // Unary
                    return Unary();
                case 0x02:
                    return Binary();
                case 0x03:
                    return EvaluateExpression();
                case 0x04:
                    return Literal();
                default:
                    return Literal();
            }
        }

        private object Unary()
        {
            byte op = Advance();
            object? right = EvaluateExpression();

            switch (op)
            {
                case 0x01: // !
                    {
                        if (right is bool b)
                            return !b;

                        if (right is int i)
                        {
                            if (i == 0) return 1;
                            else return 0;
                        }

                        if (right is double d)
                        {
                            if (d == 0) return 1;
                            else return 0;
                        }

                        break;
                    }
                case 0x02: // -
                    {
                        if (right is int i)
                            return -i;

                        if (right is double d)
                            return -d;

                        break;
                    }
            }

            throw new Exception();
        }

        private object Binary()
        {
            byte op = Advance();
            object? left = EvaluateExpression();
            object? right = EvaluateExpression();

            switch (op)
            {
                case 0x01: // +
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia + ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db + ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic + dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd + dd2;
                        }

                        if (left is string s && right is string s2)
                        {
                            return s + s2;
                        }

                        break;
                    }
                case 0x02: // -
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia - ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db - ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic - dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd - dd2;
                        }

                        break;
                    }
                case 0x03: // *
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia * ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db * ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic * dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd * dd2;
                        }

                        break;
                    }
                case 0x04: // /
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia / ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db / ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic / dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd / dd2;
                        }

                        break;
                    }
                case 0x05: // ==
                    {
                        return left == right;
                    }
                case 0x06: // !=
                    {
                        return left != right;
                    }
                case 0x07: // <
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia < ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db < ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic < dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd < dd2;
                        }

                        break;
                    }
                case 0x08: // <=
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia <= ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db <= ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic <= dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd <= dd2;
                        }

                        break;
                    }
                case 0x09: // >
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia > ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db > ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic > dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd > dd2;
                        }

                        break;
                    }
                case 0x0A: // >=
                    {
                        if (left is int ia && right is int ia2)
                        {
                            return ia >= ia2;
                        }

                        if (left is double db && right is int ib)
                        {
                            return db >= ib;
                        }

                        if (left is int ic && right is double dc)
                        {
                            return ic >= dc;
                        }

                        if (left is double dd && right is double dd2)
                        {
                            return dd >= dd2;
                        }

                        break;
                    }
            }

            throw new Exception();
        }

        private object? Literal()
        {
            byte type = Advance();

            switch (type)
            {
                case 0x00:
                    return null;
                case 0x01:
                    return Advance() == 0x01;
                case 0x02:
                    _current += 4;
                    return BitConverter.ToInt32(_bytes, _current - 4);
                case 0x03:
                    _current += 8;
                    return BitConverter.ToDouble(_bytes, _current - 8);
                case 0x04:
                    byte length = Advance();
                    string value = "";
                    for (int i = 0; i < length; i++)
                    {
                        value += (char)Advance();
                    }
                    return value;
            }

            throw new Exception();
        }

        private bool IsEnd() => _current >= _bytes.Length;

        private byte Peek() => _bytes[_current];
        private byte Advance()
        {
            if (IsEnd()) return 0x00;
            return _bytes[_current++];
        }
    }
}
