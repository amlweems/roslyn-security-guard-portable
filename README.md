# Portable Roslyn Security Guard

Fork of [roslyn-security-guard](https://github.com/dotnet-security-guard/roslyn-security-guard)
which builds on Linux and macOS with [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/).
This fork removes the need to use this project as a Visual Studio extension and
allows it to be run from a Docker container against a directory of C# code.

## Build

```
docker build -t roslyn-security-guard .
```

## Run

```
docker run -v SOURCE_DIR:/src roslyn-security-guard-portable /src
```

