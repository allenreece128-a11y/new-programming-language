using System;
using System.Collections.Generic;

namespace NovaScript.Core
{
    public class Environment
    {
        private readonly Environment? _enclosing;
        private readonly Dictionary<string, (object? Value, bool IsMutable)> _values = new();

        public Environment()
        {
            _enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            _enclosing = enclosing;
        }

        public void Define(string name, object? value, bool isMutable)
        {
            _values[name] = (value, isMutable);
        }

        public object? Get(Token name)
        {
            if (_values.ContainsKey(name.Value))
            {
                return _values[name.Value].Value;
            }

            // Implicit 'this' access for structures
            if (_values.ContainsKey("this"))
            {
                var thisObj = _values["this"].Value;
                // We use dynamic or cast to NovaInstance to avoid circular dependency if possible, 
                // but since they are in the same namespace it's fine.
                if (thisObj is NovaInstance instance)
                {
                    try { return instance.Get(name); } catch { /* ignore and move to enclosing */ }
                }
            }

            if (_enclosing != null) return _enclosing.Get(name);

            throw new Exception($"Undefined variable '{name.Value}' at line {name.Line}.");
        }

        public void Assign(Token name, object? value)
        {
            if (_values.ContainsKey(name.Value))
            {
                if (!_values[name.Value].IsMutable)
                {
                    throw new Exception($"Cannot assign to immutable variable '{name.Value}' at line {name.Line}.");
                }
                _values[name.Value] = (value, true);
                return;
            }

            // Implicit 'this' access for structures
            if (_values.ContainsKey("this"))
            {
                var thisObj = _values["this"].Value;
                if (thisObj is NovaInstance instance)
                {
                    try 
                    { 
                        instance.Set(name, value); 
                        return;
                    } catch { /* ignore and move to enclosing */ }
                }
            }

            if (_enclosing != null)
            {
                _enclosing.Assign(name, value);
                return;
            }

            throw new Exception($"Undefined variable '{name.Value}' at line {name.Line}.");
        }

        public Dictionary<string, (object? Value, bool IsMutable)> GetAllVariables()
        {
            return new Dictionary<string, (object? Value, bool IsMutable)>(_values);
        }

        public Environment? GetEnclosing() => _enclosing;
    }
}
