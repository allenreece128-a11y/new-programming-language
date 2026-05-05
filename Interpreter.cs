using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NovaScript.Core
{
    public interface INovaCallable
    {
        int Arity();
        object? Call(Interpreter interpreter, List<object?> arguments);
    }

    public class NovaFunction : INovaCallable
    {
        private readonly FunctionStmt _declaration;
        private readonly Environment _closure;

        public NovaFunction(FunctionStmt declaration, Environment closure)
        {
            _declaration = declaration;
            _closure = closure;
        }

        public int Arity() => _declaration.Params.Count;

        public NovaFunction Bind(NovaInstance instance)
        {
            var environment = new Environment(_closure);
            environment.Define("this", instance, false);
            return new NovaFunction(_declaration, environment);
        }

        public object? Call(Interpreter interpreter, List<object?> arguments)
        {
            var environment = new Environment(_closure);
            for (int i = 0; i < _declaration.Params.Count; i++)
            {
                environment.Define(_declaration.Params[i].Name.Value, arguments[i], _declaration.Params[i].IsMutable);
            }

            interpreter.PushCallFrame(_declaration.Name.Value);
            try
            {
                interpreter.ExecuteBlock(_declaration.Body, environment);
            }
            catch (ReturnException e)
            {
                return e.Value;
            }
            finally
            {
                interpreter.PopCallFrame();
            }

            return null;
        }

        public override string ToString() => $"<fn {_declaration.Name.Value}>";
    }

    public delegate object? NativeFn(Interpreter interpreter, List<object?> arguments);

    public class NovaNativeFunction : INovaCallable
    {
        private readonly int _arity;
        private readonly NativeFn _function;

        public NovaNativeFunction(int arity, NativeFn function)
        {
            _arity = arity;
            _function = function;
        }

        public int Arity() => _arity;
        public object? Call(Interpreter interpreter, List<object?> arguments) => _function(interpreter, arguments);
        public override string ToString() => "<native fn>";
    }

    public class ReturnException : Exception
    {
        public object? Value { get; }
        public ReturnException(object? value) : base() => Value = value;
    }

    public class BreakException : Exception { }
    public class ContinueException : Exception { }

    public sealed class NovaList
    {
        private readonly List<object?> _items = new();
        public int Count => _items.Count;

        public void Add(object? value) => _items.Add(value);

        public void Insert(int index, object? value)
        {
            if (index < 0) index = 0;
            if (index > _items.Count) index = _items.Count;
            _items.Insert(index, value);
        }

        public object? Get(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new Exception($"List index out of range: {index} (size {_items.Count}).");
            }

            return _items[index];
        }

        public void Set(int index, object? value)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new Exception($"List index out of range: {index} (size {_items.Count}).");
            }

            _items[index] = value;
        }

        public object? RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new Exception($"List index out of range: {index} (size {_items.Count}).");
            }

            object? removed = _items[index];
            _items.RemoveAt(index);
            return removed;
        }

        public object? Pop()
        {
            if (_items.Count == 0) return null;
            int last = _items.Count - 1;
            object? value = _items[last];
            _items.RemoveAt(last);
            return value;
        }

        public List<object?> Snapshot() => new(_items);
    }

    public sealed class NovaMap
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public int Count => _values.Count;
        public void Set(string key, object? value) => _values[key] = value;
        public bool Has(string key) => _values.ContainsKey(key);
        public object? Remove(string key)
        {
            if (!_values.TryGetValue(key, out object? existing)) return null;
            _values.Remove(key);
            return existing;
        }

        public object? Get(string key)
        {
            if (_values.TryGetValue(key, out object? value)) return value;
            return null;
        }

        public List<string> Keys() => _values.Keys.ToList();
    }

    public class NovaStructure : INovaCallable
    {
        public string Name { get; }
        public List<VarStmt> Properties { get; }
        public Dictionary<string, NovaFunction> Methods { get; }

        public NovaStructure(string name, List<VarStmt> properties, Dictionary<string, NovaFunction> methods)
        {
            Name = name;
            Properties = properties;
            Methods = methods;
        }

        public int Arity()
        {
            if (Methods.ContainsKey("init")) return Methods["init"].Arity();
            return 0;
        }

        public object? Call(Interpreter interpreter, List<object?> arguments)
        {
            var instance = new NovaInstance(this);
            if (Methods.ContainsKey("init"))
            {
                Methods["init"].Bind(instance).Call(interpreter, arguments);
            }
            return instance;
        }

        public override string ToString() => Name;
    }

    public class NovaInstance
    {
        private readonly NovaStructure _structure;
        private readonly Dictionary<string, object?> _fields = new();

        public NovaInstance(NovaStructure structure)
        {
            _structure = structure;
            foreach (var prop in structure.Properties)
            {
                _fields[prop.Name.Value] = null; // Default value
            }
        }

        public object? Get(Token name)
        {
            if (_fields.ContainsKey(name.Value)) return _fields[name.Value];
            if (_structure.Methods.ContainsKey(name.Value)) return _structure.Methods[name.Value].Bind(this);

            throw new Exception($"Undefined property '{name.Value}' at line {name.Line}.");
        }

        public void Set(Token name, object? value)
        {
            if (_fields.ContainsKey(name.Value))
            {
                _fields[name.Value] = value;
                return;
            }
            throw new Exception($"Undefined property '{name.Value}' at line {name.Line}.");
        }

        public override string ToString() => $"<instance of {_structure.Name}>";
    }

    public class Interpreter
    {
        public Environment Globals { get; } = new();
        private Environment _environment;
        private readonly NovaGraphicsRuntime _graphics = new();
        private int _loopDepth;
        private readonly List<string> _callStack = new();
        private readonly HashSet<string> _loadedModules = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _modulesInProgress = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _sourceContextStack = new();

        public Interpreter()
        {
            _environment = Globals;

            // Core native functions
            Globals.Define("input", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                Console.Write(arguments[0]?.ToString());
                return Console.ReadLine();
            }), false);

            Globals.Define("toNum", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                if (double.TryParse(arguments[0]?.ToString(), out double result)) return result;
                return 0.0;
            }), false);

            // Container types: list + map
            Globals.Define("list_new", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return new NovaList();
            }), false);

            Globals.Define("list_len", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                return (double)AsList(arguments[0]).Count;
            }), false);

            Globals.Define("list_push", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var list = AsList(arguments[0]);
                list.Add(arguments[1]);
                return (double)list.Count;
            }), false);

            Globals.Define("list_insert", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                var list = AsList(arguments[0]);
                list.Insert(ToInt(arguments[1], "index"), arguments[2]);
                return null;
            }), false);

            Globals.Define("list_get", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var list = AsList(arguments[0]);
                return list.Get(ToInt(arguments[1], "index"));
            }), false);

            Globals.Define("list_set", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                var list = AsList(arguments[0]);
                list.Set(ToInt(arguments[1], "index"), arguments[2]);
                return arguments[2];
            }), false);

            Globals.Define("list_remove", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var list = AsList(arguments[0]);
                return list.RemoveAt(ToInt(arguments[1], "index"));
            }), false);

            Globals.Define("list_pop", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                return AsList(arguments[0]).Pop();
            }), false);

            Globals.Define("map_new", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return new NovaMap();
            }), false);

            Globals.Define("map_len", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                return (double)AsMap(arguments[0]).Count;
            }), false);

            Globals.Define("map_set", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                var map = AsMap(arguments[0]);
                string key = ToMapKey(arguments[1]);
                map.Set(key, arguments[2]);
                return arguments[2];
            }), false);

            Globals.Define("map_get", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var map = AsMap(arguments[0]);
                string key = ToMapKey(arguments[1]);
                return map.Get(key);
            }), false);

            Globals.Define("map_has", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var map = AsMap(arguments[0]);
                string key = ToMapKey(arguments[1]);
                return map.Has(key);
            }), false);

            Globals.Define("map_remove", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var map = AsMap(arguments[0]);
                string key = ToMapKey(arguments[1]);
                return map.Remove(key);
            }), false);

            Globals.Define("map_keys", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                var map = AsMap(arguments[0]);
                var list = new NovaList();
                foreach (string key in map.Keys())
                {
                    list.Add(key);
                }
                return list;
            }), false);

            // Graphics + OpenGL
            Globals.Define("gfx_init", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                _graphics.Init(ToInt(arguments[0], "width"), ToInt(arguments[1], "height"), ToStringValue(arguments[2]));
                return null;
            }), false);

            Globals.Define("gfx_poll", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return false;
                return _graphics.Poll();
            }), false);

            Globals.Define("gfx_running", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return _graphics.IsInitialized && _graphics.IsRunning;
            }), false);

            Globals.Define("gfx_present", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return false;
                return _graphics.Present();
            }), false);

            Globals.Define("gfx_clear", new NovaNativeFunction(4, (interpreter, arguments) =>
            {
                _graphics.ClearCanvas(
                    ToInt(arguments[0], "r"),
                    ToInt(arguments[1], "g"),
                    ToInt(arguments[2], "b"),
                    ToInt(arguments[3], "a"));
                return null;
            }), false);

            Globals.Define("gfx_set_clear_color", new NovaNativeFunction(4, (interpreter, arguments) =>
            {
                _graphics.SetScreenClearColor(
                    ToInt(arguments[0], "r"),
                    ToInt(arguments[1], "g"),
                    ToInt(arguments[2], "b"),
                    ToInt(arguments[3], "a"));
                return null;
            }), false);

            Globals.Define("gfx_pixel", new NovaNativeFunction(6, (interpreter, arguments) =>
            {
                _graphics.Pixel(
                    ToInt(arguments[0], "x"),
                    ToInt(arguments[1], "y"),
                    ToInt(arguments[2], "r"),
                    ToInt(arguments[3], "g"),
                    ToInt(arguments[4], "b"),
                    ToInt(arguments[5], "a"));
                return null;
            }), false);

            Globals.Define("gfx_line", new NovaNativeFunction(8, (interpreter, arguments) =>
            {
                _graphics.Line(
                    ToInt(arguments[0], "x1"),
                    ToInt(arguments[1], "y1"),
                    ToInt(arguments[2], "x2"),
                    ToInt(arguments[3], "y2"),
                    ToInt(arguments[4], "r"),
                    ToInt(arguments[5], "g"),
                    ToInt(arguments[6], "b"),
                    ToInt(arguments[7], "a"));
                return null;
            }), false);

            Globals.Define("gfx_rect", new NovaNativeFunction(8, (interpreter, arguments) =>
            {
                _graphics.Rect(
                    ToInt(arguments[0], "x"),
                    ToInt(arguments[1], "y"),
                    ToInt(arguments[2], "w"),
                    ToInt(arguments[3], "h"),
                    ToInt(arguments[4], "r"),
                    ToInt(arguments[5], "g"),
                    ToInt(arguments[6], "b"),
                    ToInt(arguments[7], "a"),
                    true);
                return null;
            }), false);

            Globals.Define("gfx_rect_line", new NovaNativeFunction(8, (interpreter, arguments) =>
            {
                _graphics.Rect(
                    ToInt(arguments[0], "x"),
                    ToInt(arguments[1], "y"),
                    ToInt(arguments[2], "w"),
                    ToInt(arguments[3], "h"),
                    ToInt(arguments[4], "r"),
                    ToInt(arguments[5], "g"),
                    ToInt(arguments[6], "b"),
                    ToInt(arguments[7], "a"),
                    false);
                return null;
            }), false);

            Globals.Define("gfx_text", new NovaNativeFunction(8, (interpreter, arguments) =>
            {
                _graphics.Text(
                    ToInt(arguments[0], "x"),
                    ToInt(arguments[1], "y"),
                    ToStringValue(arguments[2]),
                    ToInt(arguments[3], "scale"),
                    ToInt(arguments[4], "r"),
                    ToInt(arguments[5], "g"),
                    ToInt(arguments[6], "b"),
                    ToInt(arguments[7], "a"));
                return null;
            }), false);

            Globals.Define("gfx_image", new NovaNativeFunction(5, (interpreter, arguments) =>
            {
                _graphics.Image(
                    ToStringValue(arguments[0]),
                    ToInt(arguments[1], "x"),
                    ToInt(arguments[2], "y"),
                    ToInt(arguments[3], "w"),
                    ToInt(arguments[4], "h"));
                return null;
            }), false);

            Globals.Define("gfx_resize", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                _graphics.Resize(ToInt(arguments[0], "width"), ToInt(arguments[1], "height"));
                return null;
            }), false);

            Globals.Define("gfx_set_title", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.SetTitle(ToStringValue(arguments[0]));
                return null;
            }), false);

            Globals.Define("gfx_set_vsync", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.SetVSync(ToBool(arguments[0]));
                return null;
            }), false);

            Globals.Define("gfx_width", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return 0.0;
                return (double)_graphics.Width;
            }), false);

            Globals.Define("gfx_height", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return 0.0;
                return (double)_graphics.Height;
            }), false);

            Globals.Define("gfx_close", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                _graphics.Close();
                return null;
            }), false);

            Globals.Define("gfx_quit", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                _graphics.Dispose();
                return null;
            }), false);

            // Chart system
            Globals.Define("chart_config", new NovaNativeFunction(4, (interpreter, arguments) =>
            {
                _graphics.ChartConfigure(
                    ToStringValue(arguments[0]),
                    ToStringValue(arguments[1]),
                    ToStringValue(arguments[2]),
                    ToStringValue(arguments[3]));
                return null;
            }), false);

            Globals.Define("chart_set_type", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.ChartSetType(ToStringValue(arguments[0]));
                return null;
            }), false);

            Globals.Define("chart_set_title", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.ChartSetTitle(ToStringValue(arguments[0]));
                return null;
            }), false);

            Globals.Define("chart_set_variables", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                _graphics.ChartSetVariables(ToStringValue(arguments[0]), ToStringValue(arguments[1]));
                return null;
            }), false);

            Globals.Define("chart_add_point", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                _graphics.ChartAddPoint(
                    ToStringValue(arguments[0]),
                    ToStringValue(arguments[1]),
                    ToNumber(arguments[2], "yValue"));
                return null;
            }), false);

            Globals.Define("chart_add_xy", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                _graphics.ChartAddPoint(
                    "Series 1",
                    ToStringValue(arguments[0]),
                    ToNumber(arguments[1], "yValue"));
                return null;
            }), false);

            Globals.Define("chart_add_slice", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                _graphics.ChartAddSlice(
                    ToStringValue(arguments[0]),
                    ToNumber(arguments[1], "value"));
                return null;
            }), false);

            Globals.Define("chart_clear_data", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                _graphics.ChartClearData();
                return null;
            }), false);

            Globals.Define("chart_render", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                _graphics.ChartRender();
                return null;
            }), false);

            // Input + timing helpers
            Globals.Define("input_key_down", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return false;
                return _graphics.IsKeyDown(ToStringValue(arguments[0]));
            }), false);

            Globals.Define("input_mouse_x", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return 0.0;
                return _graphics.MouseX();
            }), false);

            Globals.Define("input_mouse_y", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return 0.0;
                return _graphics.MouseY();
            }), false);

            Globals.Define("input_mouse_down", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                if (!_graphics.IsInitialized) return false;
                return _graphics.IsMouseDown(ToInt(arguments[0], "button"));
            }), false);

            Globals.Define("time_ticks", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return _graphics.TicksMs;
            }), false);

            Globals.Define("time_delta", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return _graphics.DeltaMs;
            }), false);

            Globals.Define("time_sleep", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                int ms = ToInt(arguments[0], "ms");
                if (ms < 0) ms = 0;
                System.Threading.Thread.Sleep(ms);
                return null;
            }), false);

            // Current date helper: returns e.g. "4/30/2026"
            Globals.Define("time_now", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                return DateTime.Now.ToString("M/d/yyyy");
            }), false);

            // Return the large ASCII art as a NovaScript list
            Globals.Define("get_sample_art", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                var list = new NovaList();
                list.Add("█▓█░░░                                          ░█▒                                             ░░▒█");
                list.Add("░█▓░░▓▓░░                                      ░▓▓▒▒                                         ░▒▓▓▓█▒");
                list.Add(" ░█▒▓░░░▒█▒░                                  ░█░▓░▒░                                    ░░▓▓░░▒▒▒▒░");
                list.Add("  ░█░▓▒░   ░▓▓░░░                            ░█░ ▓░ ▓░                               ░░▒█▒   ▒▓░░▒░ ");
                list.Add("   ░▓░░▓░     ░░▓▓░░                        ░▓░  ▓░ ░█░                           ░▒▓▓░    ░▓░ ░▓░  ");
                list.Add("    ░▓  ░█░      ░░▒█▒░                     ▓░   ▓░   █░                       ░▓▓░░░    ░█░  ░▓░   ");
                list.Add("     ▒▓   ░▓░         ░▓▓░░               ░▓░    ▓░    ▓░                 ░░▒█▒░       ░▓▒   ░▓░    ");
                list.Add("      ▒▒    ▒▓           ░▒▓▓░            ▓▒     ▓░    ░▓░              ▒▓▒░          ▓▒    ░▓░     ");
                list.Add("       ▒▒    ░▓▒            ░░▒█▒        ▒▒      ▓░     ░█          ░▓▓░░░          ▒▓░    ░▓░      ");
                list.Add("       ░▒░      ▓▒░             ░░▓▓░░░░░▒       ▓░     ░░▓░    ░▒█▒░            ░░█░     ░█░       ");
                list.Add("        ░▓░      ░▓░                ░▒▓▓▓░       ▓░       ░▓░▒▓▒░               ░▓░      ░█░        ");
                list.Add("         ░█░       ░█░                ░█▒▓█░░    ▓░      ░▓█▒░                ░█░░      ░▓░░        ");
                list.Add("          ░▓░        ▒▓░             ░▓░█░░ ░▓▓░░▓░ ░░▒▓▒░▓░▒▒              ░▓▒░       ░▒░          ");
                list.Add("           ░▓░         ▒▒           ░▓░ ░▓░    ░▒█▒▓▒░░  ░▒░ ▓▒            ▒▓          ▒▒           ");
                list.Add("            ░█          ░▓▒        ░▓░   ▓░   ░▓██░░▓█░ ░█    ▓░         ▒▓           ▒▒            ");
                list.Add("             ░█ ░        ░░█░░    ░█░    ░▓▒▓▒   ▓░   ░▒█▓░░  ░▓░     ░░█░           ░▓             ");
                list.Add("              ░▓░           ░▓░  ░█░  ░▒█▒▒▒     ▓░    ░▓░░▒▓▒░░▓    ░█░            ░▓░             ");
                list.Add("               ░▓             ░▓░▓░░▓█░    ▓░    ▓░    ▓░     ░█▓█░ ▓▒             ░█░               ");
                list.Add("                ▒▒            ░░▓█▓▓▓▓▓▓▓▓▓▓█▓▓▓▓█▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓██▒░            ░█░                ");
                list.Add("                 ▒▒        ░▒▓▒▓▒█▓▒        ▒░░░▒▓░▒▒░▓░        ▒▓█▓░░▒█░░       ░▓░                ");
                list.Add("                  ▒▒   ░░▓█░░ ▒▒░█░░█░     ░░█▓░ ▓░ ░▓▒░░     ░█░ █░▓    ░█▓░   ░▓░                  ");
                list.Add("                   ▓▒▒▓▒░    ░▓ ░█░  ░▓░  ░░▓▒▒░ ▓░ ░▓░▒░░░ ░▓░   █░░▓      ░▒▓▒▓░                   ");
                list.Add("                ░▒▓▒▓░▒▓▓░░ ░▓░ ░█░   ░░█░▓░░░█░ ▓░░█░░░░▓░█░░    █░ ▒▓  ░░▓▓▒░▓▒▓▓░░               ");
                list.Add("            ░░▓█░   ░█░   ░▒█▓░░░█░    ░▓▒▓░  ░▓░▓░▒▒░  ░▓▒▓░░    █░░░██▒░   ░▒▒░  ░░█▒░            ");
                list.Add("        ░░▒▓▒░        █░  ░█░  ░▒█▓▒░░▒▒░░░▓▒  ▓░▓░█░  ▒▓░░░░▒░░▒▓█▒░  ▒░░   ▒▒░       ░▓▓▒░        ");
                list.Add("     ░░█▒░░           ░▓░░█░    ░█░░▒▒▒█▓░░ ░▓▒░▓▓▓░ ░▓░  ░▓█▒▒▒░ █░    ▓░  ░▓░           ░░▓▓░     ");
                list.Add(" ░░▓█░░                ░▓▓      ░█░▒░     ░▒█▓░█▒█▓░█░▓█▒░░    ░▒░█░     ▓░░▓                 ░▒█▒░ ");
                list.Add("██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█");
                list.Add("░▒█▒░                  ▓░░▓     ░█▓░░    ░░▒█▓░░▓██▓░░▓█▒░░    ░░▓█░ ░   ░▓▓                  ▒█▒░░ ");
                list.Add("    ░▓▓░░             ▓░  ░▓░   ░█░░▓░▒█▓░░  ░▒▒▓▓▓░▒▒░  ░░▓█▒░▒▒░▓░    ░▓░░█            ░░▓▓░      ");
                list.Add("       ░░▓▓░         ▒░    ▒▒░ ░▒█▓▒░░▒░░░  ▒▓░█░▓░█░░▓▒  ░░░▒▒░▒▓█▒░░  █░  ░█░       ░▒▓▒░         ");
                list.Add("           ░▒█▒     ▒▒░   ░▒█▓░░░█░  ░░▒▒░░▓░ ░▒░▓░▒▒  ░▓░░░▒░░   ▓░░░▓█▒░   ░▓░  ░░█▒░░            ");
                list.Add("               ░▓▓░▒▓░▒▓▓░░  ▓▒ ░█░    ░░▓▒░░░█░ ▓░░█░░░░░▓░░     ▓░ ░▓░ ░░▓█▒▒▓▒█░░                ");
                list.Add("                  ▒▓▓▓░░     ░▓░░█░   ░▓░░░▒░▒░░ ▓░ ░▓░░▒░░░▓░    ▓░ ▒░    ░▒▓▒▒▒                   ");
                list.Add("                 ░▓   ░▒█░    ░▓░█░  ▓▒░ ░░░░▓░░ ▓░ ░▓▓░░░░ ░▒▓   ▓░▒▒  ░▓▓░░   ▒▒                  ");
                list.Add("                ░▓░       ░▓▓░░░▓█░▒▓░     ░▓░▒▒░▓░░▒▒▓░░     ░▓▒ ▓▒▓▒█▒░        ▒▒                 ");
                list.Add("                █░           ░▒▓▒██▒▒▒▒▒▒▒▒▓▓▒▒▒▓█▒▓▒▒▓▓▒▒▒▒▒▒▒▒▒██▓░░            ▓░                ");
                list.Add("              ░█░              ░█░██░░░░░░░▓░░░░░▓░░░░░▓░░░░░░▓█░░▓█░             ░▓░               ");
                list.Add("             ░█░             ░▓░  ░▓░▒█▒░░▓░     ▓░    ░▓░▒█▒░  ░█░ ░▓░            ░█░              ");
                list.Add("            ░▓░            ░▓▒░    ░▓  ░░▓█▒░    ▓░   ░▓█░░    ░█░   ░▒▓░           ░█░             ");
                list.Add("            ▓░            ▒▓░       ░▓  ░█░░░▓▓░ ▓░▒█░░░░█░   ░▓░      ░▓▒           ░▓             ");
                list.Add("           ▓▒           ▒▓░          ▒▒░▒▒░   ░▒██▒░    ░▒░  ░▓░         ░▓▒          ░▓            ");
                list.Add("          ▒▒          ░▓░            ░▒▒█░░░▓▓░░ ▓░░▓▓░░ ░█░░▓░            ░▓░░        ░▓           ");
                list.Add("         ▒▒         ░█░               ░▓▒█▒      ▓░    ░█▓░▒▓░               ░█░        ░▓          ");
                list.Add("       ░░▓░       ░▓▒              ░░▓▓░▓░       ▓░       ░██▒░                ▒▓░       ░▒░        ");
                list.Add("       ░▓░       ▓▒░            ░▓▓░░    ▓░      ▓░      ░▒▒ ░░▓▓░              ░▒▓      ░▒▒        ");
                list.Add("     ░░▓      ░▒▓░         ░░▒█▒ ░        █░     ▓░      ░▓░      ▒█▒░░           ░▓▒░     ▓░       ");
                list.Add("     ░▓      ░█░░       ░░▓▓░░            ░▓░    ▓░     ░▓          ░░▓▓░░         ░░▓░     ▓░      ");
                list.Add("    ░▓░    ░▓░       ░▓▓▒░                 ░▓░   ▓░    ░▓               ░░▓▓░         ░▓░    ▓░     ");
                list.Add("   ░█░  ░░█░░    ░░█▓░                      ░█   ▓░   ░▓░                   ░▒█░░      ░░█░   ▓░    ");
                list.Add("  ░█░  ░▓▒░   ░▓▓░░                          ░█░ ▓░  ░▓░                       ░░▓▓░░    ░▒▓░ ░█░   ");
                list.Add(" ░▓░  ▒▓░ ░▒█▒░                               ░▓ ▓░ ░█░░                          ░░▒█▒░   ░▓▒ ░█░  ");
                list.Add("░▓░░░▓░░▓▓░                                    ░▒▓░░█░                                 ░▓█░░░░▓▒░█░ ");
                list.Add("▒▒░█▓▓▒░                                        ▒█░▓░                                     ░▒▓▓░░█░▓ ");
                list.Add("▓█▒░░                                            ▓█░                                        ░ ░▒█▒█▓");
                return list;
            }), false);

            // Draw a list of strings centered on the screen using the bitmap font
            Globals.Define("draw_ascii_art_center", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                var art = AsList(arguments[0]);
                int scale = ToInt(arguments[1], "scale");
                var lines = art.Snapshot();
                int count = lines.Count;
                int charW = 6 * Math.Max(1, scale);
                int charH = 8 * Math.Max(1, scale);
                int totalH = count * charH;
                int startY = (_graphics.Height - totalH) / 2;

                for (int i = 0; i < count; i++)
                {
                    var o = lines[i];
                    string line = o?.ToString() ?? string.Empty;
                    int len = line.Length;
                    int textW = len * charW;
                    int startX = (_graphics.Width - textW) / 2;
                    _graphics.Text(startX, startY + i * charH, line, scale, 80, 255, 200, 255);
                }

                return null;
            }), false);

            // Shader API
            Globals.Define("shader_create", new NovaNativeFunction(2, (interpreter, arguments) =>
            {
                return (double)_graphics.ShaderCreate(ToStringValue(arguments[0]), ToStringValue(arguments[1]));
            }), false);

            Globals.Define("shader_use", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.ShaderUse(ToInt(arguments[0], "shaderHandle"));
                return null;
            }), false);

            Globals.Define("shader_use_default", new NovaNativeFunction(0, (interpreter, arguments) =>
            {
                _graphics.ShaderUseDefault();
                return null;
            }), false);

            Globals.Define("shader_destroy", new NovaNativeFunction(1, (interpreter, arguments) =>
            {
                _graphics.ShaderDestroy(ToInt(arguments[0], "shaderHandle"));
                return null;
            }), false);

            Globals.Define("shader_uniform1f", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                _graphics.ShaderUniform1f(
                    ToInt(arguments[0], "shaderHandle"),
                    ToStringValue(arguments[1]),
                    ToNumber(arguments[2], "value"));
                return null;
            }), false);

            Globals.Define("shader_uniform1i", new NovaNativeFunction(3, (interpreter, arguments) =>
            {
                _graphics.ShaderUniform1i(
                    ToInt(arguments[0], "shaderHandle"),
                    ToStringValue(arguments[1]),
                    ToInt(arguments[2], "value"));
                return null;
            }), false);

            Globals.Define("shader_uniform2f", new NovaNativeFunction(4, (interpreter, arguments) =>
            {
                _graphics.ShaderUniform2f(
                    ToInt(arguments[0], "shaderHandle"),
                    ToStringValue(arguments[1]),
                    ToNumber(arguments[2], "x"),
                    ToNumber(arguments[3], "y"));
                return null;
            }), false);

            Globals.Define("shader_uniform3f", new NovaNativeFunction(5, (interpreter, arguments) =>
            {
                _graphics.ShaderUniform3f(
                    ToInt(arguments[0], "shaderHandle"),
                    ToStringValue(arguments[1]),
                    ToNumber(arguments[2], "x"),
                    ToNumber(arguments[3], "y"),
                    ToNumber(arguments[4], "z"));
                return null;
            }), false);

            Globals.Define("shader_uniform4f", new NovaNativeFunction(6, (interpreter, arguments) =>
            {
                _graphics.ShaderUniform4f(
                    ToInt(arguments[0], "shaderHandle"),
                    ToStringValue(arguments[1]),
                    ToNumber(arguments[2], "x"),
                    ToNumber(arguments[3], "y"),
                    ToNumber(arguments[4], "z"),
                    ToNumber(arguments[5], "w"));
                return null;
            }), false);

            AppDomain.CurrentDomain.ProcessExit += (_, _) => _graphics.Dispose();
        }

        internal void PushCallFrame(string name)
        {
            _callStack.Add(name);
        }

        internal void PopCallFrame()
        {
            if (_callStack.Count == 0) return;
            _callStack.RemoveAt(_callStack.Count - 1);
        }

        private static NovaList AsList(object? value)
        {
            if (value is NovaList list) return list;
            throw new Exception("Expected list value.");
        }

        private static NovaMap AsMap(object? value)
        {
            if (value is NovaMap map) return map;
            throw new Exception("Expected map value.");
        }

        private static string ToMapKey(object? value)
        {
            if (value == null) return "null";
            if (value is bool b) return b ? "true" : "false";
            if (value is double d) return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            return value.ToString() ?? string.Empty;
        }

        private static double ToNumber(object? value, string argumentName)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is bool b) return b ? 1.0 : 0.0;
            if (value != null && double.TryParse(value.ToString(), out double parsed)) return parsed;
            throw new Exception($"Argument '{argumentName}' must be a number.");
        }

        private static int ToInt(object? value, string argumentName)
        {
            return (int)Math.Round(ToNumber(value, argumentName));
        }

        private static bool ToBool(object? value)
        {
            if (value is bool b) return b;
            if (value is double d) return Math.Abs(d) > double.Epsilon;
            if (value is int i) return i != 0;
            if (value is string s)
            {
                if (bool.TryParse(s, out bool parsed)) return parsed;
                if (string.Equals(s, "(+)", StringComparison.Ordinal)) return true;
                if (string.Equals(s, "(-)", StringComparison.Ordinal)) return false;
                if (double.TryParse(s, out double n)) return Math.Abs(n) > double.Epsilon;
                return s.Length > 0;
            }
            return value != null;
        }

        private static string ToStringValue(object? value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public void Interpret(List<Stmt> statements, string? sourcePath = null)
        {
            try
            {
                ExecuteStatements(statements, sourcePath);
            }
            catch (BreakException)
            {
                WriteLineColored("[Runtime Error] '!!>' can only be used inside a loop.", ConsoleColor.Red);
            }
            catch (ContinueException)
            {
                WriteLineColored("[Runtime Error] '!!<' can only be used inside a loop.", ConsoleColor.Red);
            }
            catch (Exception e)
            {
                WriteLineColored($"[Runtime Error] {e.Message}", ConsoleColor.Red);
                if (_callStack.Count > 0)
                {
                    WriteLineColored("[Call Stack]", ConsoleColor.DarkRed);
                    for (int i = _callStack.Count - 1; i >= 0; i--)
                    {
                        WriteLineColored($"  at {_callStack[i]}()", ConsoleColor.DarkRed);
                    }
                }
            }
            finally
            {
                _callStack.Clear();
            }
        }

        private void ExecuteStatements(List<Stmt> statements, string? sourcePath)
        {
            bool pushedSourceContext = TryPushSourceContext(sourcePath);
            try
            {
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                if (pushedSourceContext)
                {
                    _sourceContextStack.RemoveAt(_sourceContextStack.Count - 1);
                }
            }
        }

        private bool TryPushSourceContext(string? sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) return false;

            string fullPath = Path.GetFullPath(sourcePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory)) return false;

            _sourceContextStack.Add(directory);
            return true;
        }

        private void Execute(Stmt stmt)
        {
            switch (stmt)
            {
                case ExpressionStmt s: Evaluate(s.Expression); break;
                case OutputStmt s:
                    var text = Stringify(Evaluate(s.Expression));
                    WriteLineColored(text, PickOutputColor(text));
                    break;
                case VarStmt s:
                    object? val = null;
                    if (s.Initializer != null) val = Evaluate(s.Initializer);
                    _environment.Define(s.Name.Value, val, s.IsMutable);
                    break;
                case ImportStmt s:
                    ExecuteImport(s);
                    break;
                case BlockStmt s: ExecuteBlock(s.Statements, new Environment(_environment)); break;
                case IfStmt s:
                    if (IsTruthy(Evaluate(s.Condition))) Execute(s.ThenBranch);
                    else if (s.ElseBranch != null) Execute(s.ElseBranch);
                    break;
                case WhileStmt s:
                    _loopDepth++;
                    try
                    {
                        while (IsTruthy(Evaluate(s.Condition)))
                        {
                            try
                            {
                                Execute(s.Body);
                            }
                            catch (ContinueException)
                            {
                                continue;
                            }
                            catch (BreakException)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        _loopDepth--;
                    }
                    break;
                case BreakStmt:
                    if (_loopDepth <= 0) throw new BreakException();
                    throw new BreakException();
                case ContinueStmt:
                    if (_loopDepth <= 0) throw new ContinueException();
                    throw new ContinueException();
                
                case FunctionStmt s:
                    var function = new NovaFunction(s, _environment);
                    _environment.Define(s.Name.Value, function, false);
                    break;
                case StructureStmt s:
                    var methods = new Dictionary<string, NovaFunction>();
                    foreach (var method in s.Methods)
                    {
                        methods[method.Name.Value] = new NovaFunction(method, _environment);
                    }
                    var structure = new NovaStructure(s.Name.Value, s.Properties, methods);
                    _environment.Define(s.Name.Value, structure, false);
                    break;
                case ReturnStmt s:
                    object? retVal = null;
                    if (s.Value != null) retVal = Evaluate(s.Value);
                    throw new ReturnException(retVal);
                case TryCatchStmt s:
                    try
                    {
                        ExecuteBlock(s.TryBlock, new Environment(_environment));
                    }
                    catch (ReturnException) { throw; }
                    catch (BreakException) { throw; }
                    catch (ContinueException) { throw; }
                    catch (Exception e)
                    {
                        if (s.CatchBlock != null)
                        {
                            var catchEnv = new Environment(_environment);
                            if (s.CatchVar != null) catchEnv.Define(s.CatchVar.Value, e.Message, false);
                            ExecuteBlock(s.CatchBlock, catchEnv);
                        }
                    }
                    finally
                    {
                        if (s.FinallyBlock != null)
                        {
                            ExecuteBlock(s.FinallyBlock, new Environment(_environment));
                        }
                    }
                    break;
            }
        }

        private void ExecuteImport(ImportStmt stmt)
        {
            object? pathValue = Evaluate(stmt.PathExpression);
            string importPath = ToStringValue(pathValue).Trim();
            if (string.IsNullOrWhiteSpace(importPath))
            {
                throw new Exception($"Import path cannot be empty at line {stmt.Keyword.Line}.");
            }

            string resolvedPath = ResolveImportPath(importPath);
            if (_loadedModules.Contains(resolvedPath)) return;

            if (_modulesInProgress.Contains(resolvedPath))
            {
                throw new Exception($"Circular import detected: {resolvedPath}");
            }

            _modulesInProgress.Add(resolvedPath);
            PushCallFrame($"import {Path.GetFileName(resolvedPath)}");
            try
            {
                string source = File.ReadAllText(resolvedPath);
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var statements = parser.Parse();
                ExecuteStatements(statements, resolvedPath);
                _loadedModules.Add(resolvedPath);
            }
            finally
            {
                PopCallFrame();
                _modulesInProgress.Remove(resolvedPath);
            }
        }

        private string ResolveImportPath(string importPath)
        {
            var candidateBases = new List<string>();
            if (_sourceContextStack.Count > 0)
            {
                candidateBases.Add(_sourceContextStack[_sourceContextStack.Count - 1]);
            }

            candidateBases.Add(Directory.GetCurrentDirectory());
            candidateBases.Add(AppContext.BaseDirectory);

            bool hasExtension = Path.HasExtension(importPath);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> candidates;
            if (Path.IsPathRooted(importPath))
            {
                candidates = ExpandPathCandidates(importPath, hasExtension);
            }
            else
            {
                var allCandidates = new List<string>();
                foreach (string baseDir in candidateBases)
                {
                    string combined = Path.Combine(baseDir, importPath);
                    allCandidates.AddRange(ExpandPathCandidates(combined, hasExtension));
                }

                candidates = allCandidates;
            }

            foreach (string candidate in candidates)
            {
                string fullPath = Path.GetFullPath(candidate);
                if (!seen.Add(fullPath)) continue;
                if (File.Exists(fullPath)) return fullPath;
            }

            throw new Exception($"Import file not found: {importPath}");
        }

        private static IEnumerable<string> ExpandPathCandidates(string basePath, bool hasExtension)
        {
            yield return basePath;
            if (!hasExtension)
            {
                yield return basePath + ".ns";
            }
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var previous = _environment;
            try
            {
                _environment = environment;
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                _environment = previous;
            }
        }

        private object? Evaluate(Expr expr)
        {
            switch (expr)
            {
                case LiteralExpr e: return e.Value;
                case VariableExpr e: return _environment.Get(e.Name);
                case GetExpr e:
                    var obj = Evaluate(e.Object);
                    if (obj is NovaInstance instance) return instance.Get(e.Name);
                    throw new Exception("Only instances have properties.");
                case SetExpr e:
                    var o = Evaluate(e.Object);
                    if (o is NovaInstance inst)
                    {
                        var value = Evaluate(e.Value);
                        inst.Set(e.Name, value);
                        return value;
                    }
                    throw new Exception("Only instances have properties.");
                case BinaryExpr e:
                    var left = Evaluate(e.Left);
                    var right = Evaluate(e.Right);
                    switch (e.Operator.Type)
                    {
                        case TokenType.Plus:
                            if (left is double l1 && right is double r1) return l1 + r1;
                            if (left is string || right is string) return Stringify(left) + Stringify(right);
                            throw new Exception("Operands must be two numbers or two strings.");
                        case TokenType.Minus: return (double)left! - (double)right!;
                        case TokenType.Star: return (double)left! * (double)right!;
                        case TokenType.Slash: return (double)left! / (double)right!;
                        case TokenType.Greater: return (double)left! > (double)right!;
                        case TokenType.GreaterEqual: return (double)left! >= (double)right!;
                        case TokenType.Less: return (double)left! < (double)right!;
                        case TokenType.LessEqual: return (double)left! <= (double)right!;
                        case TokenType.EqualEqual: return IsEqual(left, right);
                        case TokenType.BangEqual: return !IsEqual(left, right);
                    }
                    break;
                case UnaryExpr e:
                    var r = Evaluate(e.Right);
                    if (e.Operator.Type == TokenType.Minus) return -(double)r!;
                    // Add ! for logical not if needed
                    break;
                case AssignExpr e:
                    var v = Evaluate(e.Value);
                    _environment.Assign(e.Name, v);
                    return v;
                case CallExpr e:
                    var callee = Evaluate(e.Callee);
                    var args = e.Arguments.Select(Evaluate).ToList();
                    if (callee is INovaCallable func)
                    {
                        if (args.Count != func.Arity()) throw new Exception($"Expected {func.Arity()} arguments but got {args.Count}.");
                        return func.Call(this, args);
                    }
                    throw new Exception("Can only call functions and structures.");
                case NewExpr e:
                    var structure = _environment.Get(e.Name);
                    var arguments = e.Arguments.Select(Evaluate).ToList();
                    if (structure is NovaStructure s)
                    {
                        if (arguments.Count != s.Arity()) throw new Exception($"Expected {s.Arity()} arguments but got {arguments.Count}.");
                        return s.Call(this, arguments);
                    }
                    throw new Exception("Can only instantiate structures.");
            }
            return null;
        }

        private bool IsTruthy(object? obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            return true;
        }

        private bool IsEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;
            return a.Equals(b);
        }

        private string Stringify(object? obj)
        {
            if (obj == null) return "(_)";
            if (obj is bool b) return b ? "(+)" : "(-)";
            if (obj is NovaList list)
            {
                var pieces = list.Snapshot().Select(Stringify);
                return "[" + string.Join(", ", pieces) + "]";
            }
            if (obj is NovaMap map)
            {
                var parts = new List<string>();
                foreach (string key in map.Keys())
                {
                    parts.Add(key + ": " + Stringify(map.Get(key)));
                }
                return "{" + string.Join(", ", parts) + "}";
            }
            return obj.ToString()!;
        }

        private static ConsoleColor PickOutputColor(string text)
        {
            if (string.Equals(text, "1", StringComparison.Ordinal)) return ConsoleColor.Green;
            if (string.Equals(text, "0", StringComparison.Ordinal)) return ConsoleColor.Cyan;
            if (text.Contains("error", StringComparison.OrdinalIgnoreCase)) return ConsoleColor.Red;
            if (text.Contains("warning", StringComparison.OrdinalIgnoreCase)) return ConsoleColor.Yellow;
            if (text.Contains("success", StringComparison.OrdinalIgnoreCase)) return ConsoleColor.Green;
            if (text.Contains("nova", StringComparison.OrdinalIgnoreCase)) return ConsoleColor.Magenta;
            return ConsoleColor.Gray;
        }

        private static void WriteLineColored(string text, ConsoleColor color)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(text);
                return;
            }

            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = previous;
        }

        public Dictionary<string, (object? Value, bool IsMutable)> SnapshotVariables()
        {
            var result = new Dictionary<string, (object? Value, bool IsMutable)>();
            var env = _environment;
            while (env != null)
            {
                foreach (var kv in env.GetAllVariables())
                {
                    if (!result.ContainsKey(kv.Key))
                    {
                        result[kv.Key] = kv.Value;
                    }
                }
                env = env.GetEnclosing();
            }
            return result;
        }
    }
}
