# GitHub Copilot Instructions for WinPOS

## Project Overview

WinPOS is a Point of Sale (POS) system built with:
- **.NET 10** with C# 13
- **WPF (Windows Presentation Foundation)** for the UI
- **MVVM (Model-View-ViewModel)** architecture pattern
- **SQLite** database via Microsoft.Data.Sqlite
- Multi-language support (English, German, French, Turkish)
- Dark/Light theme support

## Project Structure

```
WinPOS/
├── Data/
│   ├── Admin/          # Admin interface
│   ├── Models/         # Data models (Product, Category, CartItem)
│   ├── PosDb.cs        # Database context and operations
│   └── SettingsStore.cs # Application settings
├── ViewModels/         # MVVM ViewModels
├── Helpers/            # Utility classes (RelayCommand)
├── Languages/          # Localization resources (XAML)
├── Themes/            # Dark and Light themes (XAML)
├── MainWindow.xaml    # Main POS interface
└── *.xaml files       # Various windows and dialogs
```

## Development Guidelines

### Code Style and Conventions

1. **C# Conventions**:
   - Use C# 13 features and modern syntax
   - Enable nullable reference types (`<Nullable>enable</Nullable>`)
   - Use implicit usings (`<ImplicitUsings>enable</ImplicitUsings>`)
   - Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for locals)
   - Use LINQ for data operations where appropriate

2. **XAML Conventions**:
   - Keep XAML clean and readable with proper indentation
   - Use data binding to ViewModels (MVVM pattern)
   - Use resource dictionaries for themes and localization
   - Avoid code-behind when possible; use Commands and data binding

3. **Architecture**:
   - Follow MVVM pattern strictly
   - ViewModels should not reference Views
   - Use RelayCommand for button commands
   - Keep business logic in ViewModels or separate service classes
   - Database operations should be in PosDb.cs

### Database

- **SQLite** is used via Microsoft.Data.Sqlite package
- Database file: `winpos.db` (auto-created in bin directory)
- Schema includes: Products, Categories, Sales, and related tables
- Use parameterized queries to prevent SQL injection
- Database operations are synchronous (SQLite is file-based and fast)

### Localization

- Multi-language support via XAML resource dictionaries in `Languages/` folder
- Supported languages: English (en), German (de-CH), French (fr-CH), Turkish (tr)
- Use `LanguageManager` to switch languages dynamically
- Add new translations to all language files when adding new UI text
- Use `DynamicResource` in XAML for localizable strings

### Themes

- Two themes: Dark and Light (in `Themes/` folder)
- Theme switching managed by `ThemeManager.cs`
- Use theme resources for colors, brushes, and styles
- Ensure new UI elements respect theme colors

### Build and Test

- **Build**: `dotnet build WinPOS/WinPOS.csproj`
- **Run**: `dotnet run --project WinPOS/WinPOS.csproj` (Windows only)
- **Clean**: `dotnet clean WinPOS/WinPOS.csproj`
- Target framework: `net10.0-windows` (requires .NET 10 SDK)
- No automated tests are currently in place

## Security Considerations

1. **Database Security**:
   - Always use parameterized queries
   - Never concatenate user input into SQL strings
   - Validate all user inputs before database operations

2. **Sensitive Data**:
   - Do not log sensitive information (prices, customer data)
   - Keep database files secure (not in version control)
   - Settings should not contain plaintext passwords

3. **Input Validation**:
   - Validate all numeric inputs (prices, quantities)
   - Sanitize text inputs to prevent issues
   - Handle edge cases (negative numbers, zero quantities)

## Common Tasks

### Adding a New Window

1. Create XAML file and code-behind in appropriate directory
2. Create corresponding ViewModel if needed
3. Use `Window` class for dialogs, properly sized
4. Ensure theme support by using theme resources
5. Add localized strings to all language files

### Adding a New Database Entity

1. Create model class in `Data/Models/`
2. Add table creation in `PosDb.cs` initialization
3. Add CRUD methods in `PosDb.cs`
4. Update ViewModels to use new entity

### Modifying UI

1. Edit XAML files for layout changes
2. Use data binding to ViewModels
3. Test with both Dark and Light themes
4. Test with different languages if text is added
5. Ensure responsive design (window resizing)

## Things to Avoid

- ❌ Do not add dependencies without strong justification
- ❌ Do not mix business logic in code-behind files
- ❌ Do not hardcode strings in XAML or C# (use localization)
- ❌ Do not hardcode colors or styles (use theme resources)
- ❌ Do not commit `bin/`, `obj/`, or `.db` files
- ❌ Do not break the MVVM pattern
- ❌ Do not use synchronous blocking calls on UI thread for long operations

## File Naming Patterns

- Windows: `*Window.xaml` and `*Window.xaml.cs`
- ViewModels: `*ViewModel.cs`
- Models: Plain noun (e.g., `Product.cs`, `Category.cs`)
- Themes: `Dark.xaml`, `Light.xaml`
- Languages: `Strings.{locale}.xaml` (e.g., `Strings.en.xaml`)

## Notes

- This is a Windows-only application (WPF)
- Uses .NET 10 (latest at time of creation)
- SQLite database is embedded and lightweight
- UI is designed for touch and mouse input (POS terminals)
- The application is a single-window app with dialog popups
