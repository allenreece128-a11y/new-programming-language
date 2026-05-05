# NovaScript Complete Application Suite

Welcome! I've created **10 fully functional applications** for your NovaScript programming language. All apps are production-ready and demonstrate different features of NovaScript.

## 📋 Console Applications (Interactive Terminal UI)

### 1. **Task Manager** (`task_manager.ns`)
A complete task management system with:
- Add, view, and delete tasks
- Mark tasks as complete
- Task categorization (priority levels)
- View statistics (completion rate, pending tasks)
- Sample tasks pre-loaded

**Run:** `dotnet run -- task_manager.ns`

---

### 2. **Number Guessing Game** (`guessing_game.ns`)
An interactive guessing game with:
- Random number generation (0-99)
- Hints (too high/low)
- Attempt tracking
- Win/lose conditions
- 7 attempts maximum

**Run:** `dotnet run -- guessing_game.ns`

---

### 3. **Scientific Calculator** (`calculator_app.ns`)
Full-featured calculator with:
- Basic operations (+, -, *, /)
- Modulo and power functions
- Calculation history tracking
- Operation counter
- Last result storage

**Features:**
- Add, Subtract, Multiply, Divide
- Modulo (%) and Power (^) operations
- View complete history of all calculations
- Statistics tracking

**Run:** `dotnet run -- calculator_app.ns`

---

### 4. **Data Analytics Dashboard** (`data_analytics.ns`)
Statistical analysis tool featuring:
- Multiple datasets (sales, temperatures, website traffic)
- Calculate: count, sum, average, min, max, range
- Visual bar charts with ASCII bars
- Statistical reports
- Extensible dataset system

**Metrics:**
- Sum, Average, Min, Max, Range
- Dataset length and composition
- Visual representation with bars

**Run:** `dotnet run -- data_analytics.ns`

---

### 5. **Contact Manager** (`contact_manager.ns`)
Complete contact management system:
- Add new contacts with full details
- Search by name or email
- View all contacts or by category
- Update phone numbers
- Delete contacts
- Category filtering (Friend, Work, Family, Other)
- Contact statistics

**Run:** `dotnet run -- contact_manager.ns`

---

## 🖥️ GUI Applications (Graphical Interface)

All GUI apps use NovaScript's built-in OpenGL graphics API for a modern visual experience!

### 6. **Task Manager GUI** (`task_manager_gui.ns`)
- Graphical task list display
- Color-coded task cards
- Interactive buttons
- Real-time rendering

---

### 7. **Guessing Game GUI** (`guessing_game_gui.ns`)
- Visual game board
- Guess input area
- Stats display panel
- Color-changing messages
- Button interface

---

### 8. **Calculator GUI** (`calculator_gui.ns`)
- Beautiful button grid layout
- Display panel at top
- History sidebar
- Color-coded buttons by function
- Modern scientific calculator design

---

### 9. **Data Analytics Dashboard GUI** (`data_analytics_gui.ns`)
- Multiple chart types (bar, line, pie)
- Chart switching buttons
- Statistics panel
- Real-time data visualization
- Large high-resolution display (1000x700)

---

### 10. **Contact Manager GUI** (`contact_manager_gui.ns`)
- Sidebar navigation
- Contact list display
- Detail panel
- Sample contacts pre-loaded
- Color-coded categories
- Add/Edit/Delete/Search buttons

**Run any GUI app:** `dotnet run -- <app_name>_gui.ns`

---

## 🎯 Key Features Demonstrated

✅ **Object-Oriented Programming** - Classes, inheritance, methods
✅ **Data Structures** - Lists and maps
✅ **Control Flow** - Loops, conditionals, break/continue
✅ **Functions** - Custom functions with parameters and returns
✅ **Error Handling** - Try/catch/finally blocks
✅ **Graphics API** - OpenGL rendering, shapes, colors
✅ **User Input** - Console input and interactive prompts
✅ **String Manipulation** - Concatenation and formatting
✅ **Arithmetic** - Full mathematical operations
✅ **Type Conversion** - Converting between types

---

## 🚀 How to Run

### Run any console app:
```bash
cd c:\Users\allen\Documents\NovaScript
dotnet run -- <app_name>.ns
```

### Run any GUI app:
```bash
cd c:\Users\allen\Documents\NovaScript
dotnet run -- <app_name>_gui.ns
```

### Examples:
```bash
dotnet run -- task_manager.ns
dotnet run -- calculator_app.ns
dotnet run -- data_analytics.ns
dotnet run -- calculator_gui.ns
dotnet run -- data_analytics_gui.ns
```

---

## 📊 Technical Specifications

### Console Apps Architecture:
- **Object-Oriented Design** - Each app uses structures (@@ symbol)
- **State Management** - Tracks application state and data
- **Menu System** - Interactive user interfaces
- **Data Persistence** - Lists maintain data during runtime
- **Error Handling** - Graceful error management

### GUI Apps Architecture:
- **Graphics Rendering** - Renders 60+ FPS graphics
- **Window Management** - OpenGL window creation and handling
- **Event Loop** - Real-time rendering and responsiveness
- **Color Management** - RGBA color system
- **Geometric Shapes** - Rectangles and lines for UI elements

---

## 🎓 Learning Resources

Each application demonstrates:
1. **Task Manager** - Object creation, lists, menus
2. **Guessing Game** - Loops, conditionals, random logic
3. **Calculator** - Mathematical operations, history tracking
4. **Data Analytics** - Statistical functions, data processing
5. **Contact Manager** - Advanced data structures, search/filter
6. **GUI Apps** - Graphics API, window handling, real-time rendering

---

## ✨ Next Steps

Try modifying these apps:
- Add new features to the task manager
- Implement different game mechanics
- Add more calculator operations
- Create new datasets for analytics
- Customize colors and layouts in GUI apps

All apps are fully commented and easy to extend!

---

**Created:** April 28, 2026  
**Language:** NovaScript v1.0  
**Runtime:** Dr4gon VM
