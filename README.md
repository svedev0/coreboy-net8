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
native desktop support for Linux and MacOS have been removed temporarily but
are planned to be reimplemented in a modern way in the future.

## Contributing

Please feel free to contibute to the project if you have ideas for features,
bug reports, or improvements. Pull requests are welcome.

## TODO

| Category    | Summary                                                        |
| ----------- | -------------------------------------------------------------- |
| **Task**    | Write documentation                                            |
| **Task**    | Replace Windows Forms with a modern desktop framework          |
| **Task**    | Improve performace of animations                               |
| **Feature** | Implement native desktop support for Linux                     |
| **Feature** | Implement native desktop support for MacOS                     |
| **Bug**     | Fix bug where some sprites flicker/lag behind in certain games |
