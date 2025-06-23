# sqlcmd-gui

An attempt to implement a graphical user interface based on SQLCMD for executing parameterized TSQL scripts.

This repository contains a minimal Windows desktop application for executing parameterized SQL scripts with `sqlcmd`.

The application allows you to:

1. Browse for a `.sql` file.
2. Automatically detect SQLCMD variables in the script that are not defined by `:setvar` commands and prompt for their values.
3. Enter SQL Server connection information.
4. Execute the script via the `sqlcmd` command-line tool and display the output.

The source is implemented as a WPF project targeting .NET 6.0.

## Building

Use the .NET SDK on Windows:

```bash
dotnet build SqlcmdGuiApp/SqlcmdGuiApp.csproj
```

## Running

After building, run the produced executable or start it with `dotnet run`:

```bash
dotnet run --project SqlcmdGuiApp/SqlcmdGuiApp.csproj
```

You need the `sqlcmd` utility available in your PATH for the execution step to work.

Any unhandled errors encountered by the application are written to `error.log` in the application directory.
