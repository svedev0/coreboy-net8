# CoreBoy Revived

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
native desktop support for Linux and MacOS were temporarily cut but are now
being implemented (at least for Linux).

## Contributing

Please feel free to contibute to the project if you have ideas for features,
bug reports, or improvements. Pull requests are welcome.

## TODO

| Category    | Summary                                                        |
| ----------- | -------------------------------------------------------------- |
| **Task**    | Write documentation                                            |
| **Task**    | Improve performace of animations                               |
| **Feature** | Implement native desktop support for Linux (in progress)       |
| **Feature** | Implement native desktop support for MacOS                     |

## Build & run

### Windows

1. Clone this repo
2. Run: `cd coreboy-net8`
3. Run: `dotnet restore`
4. Run: `dotnet build coreboy.win -c Release -r win-x64`
5. Run: `.\coreboy.win\bin\Release\net8.0-windows\win-x64\coreboy.win.exe`

### Linux

1. Clone this repo
2. Run: `cd coreboy-net8`
3. Run: `dotnet restore`
4. Run: `dotnet build coreboy.avalonia.Desktop -c Release -r linux-x64`
5. Run: `./coreboy.avalonia.Desktop/bin/Release/net8.0/linux-x64/coreboy.avalonia.Desktop`
