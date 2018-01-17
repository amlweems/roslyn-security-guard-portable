# Portable Roslyn Security Guard

Fork of [roslyn-security-guard](https://github.com/dotnet-security-guard/roslyn-security-guard)
which builds on Linux and macOS with [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/).
This fork removes the need to use this project as a Visual Studio extension and
allows it to be run from a Docker container against a directory of C# code.

## Build

```
$ docker build -t roslyn-security-guard .
```

## Run

```
$ docker run -v SOURCE_DIR:/src roslyn-security-guard-portable /src
/src/Example/AdminController.cs(406,24): warning SG0017: Request validation is disabled
/src/Example/Messages.cs(46,43): warning SG0005: Weak random generator
/src/Example/CryptoTools.cs(120,27): warning SG0005: Weak random generator
/src/Example/ExampleController.cs(202,9): warning SG0029: Potential XSS vulnerability
/src/Example/ExampleController.cs(203,9): warning SG0029: Potential XSS vulnerability
/src/Example/ExampleController.cs(225,9): warning SG0029: Potential XSS vulnerability
/src/Example/CryptoDataContext.cs(25,27): warning SG0005: Weak random generator
```

