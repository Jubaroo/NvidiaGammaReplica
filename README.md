# Nvidia Gamma Replica

A modern, lightweight WPF-based desktop application for fine-tuning display color settings on Windows. It replicates and enhances the color adjustment functionality found in the Nvidia Control Panel, providing a streamlined interface for managing Gamma, Channel Balance, Brightness, and Contrast across multiple monitors.

![UI Preview](NvidiaGammaReplica/icon.ico) <!-- Placeholder for actual screenshot -->

## Features

- **Multi-Monitor Support**: Independently adjust settings for each connected display.
- **Master Gamma Control**: Fine-tune the overall gamma curve (Range: 0.3 to 2.8).
- **Per-Channel Balance**: Adjust Red, Green, and Blue channels independently to correct color casts.
- **Brightness & Contrast**: Precise control over display luminance and dynamic range.
- **Presets**: Quickly switch between user-defined presets (Day, Night, Gaming, Movie). Right-click a preset to save current settings.
- **Curve Preview**: Visual representation of the applied gamma curve in real-time.
- **System Tray Integration**: Minimize to tray for unobtrusive operation.
- **Auto-Start**: Option to start automatically with Windows.
- **Global Hotkeys**: Built-in support for hotkey-driven adjustments (extendable).

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 10.0 Runtime

### Installation

1. Download the latest release from the [Releases](https://github.com/yourusername/NvidiaGammaReplica/releases) page.
2. Extract the files and run `NvidiaGammaReplica.exe`.

## Technical Details

The application uses the Windows GDI32 API (`SetDeviceGammaRamp`) to manipulate the hardware lookup tables (LUT) of your graphics card. This ensures low-level, high-performance color adjustment that works across all applications, including full-screen games.

### Built With

- **Framework**: .NET 10.0 (WPF)
- **UI Components**: [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)
- **Language**: C# 13

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Jarrod Schantz**
