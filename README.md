# Lumina

A modern Windows VPN client powered by WireGuardNT kernel driver, .NET 10, and Avalonia UI.

## Features

- **High Performance** - Native WireGuardNT kernel driver integration
- **Native AOT** - Fast startup with ahead-of-time compilation
- **Modern UI** - Premium Windows 11 visual experience with Avalonia
- **Secure** - DPAPI key encryption, secure memory handling

## Tech Stack

| Layer | Technology |
|-------|------------|
| UI Framework | Avalonia 11.x + Semi.Avalonia |
| MVVM | CommunityToolkit.Mvvm 8.x |
| Runtime | .NET 10 (Native AOT) |
| Interop | LibraryImport (Source Generator) |
| Driver | WireGuardNT (wireguard.dll) |
| Network API | IP Helper API (Iphlpapi.dll) |

## Project Structure

```
Lumina/
├── src/
│   ├── Lumina.App/        # Avalonia UI application
│   ├── Lumina.Core/       # Core business logic
│   └── Lumina.Native/     # P/Invoke definitions
└── tests/
    ├── Lumina.Core.Tests/
    └── Lumina.Native.Tests/
```

## Requirements

- Windows 11
- .NET 10 SDK
- WireGuard for Windows (for wireguard.dll)

## Build

```bash
dotnet build
```

## License

[MIT](LICENSE)
