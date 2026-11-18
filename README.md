# Bloomex (v0.2.0)

A lightweight system utility and performance tuning dashboard for Android emulators on Windows.

## Features

- **Frame Rate Synchronization**: Configure and synchronize emulator configuration and boot parameter values.
- **Display & Memory Settings**: Manage DPI, display resolution, and RAM allocations.
- **Game Mod & System Profiler**:
  - High-precision multimedia timer resolution tuning (0.5ms).
  - Shell and compositor management for uninterrupted fullscreen focus.
  - CPU priority and system responsiveness optimization.
  - Memory working set purge and temporary file cleanup (`Ctrl+Shift+P`).
  - Low-latency input handling and customizable key bindings.
- **System Maintenance Tools**: Built-in tools for temporary file cleanup and memory working set optimization.
- **Custom Modern Interface**: Dark violet themed interface with genuine Inter typography and real-time telemetry.

## Building from Source

Bloomex compiles using the standard Windows .NET Framework C# compiler (`csc.exe`):

```cmd
build.bat
```

Output:
- `Bloomex.exe`

## Requirements

- Windows 10 or 11 (64-bit)
- .NET Framework 4.5+ (pre-installed on modern Windows)
- Administrative privileges for low-level system timer and process optimization

## License

MIT License. Developed by susaracreations.
