# WinPOS

A modern Point of Sale (POS) system for Windows built with .NET 10 and WPF.

## Features

- 🛒 Complete POS functionality for sales transactions
- 📊 Admin panel for product and category management
- 🌍 Multi-language support (English, German, French, Turkish)
- 🎨 Dark and Light theme support
- 💾 SQLite database for data persistence
- 🖥️ Touch-friendly interface for POS terminals

## Requirements

- Windows 10/11
- .NET 10 SDK or later

## Getting Started

### Building the Project

```bash
dotnet build WinPOS/WinPOS.csproj
```

### Running the Application

```bash
dotnet run --project WinPOS/WinPOS.csproj
```

## Project Structure

- **WinPOS/** - Main application code
  - **Data/** - Database models and operations
  - **ViewModels/** - MVVM ViewModels
  - **Languages/** - Localization resources
  - **Themes/** - UI themes (Dark/Light)
  - **Helpers/** - Utility classes

## Technology Stack

- **.NET 10** - Application framework
- **WPF** - Windows Presentation Foundation for UI
- **SQLite** - Embedded database
- **MVVM** - Architectural pattern

## License

Please refer to the repository for licensing information.
