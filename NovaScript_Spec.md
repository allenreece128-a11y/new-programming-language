# NovaScript Specification (Version 1.0)

NovaScript is a high-performance, minimalist programming language designed for AI-driven development and modern high-concurrency systems.

## 1. Syntax Overview
NovaScript uses a clean, iconic syntax. Indentation is optional, but blocks are defined by `::` (start) and `;;` (end).

### 1.1 Keywords (Iconic Dictionary)
- `$`: Variable declaration (mutable)
- `#`: Variable declaration (constant/immutable)
- `!!`: Output / Signal
- `~`: If statement
- `?`: Else statement
- `&`: Function definition
- `*`: Loop
- `@@`: Structure / Class
- `^`: Error handling / Try
- `!`: Catching an error
- `!!^`: Finally block
- `>>`: Return statement
- `import`: Import another `.ns` file
- `::`: Start of block
- `;;`: End of block
- `!!>` : Break loop
- `!!<` : Continue loop
- `(+)`: Boolean True
- `(-)`: Boolean False
- `(_)`: Null/None

## 2. Variable Declaration
Variables are categorized by their stability.
```ns
$ count = 10;  // Mutable
# max = 100; // Immutable
```

## 3. Conditional Statements
```ns
~ count > max ::
    !! "Value exceeded!";
;; ? ::
    !! "Value within limits.";
;;
```

## 4. Loops
The `*` symbol handles iteration.
```ns
$ count = 0;
* count < 10 ::
    !! count;
    count = count + 1;
;;
```
Loop controls:
- `!!>` breaks out of the current loop.
- `!!<` skips to the next loop iteration.

## 5. Functions
```ns
& calculateSum($ x, $ y) ::
    >> x + y;
;;
```

## 6. Objects (Structures)
Structures are defined as collections of properties and functions.
```ns
@@ User ::
    $ id;
    $ name;

    & init($ id_val, $ name_val) ::
        id = id_val;
        name = name_val;
    ;;

    & status() ::
        !! "User " + name + " (ID: " + id + ")";
    ;;
;;

$ myUser = new User(1, "Alice");
myUser.status();
```

## 6.1 Modules (Import)
Use `import` to execute another NovaScript file. Relative paths resolve from the current file.
```ns
import "module_math.ns";
!! add(2, 3);
```

## 7. Error Handling
```ns
^ ::
    $ result = 10 / 0;
;; ! err ::
    !! "Error detected: " + err;
;; !!^ ::
    !! "Cleanup phase.";
;;
```

## 8. Standard Library (Core SDK)
NovaScript includes a set of native functions for common tasks.

### 8.1 Core Functions
- `input($ prompt)`: Displays a prompt and reads a line of text from the console.
- `toNum($ value)`: Converts a string or value to a number.
- Container functions:
  - `list_new()`
  - `list_len($ list)`
  - `list_push($ list, $ value)`
  - `list_insert($ list, $ index, $ value)`
  - `list_get($ list, $ index)`
  - `list_set($ list, $ index, $ value)`
  - `list_remove($ list, $ index)`
  - `list_pop($ list)`
  - `map_new()`
  - `map_len($ map)`
  - `map_set($ map, $ key, $ value)`
  - `map_get($ map, $ key)`
  - `map_has($ map, $ key)`
  - `map_remove($ map, $ key)`
  - `map_keys($ map)` returns a list of keys.

### 8.2 Concept SDK
- `Core.IO`: Basic input/output functions.
- `Core.Math`: High-performance geometric and algebraic computations.
- `Core.Net`: Distributed networking and AI model interfacing.
- `Core.Thread`: Concurrent execution and parallel processing.

### 8.3 Graphics + Shader Runtime (OpenGL)
NovaScript includes a native real-time graphics layer backed by OpenTK/OpenGL.

Window and frame loop:
- `gfx_init($ width, $ height, $ title)`
- `gfx_poll()`
- `gfx_running()`
- `gfx_present()`
- `gfx_close()`
- `gfx_quit()`

Canvas drawing (RGBA 0-255):
- `gfx_clear($ r, $ g, $ b, $ a)`
- `gfx_set_clear_color($ r, $ g, $ b, $ a)` (OpenGL backbuffer clear color)
- `gfx_pixel($ x, $ y, $ r, $ g, $ b, $ a)`
- `gfx_line($ x1, $ y1, $ x2, $ y2, $ r, $ g, $ b, $ a)`
- `gfx_rect($ x, $ y, $ w, $ h, $ r, $ g, $ b, $ a)` (filled)
- `gfx_rect_line($ x, $ y, $ w, $ h, $ r, $ g, $ b, $ a)` (outline)
- `gfx_text($ x, $ y, $ text, $ scale, $ r, $ g, $ b, $ a)` (5x7 bitmap text)
- `gfx_image($ path, $ x, $ y, $ w, $ h)` (draw image file to canvas)
- `gfx_resize($ width, $ height)`
- `gfx_set_title($ title)`
- `gfx_set_vsync($ enabled)`
- `gfx_width()`
- `gfx_height()`

Input and timing:
- `input_key_down($ keyName)`
- `input_mouse_x()`
- `input_mouse_y()`
- `input_mouse_down($ buttonIndex)`
- `time_ticks()`
- `time_delta()`
- `time_sleep($ ms)`

Shader API:
- `shader_create($ vertexSource, $ fragmentSource)` returns shader handle.
- `shader_use($ shaderHandle)`
- `shader_use_default()`
- `shader_destroy($ shaderHandle)`
- `shader_uniform1f($ shaderHandle, $ name, $ value)`
- `shader_uniform1i($ shaderHandle, $ name, $ value)`
- `shader_uniform2f($ shaderHandle, $ name, $ x, $ y)`
- `shader_uniform3f($ shaderHandle, $ name, $ x, $ y, $ z)`
- `shader_uniform4f($ shaderHandle, $ name, $ x, $ y, $ z, $ w)`

Chart API:
- `chart_config($ type, $ title, $ eVariable, $ dVariable)` where `$ type` is `line`, `bar`, `pie`, or `pigraph`.
- `chart_set_type($ type)`
- `chart_set_title($ title)`
- `chart_set_variables($ eVariable, $ dVariable)`
- `chart_add_point($ seriesName, $ xLabel, $ yValue)` for line/bar datasets.
- `chart_add_xy($ xLabel, $ yValue)` convenience overload for a single default series.
- `chart_add_slice($ label, $ value)` for pie datasets.
- `chart_clear_data()`
- `chart_render()` draws the chart with the built-in dark neon style.

## 9. File Extension & Icon
- **Extension**: `.ns`
- **Icon Concept**: A minimalist, dark-mode square with a glowing white circle in the center. A vertical violet "energy streak" bisects the circle. The letters "NS" are embossed in a metallic silver, futuristic sans-serif font at the bottom-right corner.

## 10. Implementation Model
NovaScript is designed to be **Transpiled to C# (LLVM-backed)** or interpreted by the **Dr4gon Virtual Machine (DVM)**. 
1. **Parser**: Generates an Abstract Syntax Tree (AST).
2. **Optimizer**: Uses AI-driven heuristics to predict memory usage and parallelize `*` loops.
3. **Execution**: Compiles to machine code via LLVM for maximum performance.
