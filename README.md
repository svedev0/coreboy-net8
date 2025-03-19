# CoreBoy

A GameBoy/GameBoy Color emulator written in C# .NET 8.

## Introduction

The aim of this project is to port David Whitney's
[CoreBoy](https://github.com/davidwhitney/CoreBoy) (a now abandoned project,
originally written in .NET Core 3.1) to .NET 8 and hopefully breathe some new
life into it.

This project is still in its early stages. All of the original code is updated
to be compatible with .NET 8 and most of the original code has been refactored
and/or rewritten in order to ensure compatibility and modern best practices.

Some sacrifices have been made in order to increase velocity. For instance,
native desktop support for MacOS has been cut but the goal is to implement it
in the future.

## Contributing

Please feel free to contibute to the project if you have ideas for features,
bug reports, or improvements. Pull requests are welcome.

## TODO

| Category    | Summary                                                  |
| ----------- | -------------------------------------------------------- |
| **Task**    | Write documentation                                      |
| **Task**    | Improve performace of animations                         |
| **Task**    | Adjust sound pitch when fast-forwarding                  |
| **Feature** | Native desktop support for MacOS                         |
| **Feature** | Sending controller inputs from code                      |
| **Feature** | A way to measure and display emulation speed             |
| **Bug**     | Audio not working in Linux build                         |
| **Bug**     | Uncapped frame rate in Linux build                       |

## Build & run

### Windows

1. Clone this repo
2. Run: `cd coreboy-net8`
3. Run: `dotnet restore`
4. Run: `dotnet publish coreboy.avalonia.desktop -c Release -r win-x64`
5. Run: `cd coreboy.avalonia.desktop\bin\Release\net8.0\win-x64\publish`
6. Run: `.\coreboy.avalonia.desktop.exe`

### Linux

1. Clone this repo
2. Run: `cd coreboy-net8`
3. Run: `dotnet restore`
4. Run: `dotnet publish coreboy.avalonia.desktop -c Release -r linux-x64`
5. Run: `cd coreboy.avalonia.desktop/bin/Release/net8.0/linux-x64/publish`
6. Run: `./coreboy.avalonia.desktop`

### Interactive CLI

When running the CLI in interactive mode (`--interactive`) or with any other
named argument/flag, you must specify `-r` (or `--rom`) before the ROM file
path. For example: `coreboy.cli -r "path/to/rom" --interactive`
