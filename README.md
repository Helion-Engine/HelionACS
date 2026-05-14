# Helion ACSVM Adapter

This is a C# -> C++ interop library for using ACSVM within Helion. It's very
impractical to create bindings directly to a C++ library like ACSVM which
expects you to inherit from classes, so there's an `extern "C"` C++ layer which
essentially builds an interop-able layer on top of ACSVM. Then, a safe-C# layer
is provided on top of that, so all the nasty details are hidden from the engine
itself.

## Building

Kinda TODO as it needs to be made more cross-platform, but on Linux it should
be something like this. You'll need `dotnet`, `CMake`, `ninja`, and
`ClangSharpPInvokeGenerator` (which can be installed via `dotnet tool restore`
locally to the project, with `dotnet-tools.json`). Then run:

```
mkdir -p ./build-native/linux-x64
cmake -B ./build-native/linux-x64 -G Ninja -DCMAKE_BUILD_TYPE=RelWithDebInfo
cmake --build ./build-native/linux-x64
dotnet tool run ClangSharpPInvokeGenerator -c multi-file generate-helper-types --file ./src/cxx/HelionACS.hpp -n HelionACS.Interop --libraryPath HelionACS-native -o ./src/cs/Generated/
dotnet build
```

Windows Release
```
cmake -B build -G "Visual Studio 17 2022" -A x64
cmake --build build --config Release
```

## Testing

You'll need `acc`, the ACS compiler. Unit tests are used for engine-intended
API surface. You can run them as such, once things are built:

```
acc tests/test.acs tests/test.acs.o
dotnet test
```
