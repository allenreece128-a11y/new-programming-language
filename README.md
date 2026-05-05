# genesis - experimental Language

Welcome to the future of coding. NovaScript is a high-performance, minimalist programming language with an iconic syntax.

## 1. How to Run genesis
NovaScript is powered by the **Dr4gon VM** (DVM). It features a fully functional interpreter and an integrated terminal environment to execute NovaScript code directly.

### To run a file
Open your terminal in the project directory and use the following command:
```bash
 dotnet run -- hello_world.ns
```

### Interactive Terminal (NovaTerminal)
Start the NovaScript terminal:
```bash
 dotnet run
```
You’ll see a prompt `ns>` where you can type genesis code directly. Use `::` to open and `;;` to close multi-line blocks.

Built-in commands:
```
.help            Show help
.exit | .quit    Exit the terminal
.clear           Clear the screen
.vars            List variables in current scopes
.run <file.ns>   Execute a NovaScript file in this session
.graph           Interactive graph builder (line/bar/pie)
.cancel          Clear unfinished multi-line input
```

Example session:
```
ns> $ count = 2;
ns> !! count * 3;
6
ns> ~ count > 1 ::
...     !! "ok";
... ;;
ok
```

## 2. How to Start Coding
1. **Create a File**: Create a new file with the `.ns` extension (e.g., `main.ns`).
2. **Write Syntax**: Use the **Iconic Syntax** (defined in `NovaScript_Spec.md`):
   - `!!` for output.
   - `$` for mutable variables, `#` for immutable.
   - `::` and `;;` for code blocks.
   - `(+)`, `(-)`, `(_)` for True, False, and Null.
3. **Execute**: Run your new file using the `dotnet run -- <file>.ns` command.

## 3. Quick Syntax Cheat Sheet
- **Print**: `!! "Text";`
- **Variable**: `$ name = "Alice";`
- **If/Else**: `~ condition :: ... ;; ? :: ... ;;`
- **Loop**: `* condition :: ... ;;`
- **Break**: `!!>;`
- **Continue**: `!!<;`
- **Function**: `& myFunc($ arg) :: ... ;;`
- **Structure**: `@@ User :: $ id; & init($ i) :: id = i; ;; ;;`
- **Instance**: `$ u = new User(1);`
- **Import Module**: `import "module_math.ns";`

## 4. Graphics + Shader API
All functions below are built in and available from any `.ns` script:

Core containers:
- `list_new()`
- `list_len(list)`
- `list_push(list, value)`
- `list_insert(list, index, value)`
- `list_get(list, index)`
- `list_set(list, index, value)`
- `list_remove(list, index)`
- `list_pop(list)`
- `map_new()`
- `map_len(map)`
- `map_set(map, key, value)`
- `map_get(map, key)`
- `map_has(map, key)`
- `map_remove(map, key)`
- `map_keys(map)` (returns a list)

- `gfx_init(width, height, title)`
- `gfx_poll()`
- `gfx_running()`
- `gfx_present()`
- `gfx_clear(r, g, b, a)`
- `gfx_set_clear_color(r, g, b, a)`
- `gfx_pixel(x, y, r, g, b, a)`
- `gfx_line(x1, y1, x2, y2, r, g, b, a)`
- `gfx_rect(x, y, w, h, r, g, b, a)` (filled)
- `gfx_rect_line(x, y, w, h, r, g, b, a)` (outline)
- `gfx_text(x, y, text, scale, r, g, b, a)` (5x7 bitmap text)
- `gfx_image(path, x, y, w, h)` (draw image file to canvas)
- `gfx_resize(width, height)`
- `gfx_set_title(title)`
- `gfx_set_vsync(flag)`
- `gfx_width()`
- `gfx_height()`
- `gfx_close()`
- `gfx_quit()`

Input and timing:
- `input_key_down(keyName)` (`Escape`, `Space`, `A`, `Left`, etc.)
- `input_mouse_x()`, `input_mouse_y()`
- `input_mouse_down(buttonIndex)` (`0` left, `1` right, `2` middle)
- `time_ticks()` (milliseconds)
- `time_delta()` (milliseconds since last `gfx_poll()`)
- `time_sleep(ms)`

Shader API:
- `shader_create(vertexSource, fragmentSource)` -> shader handle
- `shader_use(shaderHandle)`
- `shader_use_default()`
- `shader_destroy(shaderHandle)`
- `shader_uniform1f(shaderHandle, name, value)`
- `shader_uniform1i(shaderHandle, name, value)`
- `shader_uniform2f(shaderHandle, name, x, y)`
- `shader_uniform3f(shaderHandle, name, x, y, z)`
- `shader_uniform4f(shaderHandle, name, x, y, z, w)`

Chart API:
- `chart_config(type, title, eVariable, dVariable)` (`type`: `line`, `bar`, `pie`/`pigraph`)
- `chart_set_type(type)`
- `chart_set_title(title)`
- `chart_set_variables(eVariable, dVariable)`
- `chart_add_point(seriesName, xLabel, yValue)`
- `chart_add_xy(xLabel, yValue)` (quick add to `Series 1`)
- `chart_add_slice(label, value)` (pie slices)
- `chart_clear_data()`
- `chart_render()`

Refer to `NovaScript_Spec.md` for the full language documentation.
