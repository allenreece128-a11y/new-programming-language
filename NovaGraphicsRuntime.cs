using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace NovaScript.Core
{
    public sealed class NovaGraphicsRuntime : IDisposable
    {
        private GameWindow? _window;
        private int _width;
        private int _height;
        private byte[] _canvas = Array.Empty<byte>();

        private int _canvasTexture;
        private int _quadVao;
        private int _quadVbo;
        private int _quadEbo;
        private int _defaultShaderProgram;
        private int _activeShaderProgram;

        private readonly Dictionary<int, int> _shaderPrograms = new();
        private readonly Dictionary<string, LoadedImage> _imageCache = new(StringComparer.OrdinalIgnoreCase);
        private int _nextShaderHandle = 1;

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _lastFrameMs;
        private double _deltaMs;
        private bool _initialized;
        private bool _disposed;

        private float _clearColorR;
        private float _clearColorG;
        private float _clearColorB;
        private float _clearColorA = 1f;
        private static readonly Dictionary<char, string[]> Font5x7 = new()
        {
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
            ['\''] = new[] { "..#..", "..#..", ".#...", ".....", ".....", ".....", "....." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." },
            [','] = new[] { ".....", ".....", ".....", ".....", ".##..", ".##..", ".#..." },
            [':'] = new[] { ".....", ".##..", ".##..", ".....", ".##..", ".##..", "....." },
            ['/'] = new[] { "....#", "...#.", "..#..", ".#...", "#....", ".....", "....." },
            ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "#....", "####.", "....#", "....#", "####." },
            ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", ".#.#.", "..#..", "..#..", "..#..", ".#.#.", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['█'] = new[] { "#####", "#####", "#####", "#####", "#####", "#####", "#####" },
            ['▓'] = new[] { "#####", "#...#", "#####", "#...#", "#####", "#...#", "#####" },
            ['▒'] = new[] { "#.#.#", ".#.#.", "#.#.#", ".#.#.", "#.#.#", ".#.#.", "#.#.#" },
            ['░'] = new[] { ".#.#.", "#.#.#", ".#.#.", "#.#.#", ".#.#.", "#.#.#", ".#.#." }
        };

        private readonly List<ChartPoint> _chartPoints = new();
        private readonly List<ChartSlice> _chartSlices = new();
        private readonly Dictionary<string, ChartColor> _chartSeriesColors = new(StringComparer.OrdinalIgnoreCase);
        private string _chartTitle = "Points scored";
        private string _chartEVariable = "E VARIABLE";
        private string _chartDVariable = "D VARIABLE";
        private ChartType _chartType = ChartType.Line;
        private int _chartPaletteIndex;
        private const double Tau = Math.PI * 2.0;
        private static readonly ChartColor[] ChartPalette =
        {
            new(255, 0, 0, 255),
            new(255, 52, 52, 255),
            new(255, 118, 64, 255),
            new(255, 184, 64, 255),
            new(0, 214, 135, 255),
            new(0, 180, 255, 255),
            new(255, 0, 255, 255),
            new(255, 255, 120, 255)
        };

        private enum ChartType
        {
            Line,
            Bar,
            Pie
        }

        private readonly record struct ChartPoint(string Series, string XLabel, double YValue);

        private readonly record struct ChartSlice(string Label, double Value, ChartColor Color);

        private readonly record struct ChartColor(byte R, byte G, byte B, byte A);

        private sealed record LoadedImage(int Width, int Height, byte[] PixelsRgba);

        public bool IsInitialized => _initialized;
        public bool IsRunning => _window != null && !_window.IsExiting;
        public int Width => _width;
        public int Height => _height;
        public double DeltaMs => _deltaMs;
        public double TicksMs => _clock.Elapsed.TotalMilliseconds;

        public void Init(int width, int height, string title)
        {
            if (width <= 0 || height <= 0)
            {
                throw new Exception("gfx_init width and height must be greater than zero.");
            }

            DestroyWindow();

            _width = width;
            _height = height;
            _canvas = new byte[_width * _height * 4];

            var gameSettings = new GameWindowSettings
            {
                UpdateFrequency = 0
            };

            var nativeSettings = new NativeWindowSettings
            {
                Title = title,
                ClientSize = new Vector2i(width, height),
                StartVisible = true,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            };

            _window = new GameWindow(gameSettings, nativeSettings);
            _window.MakeCurrent();
            GL.LoadBindings(new GLFWBindingsContext());
            _window.Resize += OnResize;

            GL.Viewport(0, 0, _width, _height);
            BuildRenderResources();

            _lastFrameMs = _clock.Elapsed.TotalMilliseconds;
            _deltaMs = 0;
            _initialized = true;
        }

        public bool Poll()
        {
            EnsureInitialized();
            _window!.ProcessEvents(0.0);

            var now = _clock.Elapsed.TotalMilliseconds;
            _deltaMs = now - _lastFrameMs;
            _lastFrameMs = now;

            return !_window!.IsExiting;
        }

        public void Close()
        {
            if (_window == null) return;
            _window.Close();
        }

        public void SetTitle(string title)
        {
            EnsureInitialized();
            _window!.Title = title;
        }

        public void SetVSync(bool enabled)
        {
            EnsureInitialized();
            _window!.VSync = enabled ? VSyncMode.On : VSyncMode.Off;
        }

        public void Resize(int width, int height)
        {
            EnsureInitialized();
            if (width <= 0 || height <= 0) return;
            _window!.ClientSize = new Vector2i(width, height);
        }

        public double MouseX()
        {
            EnsureInitialized();
            return _window!.MouseState.X;
        }

        public double MouseY()
        {
            EnsureInitialized();
            return _window!.MouseState.Y;
        }

        public bool IsMouseDown(int buttonIndex)
        {
            EnsureInitialized();
            var button = buttonIndex switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Right,
                2 => MouseButton.Middle,
                3 => MouseButton.Button1,
                4 => MouseButton.Button2,
                _ => MouseButton.Last
            };

            if (button == MouseButton.Last) return false;
            return _window!.MouseState.IsButtonDown(button);
        }

        public bool IsKeyDown(string keyName)
        {
            EnsureInitialized();
            if (!TryParseKey(keyName, out var key)) return false;
            return _window!.KeyboardState.IsKeyDown(key);
        }

        public void SetScreenClearColor(int r, int g, int b, int a)
        {
            _clearColorR = ClampByte(r) / 255f;
            _clearColorG = ClampByte(g) / 255f;
            _clearColorB = ClampByte(b) / 255f;
            _clearColorA = ClampByte(a) / 255f;
        }

        public void ClearCanvas(int r, int g, int b, int a)
        {
            EnsureInitialized();

            byte cr = ClampByte(r);
            byte cg = ClampByte(g);
            byte cb = ClampByte(b);
            byte ca = ClampByte(a);

            for (int i = 0; i < _canvas.Length; i += 4)
            {
                _canvas[i] = cr;
                _canvas[i + 1] = cg;
                _canvas[i + 2] = cb;
                _canvas[i + 3] = ca;
            }
        }

        public void Pixel(int x, int y, int r, int g, int b, int a)
        {
            EnsureInitialized();
            if (!InBounds(x, y)) return;

            int index = ((y * _width) + x) * 4;
            _canvas[index] = ClampByte(r);
            _canvas[index + 1] = ClampByte(g);
            _canvas[index + 2] = ClampByte(b);
            _canvas[index + 3] = ClampByte(a);
        }

        public void Line(int x1, int y1, int x2, int y2, int r, int g, int b, int a)
        {
            EnsureInitialized();

            int dx = Math.Abs(x2 - x1);
            int sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1);
            int sy = y1 < y2 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                Pixel(x1, y1, r, g, b, a);
                if (x1 == x2 && y1 == y2) break;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x1 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }

        public void Rect(int x, int y, int width, int height, int r, int g, int b, int a, bool fill)
        {
            EnsureInitialized();
            if (width <= 0 || height <= 0) return;

            if (fill)
            {
                int maxX = x + width;
                int maxY = y + height;
                for (int py = y; py < maxY; py++)
                {
                    for (int px = x; px < maxX; px++)
                    {
                        Pixel(px, py, r, g, b, a);
                    }
                }
                return;
            }

            int x2 = x + width - 1;
            int y2 = y + height - 1;
            Line(x, y, x2, y, r, g, b, a);
            Line(x2, y, x2, y2, r, g, b, a);
            Line(x2, y2, x, y2, r, g, b, a);
            Line(x, y2, x, y, r, g, b, a);
        }

        public void Text(int x, int y, string text, int scale, int r, int g, int b, int a)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(text)) return;
            if (scale < 1) scale = 1;

            byte cr = ClampByte(r);
            byte cg = ClampByte(g);
            byte cb = ClampByte(b);
            byte ca = ClampByte(a);

            int startX = x;
            int cursorX = x;
            int cursorY = y;

            foreach (char rawChar in text)
            {
                if (rawChar == '\n')
                {
                    cursorX = startX;
                    cursorY += 8 * scale;
                    continue;
                }

                char c = char.ToUpperInvariant(rawChar);
                if (!Font5x7.TryGetValue(c, out var glyph))
                {
                    glyph = Font5x7[' '];
                }

                DrawGlyph(cursorX, cursorY, glyph, scale, cr, cg, cb, ca);
                cursorX += 6 * scale;
            }
        }

        public void Image(string path, int x, int y, int width, int height)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(path)) return;
            if (width <= 0 || height <= 0) return;

            var image = LoadImage(path);
            if (image == null) return;

            for (int dy = 0; dy < height; dy++)
            {
                int py = y + dy;
                if (py < 0 || py >= _height) continue;
                int srcY = dy * image.Height / height;

                for (int dx = 0; dx < width; dx++)
                {
                    int px = x + dx;
                    if (px < 0 || px >= _width) continue;
                    int srcX = dx * image.Width / width;

                    int srcIndex = ((srcY * image.Width) + srcX) * 4;
                    int dstIndex = ((py * _width) + px) * 4;

                    _canvas[dstIndex] = image.PixelsRgba[srcIndex];
                    _canvas[dstIndex + 1] = image.PixelsRgba[srcIndex + 1];
                    _canvas[dstIndex + 2] = image.PixelsRgba[srcIndex + 2];
                    _canvas[dstIndex + 3] = image.PixelsRgba[srcIndex + 3];
                }
            }
        }

        public void ChartConfigure(string chartType, string title, string eVariable, string dVariable)
        {
            _chartType = ParseChartType(chartType);
            _chartTitle = string.IsNullOrWhiteSpace(title) ? "Chart" : title.Trim();
            _chartEVariable = string.IsNullOrWhiteSpace(eVariable) ? "E VARIABLE" : eVariable.Trim();
            _chartDVariable = string.IsNullOrWhiteSpace(dVariable) ? "D VARIABLE" : dVariable.Trim();
            ChartResetData();
        }

        public void ChartSetType(string chartType)
        {
            _chartType = ParseChartType(chartType);
        }

        public void ChartSetTitle(string title)
        {
            _chartTitle = string.IsNullOrWhiteSpace(title) ? "Chart" : title.Trim();
        }

        public void ChartSetVariables(string eVariable, string dVariable)
        {
            _chartEVariable = string.IsNullOrWhiteSpace(eVariable) ? "E VARIABLE" : eVariable.Trim();
            _chartDVariable = string.IsNullOrWhiteSpace(dVariable) ? "D VARIABLE" : dVariable.Trim();
        }

        public void ChartAddPoint(string series, string xLabel, double yValue)
        {
            string normalizedSeries = string.IsNullOrWhiteSpace(series) ? "Series 1" : series.Trim();
            string normalizedX = string.IsNullOrWhiteSpace(xLabel) ? $"X{_chartPoints.Count + 1}" : xLabel.Trim();
            _chartPoints.Add(new ChartPoint(normalizedSeries, normalizedX, yValue));
            _ = GetOrCreateSeriesColor(normalizedSeries);
        }

        public void ChartAddSlice(string label, double value)
        {
            string normalizedLabel = string.IsNullOrWhiteSpace(label) ? $"Slice {_chartSlices.Count + 1}" : label.Trim();
            _chartSlices.Add(new ChartSlice(normalizedLabel, value, NextChartColor()));
        }

        public void ChartClearData()
        {
            ChartResetData();
        }

        public void ChartRender()
        {
            EnsureInitialized();
            ClearCanvas(0, 0, 0, 255);

            switch (_chartType)
            {
                case ChartType.Line:
                    RenderLineChart();
                    break;
                case ChartType.Bar:
                    RenderBarChart();
                    break;
                case ChartType.Pie:
                    RenderPieChart();
                    break;
            }
        }

        public bool Present()
        {
            EnsureInitialized();
            if (!IsRunning) return false;

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                _width,
                _height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                _canvas);

            GL.ClearColor(_clearColorR, _clearColorG, _clearColorB, _clearColorA);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            int program = _activeShaderProgram == 0 ? _defaultShaderProgram : _activeShaderProgram;
            GL.UseProgram(program);

            int canvasLoc = GL.GetUniformLocation(program, "u_canvas");
            if (canvasLoc >= 0) GL.Uniform1(canvasLoc, 0);

            int timeLoc = GL.GetUniformLocation(program, "u_time");
            if (timeLoc >= 0) GL.Uniform1(timeLoc, (float)(_clock.Elapsed.TotalMilliseconds / 1000.0));

            int deltaLoc = GL.GetUniformLocation(program, "u_delta");
            if (deltaLoc >= 0) GL.Uniform1(deltaLoc, (float)(_deltaMs / 1000.0));

            int resolutionLoc = GL.GetUniformLocation(program, "u_resolution");
            if (resolutionLoc >= 0) GL.Uniform2(resolutionLoc, (float)_width, (float)_height);

            var mouse = _window!.MouseState;
            int mouseLoc = GL.GetUniformLocation(program, "u_mouse");
            if (mouseLoc >= 0) GL.Uniform2(mouseLoc, (float)mouse.X, (float)mouse.Y);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.BindVertexArray(_quadVao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            _window.SwapBuffers();
            return true;
        }

        public int ShaderCreate(string vertexSource, string fragmentSource)
        {
            EnsureInitialized();

            int program = BuildProgram(vertexSource, fragmentSource);
            int handle = _nextShaderHandle++;
            _shaderPrograms[handle] = program;
            return handle;
        }

        public void ShaderUse(int handle)
        {
            EnsureInitialized();
            if (handle == 0)
            {
                _activeShaderProgram = _defaultShaderProgram;
                return;
            }

            _activeShaderProgram = ShaderProgramFromHandle(handle);
        }

        public void ShaderUseDefault()
        {
            EnsureInitialized();
            _activeShaderProgram = _defaultShaderProgram;
        }

        public void ShaderDestroy(int handle)
        {
            EnsureInitialized();
            if (!_shaderPrograms.TryGetValue(handle, out int program)) return;
            if (_activeShaderProgram == program)
            {
                _activeShaderProgram = _defaultShaderProgram;
            }
            GL.DeleteProgram(program);
            _shaderPrograms.Remove(handle);
        }

        public void ShaderUniform1f(int handle, string uniformName, double x)
        {
            SetShaderUniform(handle, uniformName, loc => GL.Uniform1(loc, (float)x));
        }

        public void ShaderUniform1i(int handle, string uniformName, int x)
        {
            SetShaderUniform(handle, uniformName, loc => GL.Uniform1(loc, x));
        }

        public void ShaderUniform2f(int handle, string uniformName, double x, double y)
        {
            SetShaderUniform(handle, uniformName, loc => GL.Uniform2(loc, (float)x, (float)y));
        }

        public void ShaderUniform3f(int handle, string uniformName, double x, double y, double z)
        {
            SetShaderUniform(handle, uniformName, loc => GL.Uniform3(loc, (float)x, (float)y, (float)z));
        }

        public void ShaderUniform4f(int handle, string uniformName, double x, double y, double z, double w)
        {
            SetShaderUniform(handle, uniformName, loc => GL.Uniform4(loc, (float)x, (float)y, (float)z, (float)w));
        }

        public void Dispose()
        {
            if (_disposed) return;
            DestroyWindow();
            _disposed = true;
        }

        private void SetShaderUniform(int handle, string uniformName, Action<int> setter)
        {
            EnsureInitialized();
            int program = handle == 0 ? _defaultShaderProgram : ShaderProgramFromHandle(handle);
            GL.UseProgram(program);
            int location = GL.GetUniformLocation(program, uniformName);
            if (location < 0) return;
            setter(location);
        }

        private int ShaderProgramFromHandle(int handle)
        {
            if (!_shaderPrograms.TryGetValue(handle, out int program))
            {
                throw new Exception($"Invalid shader handle: {handle}");
            }

            return program;
        }

        private void OnResize(ResizeEventArgs e)
        {
            if (!_initialized || e.Width <= 0 || e.Height <= 0) return;

            _width = e.Width;
            _height = e.Height;
            _canvas = new byte[_width * _height * 4];

            GL.Viewport(0, 0, _width, _height);
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba8,
                _width,
                _height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                _canvas);
        }

        private void BuildRenderResources()
        {
            _canvasTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _canvasTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba8,
                _width,
                _height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                _canvas);

            float[] vertices =
            {
                -1f, -1f, 0f, 0f,
                 1f, -1f, 1f, 0f,
                 1f,  1f, 1f, 1f,
                -1f,  1f, 0f, 1f
            };

            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            _quadVao = GL.GenVertexArray();
            _quadVbo = GL.GenBuffer();
            _quadEbo = GL.GenBuffer();

            GL.BindVertexArray(_quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _quadEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            const string defaultVertexShader = """
                                               #version 330 core
                                               layout (location = 0) in vec2 a_pos;
                                               layout (location = 1) in vec2 a_uv;
                                               out vec2 v_uv;
                                               void main() {
                                                   gl_Position = vec4(a_pos, 0.0, 1.0);
                                                   v_uv = vec2(a_uv.x, 1.0 - a_uv.y);
                                               }
                                               """;

            const string defaultFragmentShader = """
                                                 #version 330 core
                                                 in vec2 v_uv;
                                                 out vec4 frag_color;
                                                 uniform sampler2D u_canvas;
                                                 void main() {
                                                     frag_color = texture(u_canvas, v_uv);
                                                 }
                                                 """;

            _defaultShaderProgram = BuildProgram(defaultVertexShader, defaultFragmentShader);
            _activeShaderProgram = _defaultShaderProgram;
        }

        private int BuildProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus == 0)
            {
                string log = GL.GetShaderInfoLog(vertexShader);
                GL.DeleteShader(vertexShader);
                throw new Exception($"Vertex shader compile failed: {log}");
            }

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus == 0)
            {
                string log = GL.GetShaderInfoLog(fragmentShader);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                throw new Exception($"Fragment shader compile failed: {log}");
            }

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            if (linkStatus == 0)
            {
                string log = GL.GetProgramInfoLog(program);
                GL.DeleteProgram(program);
                throw new Exception($"Shader program link failed: {log}");
            }

            return program;
        }

        private void RenderLineChart()
        {
            RenderCartesianChart(drawBars: false);
        }

        private void RenderBarChart()
        {
            RenderCartesianChart(drawBars: true);
        }

        private void RenderCartesianChart(bool drawBars)
        {
            int left = Math.Max(56, _width / 12);
            int right = _width - Math.Max(32, _width / 18);
            int top = Math.Max(78, _height / 7);
            int bottom = _height - Math.Max(68, _height / 8);
            if (right - left < 80 || bottom - top < 80)
            {
                return;
            }

            DrawTextCentered(_width / 2, 22, _chartTitle, 3, 255, 0, 255, 255);

            var xIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var xLabels = new List<string>();
            foreach (var point in _chartPoints)
            {
                string label = NormalizeChartLabel(point.XLabel, $"X{xLabels.Count + 1}");
                if (xIndex.ContainsKey(label)) continue;
                xIndex[label] = xLabels.Count;
                xLabels.Add(label);
            }

            if (xLabels.Count == 0)
            {
                xLabels.Add("X1");
                xIndex["X1"] = 0;
            }

            var seriesNames = new List<string>();
            var seriesNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var point in _chartPoints)
            {
                string name = NormalizeChartLabel(point.Series, "Series 1");
                if (seriesNameSet.Add(name))
                {
                    seriesNames.Add(name);
                    _ = GetOrCreateSeriesColor(name);
                }
            }

            if (seriesNames.Count == 0)
            {
                seriesNames.Add("Series 1");
                _ = GetOrCreateSeriesColor("Series 1");
            }

            (double minY, double maxY) = GetChartYRange();
            DrawYGridAndLabels(left, right, top, bottom, minY, maxY);

            int zeroY = MapYValue(0, minY, maxY, top, bottom);
            Line(left, top, left, bottom, 168, 168, 168, 255);
            Line(left, zeroY, right, zeroY, 168, 168, 168, 255);

            for (int i = 0; i < xLabels.Count; i++)
            {
                int x = MapCategoryCenter(i, xLabels.Count, left, right);
                Line(x, bottom, x, bottom + 4, 0, 255, 0, 255);
                DrawTextCentered(x, bottom + 14, xLabels[i], 1, 255, 0, 255, 255);
            }

            DrawTextCentered((left + right) / 2, bottom + 34, _chartEVariable, 1, 255, 0, 255, 255);
            Text(12, top - 20, _chartDVariable, 1, 255, 0, 255, 255);

            if (_chartPoints.Count == 0)
            {
                DrawTextCentered((left + right) / 2, (top + bottom) / 2 - 5, "NO DATA", 1, 255, 80, 80, 255);
                return;
            }

            if (drawBars)
            {
                DrawBarSeries(seriesNames, xLabels, xIndex, left, right, top, bottom, minY, maxY);
            }
            else
            {
                DrawLineSeries(seriesNames, xLabels, xIndex, left, right, top, bottom, minY, maxY);
            }

            DrawSeriesLegend(seriesNames, left, right, top - 32);
        }

        private void DrawYGridAndLabels(int left, int right, int top, int bottom, double minY, double maxY)
        {
            const int tickCount = 6;
            for (int i = 0; i <= tickCount; i++)
            {
                double t = i / (double)tickCount;
                int y = bottom - (int)Math.Round((bottom - top) * t);
                Line(left, y, right, y, 0, 255, 0, 255);

                double value = minY + (maxY - minY) * t;
                string label = FormatNumber(value);
                DrawTextRight(left - 8, y - 4, label, 1, 255, 0, 255, 255);
            }
        }

        private void DrawLineSeries(
            List<string> seriesNames,
            List<string> xLabels,
            Dictionary<string, int> xIndex,
            int left,
            int right,
            int top,
            int bottom,
            double minY,
            double maxY)
        {
            int categoryCount = xLabels.Count;
            foreach (string seriesName in seriesNames)
            {
                var points = new List<(int CategoryIndex, double Value)>();
                foreach (var point in _chartPoints)
                {
                    if (!string.Equals(seriesName, point.Series, StringComparison.OrdinalIgnoreCase)) continue;
                    if (xIndex.TryGetValue(NormalizeChartLabel(point.XLabel, "X"), out int categoryIndex))
                    {
                        points.Add((categoryIndex, point.YValue));
                    }
                }

                points.Sort((a, b) => a.CategoryIndex.CompareTo(b.CategoryIndex));
                if (points.Count == 0) continue;

                ChartColor color = GetOrCreateSeriesColor(seriesName);
                int prevX = 0;
                int prevY = 0;
                bool hasPrev = false;

                foreach (var p in points)
                {
                    int x = MapCategoryCenter(p.CategoryIndex, categoryCount, left, right);
                    int y = MapYValue(p.Value, minY, maxY, top, bottom);

                    if (hasPrev)
                    {
                        DrawThickLine(prevX, prevY, x, y, 2, color);
                    }

                    Rect(x - 1, y - 1, 3, 3, color.R, color.G, color.B, color.A, true);
                    prevX = x;
                    prevY = y;
                    hasPrev = true;
                }
            }
        }

        private void DrawBarSeries(
            List<string> seriesNames,
            List<string> xLabels,
            Dictionary<string, int> xIndex,
            int left,
            int right,
            int top,
            int bottom,
            double minY,
            double maxY)
        {
            int categoryCount = Math.Max(1, xLabels.Count);
            int seriesCount = Math.Max(1, seriesNames.Count);
            double slotWidth = (right - left) / (double)categoryCount;
            double groupWidth = slotWidth * 0.72;
            double barWidth = Math.Max(2.0, groupWidth / seriesCount);

            int zeroY = MapYValue(0, minY, maxY, top, bottom);

            var seriesIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < seriesNames.Count; i++)
            {
                seriesIndex[seriesNames[i]] = i;
            }

            var values = new Dictionary<(int Category, int Series), double>();
            foreach (var point in _chartPoints)
            {
                string normalizedSeries = NormalizeChartLabel(point.Series, "Series 1");
                string normalizedX = NormalizeChartLabel(point.XLabel, "X");
                if (!seriesIndex.TryGetValue(normalizedSeries, out int sIndex)) continue;
                if (!xIndex.TryGetValue(normalizedX, out int cIndex)) continue;
                values[(cIndex, sIndex)] = point.YValue;
            }

            for (int c = 0; c < categoryCount; c++)
            {
                double groupStart = left + slotWidth * c + (slotWidth - groupWidth) * 0.5;
                for (int s = 0; s < seriesCount; s++)
                {
                    if (!values.TryGetValue((c, s), out double value)) continue;

                    ChartColor color = GetOrCreateSeriesColor(seriesNames[s]);
                    int x = (int)Math.Round(groupStart + s * barWidth);
                    int w = Math.Max(1, (int)Math.Floor(barWidth) - 1);
                    int y = MapYValue(value, minY, maxY, top, bottom);
                    int barTop = Math.Min(y, zeroY);
                    int barHeight = Math.Abs(y - zeroY);
                    if (barHeight == 0) barHeight = 1;

                    Rect(x, barTop, w, barHeight, color.R, color.G, color.B, color.A, true);
                }
            }
        }

        private void DrawSeriesLegend(List<string> seriesNames, int left, int right, int y)
        {
            if (seriesNames.Count == 0) return;

            int totalWidth = 0;
            foreach (string series in seriesNames)
            {
                totalWidth += 18 + 6 + MeasureTextWidth(series, 1) + 20;
            }

            int cursorX = left + Math.Max(0, ((right - left) - totalWidth) / 2);
            foreach (string series in seriesNames)
            {
                ChartColor color = GetOrCreateSeriesColor(series);
                DrawThickLine(cursorX, y + 4, cursorX + 14, y + 4, 2, color);
                Text(cursorX + 20, y, series, 1, 92, 92, 92, 255);
                cursorX += 18 + 6 + MeasureTextWidth(series, 1) + 20;
            }
        }

        private (double Min, double Max) GetChartYRange()
        {
            if (_chartPoints.Count == 0)
            {
                return (0.0, 100.0);
            }

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;
            foreach (var point in _chartPoints)
            {
                if (point.YValue < min) min = point.YValue;
                if (point.YValue > max) max = point.YValue;
            }

            if (min > 0) min = 0;
            if (max < 0) max = 0;

            double span = max - min;
            if (span <= double.Epsilon)
            {
                span = Math.Abs(max) > 1 ? Math.Abs(max) : 1;
                max = min + span;
            }

            double step = NiceStep(span / 5.0);
            min = Math.Floor(min / step) * step;
            max = Math.Ceiling(max / step) * step;
            if (Math.Abs(max - min) <= double.Epsilon)
            {
                max = min + step;
            }

            return (min, max);
        }

        private static double NiceStep(double raw)
        {
            if (raw <= 0) return 1;

            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double residual = raw / magnitude;
            double niceResidual = residual <= 1 ? 1 : residual <= 2 ? 2 : residual <= 5 ? 5 : 10;
            return niceResidual * magnitude;
        }

        private void RenderPieChart()
        {
            DrawTextCentered(_width / 2, 22, _chartTitle, 3, 255, 0, 255, 255);
            DrawTextCentered(_width / 2, 58, _chartDVariable + " BY " + _chartEVariable, 1, 255, 0, 255, 255);

            var slices = BuildPieSlicesForRender();
            if (slices.Count == 0)
            {
                DrawTextCentered(_width / 2, _height / 2, "NO DATA", 1, 255, 80, 80, 255);
                return;
            }

            double total = 0;
            foreach (var slice in slices)
            {
                if (slice.Value > 0) total += slice.Value;
            }
            if (total <= 0)
            {
                DrawTextCentered(_width / 2, _height / 2, "NO DATA", 1, 255, 80, 80, 255);
                return;
            }

            int centerX = Math.Max(110, _width / 3);
            int centerY = _height / 2 + 18;
            int radius = Math.Max(36, Math.Min(_width / 4, _height / 3));

            double startAngle = -Math.PI / 2.0;
            foreach (var slice in slices)
            {
                if (slice.Value <= 0) continue;
                double sweep = Tau * (slice.Value / total);
                FillPieSlice(centerX, centerY, radius, startAngle, sweep, slice.Color);
                startAngle += sweep;
            }

            DrawCircleOutline(centerX, centerY, radius, new ChartColor(0, 255, 0, 255));
            DrawPieLegend(slices, total, centerX + radius + 32, 92);
        }

        private List<ChartSlice> BuildPieSlicesForRender()
        {
            var result = new List<ChartSlice>();
            if (_chartSlices.Count > 0)
            {
                foreach (var slice in _chartSlices)
                {
                    if (slice.Value > 0) result.Add(slice);
                }
                return result;
            }

            var sums = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();
            foreach (var point in _chartPoints)
            {
                string label = NormalizeChartLabel(point.XLabel, $"SLICE {order.Count + 1}");
                if (!sums.ContainsKey(label))
                {
                    sums[label] = 0;
                    order.Add(label);
                }

                if (point.YValue > 0)
                {
                    sums[label] += point.YValue;
                }
            }

            for (int i = 0; i < order.Count; i++)
            {
                string label = order[i];
                double value = sums[label];
                if (value <= 0) continue;
                ChartColor color = ChartPalette[i % ChartPalette.Length];
                result.Add(new ChartSlice(label, value, color));
            }

            return result;
        }

        private void DrawPieLegend(List<ChartSlice> slices, double total, int startX, int startY)
        {
            int y = startY;
            foreach (var slice in slices)
            {
                Rect(startX, y + 2, 12, 10, slice.Color.R, slice.Color.G, slice.Color.B, 255, true);
                string percent = FormatNumber(slice.Value / total * 100);
                Text(startX + 18, y, $"{slice.Label} {percent}%", 1, 255, 0, 255, 255);
                y += 16;
            }
        }

        private void FillPieSlice(int centerX, int centerY, int radius, double startAngle, double sweepAngle, ChartColor color)
        {
            if (radius <= 0 || sweepAngle <= 0) return;

            double normalizedStart = NormalizeAngle(startAngle);
            int radiusSquared = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radiusSquared) continue;

                    double angle = NormalizeAngle(Math.Atan2(dy, dx));
                    if (!AngleInSweep(angle, normalizedStart, sweepAngle)) continue;

                    Pixel(centerX + dx, centerY + dy, color.R, color.G, color.B, color.A);
                }
            }
        }

        private void DrawCircleOutline(int centerX, int centerY, int radius, ChartColor color)
        {
            if (radius <= 0) return;

            int previousX = centerX + radius;
            int previousY = centerY;
            for (int i = 1; i <= 360; i++)
            {
                double angle = i * Tau / 360.0;
                int x = centerX + (int)Math.Round(Math.Cos(angle) * radius);
                int y = centerY + (int)Math.Round(Math.Sin(angle) * radius);
                Line(previousX, previousY, x, y, color.R, color.G, color.B, color.A);
                previousX = x;
                previousY = y;
            }
        }

        private static bool AngleInSweep(double angle, double startAngle, double sweepAngle)
        {
            if (sweepAngle >= Tau) return true;

            double end = startAngle + sweepAngle;
            if (end <= Tau)
            {
                return angle >= startAngle && angle <= end;
            }

            return angle >= startAngle || angle <= end - Tau;
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % Tau;
            return normalized < 0 ? normalized + Tau : normalized;
        }

        private static int MapCategoryCenter(int index, int count, int left, int right)
        {
            if (count <= 0) return left;
            double slot = (right - left) / (double)count;
            return left + (int)Math.Round(slot * (index + 0.5));
        }

        private static int MapYValue(double value, double min, double max, int top, int bottom)
        {
            if (Math.Abs(max - min) <= double.Epsilon) return bottom;

            double t = (value - min) / (max - min);
            t = Math.Clamp(t, 0, 1);
            return bottom - (int)Math.Round((bottom - top) * t);
        }

        private void DrawThickLine(int x1, int y1, int x2, int y2, int thickness, ChartColor color)
        {
            int half = Math.Max(0, thickness / 2);
            for (int oy = -half; oy <= half; oy++)
            {
                for (int ox = -half; ox <= half; ox++)
                {
                    Line(x1 + ox, y1 + oy, x2 + ox, y2 + oy, color.R, color.G, color.B, color.A);
                }
            }
        }

        private void DrawTextCentered(int centerX, int y, string text, int scale, int r, int g, int b, int a)
        {
            int width = MeasureTextWidth(text, scale);
            int drawX = centerX - width / 2;
            if (drawX < 0) drawX = 0;
            Text(drawX, y, text, scale, r, g, b, a);
        }

        private void DrawTextRight(int rightX, int y, string text, int scale, int r, int g, int b, int a)
        {
            int drawX = rightX - MeasureTextWidth(text, scale);
            Text(drawX, y, text, scale, r, g, b, a);
        }

        private static int MeasureTextWidth(string text, int scale)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            if (scale < 1) scale = 1;

            int lineWidth = 0;
            int maxWidth = 0;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    if (lineWidth > maxWidth) maxWidth = lineWidth;
                    lineWidth = 0;
                    continue;
                }

                lineWidth += 6 * scale;
            }

            if (lineWidth > maxWidth) maxWidth = lineWidth;
            return maxWidth;
        }

        private ChartColor GetOrCreateSeriesColor(string seriesName)
        {
            if (_chartSeriesColors.TryGetValue(seriesName, out var color))
            {
                return color;
            }

            color = NextChartColor();
            _chartSeriesColors[seriesName] = color;
            return color;
        }

        private ChartColor NextChartColor()
        {
            var color = ChartPalette[_chartPaletteIndex % ChartPalette.Length];
            _chartPaletteIndex++;
            return color;
        }

        private void ChartResetData()
        {
            _chartPoints.Clear();
            _chartSlices.Clear();
            _chartSeriesColors.Clear();
            _chartPaletteIndex = 0;
        }

        private static string NormalizeChartLabel(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim();
        }

        private static string FormatNumber(double value)
        {
            double rounded = Math.Round(value);
            if (Math.Abs(value - rounded) < 0.000001)
            {
                return rounded.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static ChartType ParseChartType(string chartType)
        {
            if (string.IsNullOrWhiteSpace(chartType)) return ChartType.Line;

            string normalized = chartType
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal);

            return normalized switch
            {
                "line" => ChartType.Line,
                "barchart" => ChartType.Bar,
                "bar" => ChartType.Bar,
                "pie" => ChartType.Pie,
                "piechart" => ChartType.Pie,
                "pigraph" => ChartType.Pie,
                "pichart" => ChartType.Pie,
                "pi" => ChartType.Pie,
                _ => throw new Exception("Unknown chart type. Use line, bar, or pie.")
            };
        }

        private LoadedImage? LoadImage(string path)
        {
            string resolvedPath = ResolveImagePath(path);
            if (_imageCache.TryGetValue(resolvedPath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(resolvedPath))
            {
                throw new Exception($"Image file not found: {path}");
            }

            try
            {
                var raw = NovaImageLoader.LoadRgba(resolvedPath);
                var loaded = new LoadedImage(raw.Width, raw.Height, raw.PixelsRgba);
                _imageCache[resolvedPath] = loaded;
                return loaded;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load image '{path}': {ex.Message}");
            }
        }

        private static string ResolveImagePath(string path)
        {
            string candidate = path.Trim();
            if (Path.IsPathFullyQualified(candidate))
            {
                return candidate;
            }

            string fromWorkingDir = Path.GetFullPath(candidate);
            if (File.Exists(fromWorkingDir))
            {
                return fromWorkingDir;
            }

            string fromAppDir = Path.Combine(AppContext.BaseDirectory, candidate);
            if (File.Exists(fromAppDir))
            {
                return fromAppDir;
            }

            return fromWorkingDir;
        }

        private void DrawGlyph(int x, int y, string[] glyph, int scale, byte r, byte g, byte b, byte a)
        {
            for (int row = 0; row < glyph.Length; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '#') continue;

                    int baseX = x + col * scale;
                    int baseY = y + row * scale;
                    for (int oy = 0; oy < scale; oy++)
                    {
                        int py = baseY + oy;
                        for (int ox = 0; ox < scale; ox++)
                        {
                            int px = baseX + ox;
                            if (!InBounds(px, py)) continue;
                            int index = ((py * _width) + px) * 4;
                            _canvas[index] = r;
                            _canvas[index + 1] = g;
                            _canvas[index + 2] = b;
                            _canvas[index + 3] = a;
                        }
                    }
                }
            }
        }

        private void DestroyWindow()
        {
            if (_window == null)
            {
                _initialized = false;
                return;
            }

            _window.MakeCurrent();

            if (_defaultShaderProgram != 0) GL.DeleteProgram(_defaultShaderProgram);
            _defaultShaderProgram = 0;
            _activeShaderProgram = 0;

            foreach (var program in _shaderPrograms.Values)
            {
                GL.DeleteProgram(program);
            }
            _shaderPrograms.Clear();
            _imageCache.Clear();
            _nextShaderHandle = 1;

            if (_quadVao != 0) GL.DeleteVertexArray(_quadVao);
            if (_quadVbo != 0) GL.DeleteBuffer(_quadVbo);
            if (_quadEbo != 0) GL.DeleteBuffer(_quadEbo);
            if (_canvasTexture != 0) GL.DeleteTexture(_canvasTexture);

            _quadVao = 0;
            _quadVbo = 0;
            _quadEbo = 0;
            _canvasTexture = 0;

            _window.Resize -= OnResize;
            _window.Close();
            _window.Dispose();
            _window = null;
            _canvas = Array.Empty<byte>();
            _width = 0;
            _height = 0;
            _initialized = false;
        }

        private static byte ClampByte(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }

        private static bool TryParseKey(string keyName, out Keys key)
        {
            string normalized = keyName.Trim().Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
            switch (normalized.ToLowerInvariant())
            {
                case "esc": key = Keys.Escape; return true;
                case "enter": key = Keys.Enter; return true;
                case "return": key = Keys.Enter; return true;
                case "space": key = Keys.Space; return true;
                case "left": key = Keys.Left; return true;
                case "right": key = Keys.Right; return true;
                case "up": key = Keys.Up; return true;
                case "down": key = Keys.Down; return true;
                case "lshift": key = Keys.LeftShift; return true;
                case "rshift": key = Keys.RightShift; return true;
                case "lctrl": key = Keys.LeftControl; return true;
                case "rctrl": key = Keys.RightControl; return true;
                default:
                    return Enum.TryParse(normalized, true, out key);
            }
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        private void EnsureInitialized()
        {
            if (!_initialized || _window == null)
            {
                throw new Exception("Graphics runtime not initialized. Call gfx_init(...) first.");
            }
        }
    }
}
