# Nvidia Gamma Replica

A modern, lightweight WPF-based desktop application for fine-tuning display color settings on Windows. It replicates and enhances the color adjustment functionality found in the Nvidia Control Panel, providing a streamlined interface for managing Gamma, Channel Balance, Brightness, and Contrast across multiple monitors.

[![Nvidia Gamma Replica Icon](NvidiaGammaReplica/icon.ico)](NvidiaGammaReplica/icon.ico)

### 🖥️ Multi-Monitor Support
Independently adjust settings for each connected display. The application automatically detects all active monitors using friendly naming (e.g. `Monitor 1 (Primary)`, `Monitor 2`) and allows you to target them individually or apply resets globally.

### 🔍 Screen Identifier
Unsure which screen is which? Click the **Identify** button next to the monitor dropdown to flash a large, glowing identifier overlay (`1`, `2`, etc.) in the center of the respective physical display.

### 🎨 Precision Color Tuning
Fine-tune the overall **Master Gamma** (Range: 0.3 to 2.8) or dive into **Channel Balance** to adjust Red, Green, and Blue channels independently. This is perfect for correcting color casts or achieving a specific look for different tasks.

### 💡 Brightness & Contrast
Direct control over display luminance and dynamic range. These adjustments are applied on top of the gamma curve for maximum flexibility.

### 💾 Smart Presets
Quickly switch between user-defined presets (**Preset 1**, **Preset 2**, **Preset 3**, **Preset 4**). You can easily overwrite any preset with your current settings by simply **right-clicking** the preset button. Presets can also be selected and applied directly from the taskbar system tray context menu.

### 📈 Real-time Curve Preview
A built-in visualizer shows you exactly how your adjustments are affecting the gamma ramp before you apply them.

### 🎬 Smooth Transitions
When switching between presets or resetting to default, the slider values and hardware gamma colors transition smoothly over 250ms rather than popping instantly, creating a premium visual experience.

### 🛠️ System Integration
- **Tray Operation**: Minimize the application to the system tray to keep your taskbar clean.
- **On-Screen Display (OSD)**: Displays a clean, transparent, auto-fading overlay showing the current gamma value when adjusting settings globally via hotkeys (`Ctrl+Alt+Up`/`Down`/`0`).
- **Auto-Start**: Optional "Start with Windows" feature to ensure your color profile is applied the moment you log in.
- **Low Level**: Uses the GDI32 API (`SetDeviceGammaRamp`) for high-performance, hardware-level adjustments.

## Tech Stack
*   **.NET 9**
*   **C# 13**
*   **WPF** (Windows Presentation Foundation)
*   **Hardcodet.NotifyIcon.Wpf** for tray management

## Installation / Building

### Prerequisites
*   Windows 10/11
*   .NET 9.0 SDK (for building) or Runtime (for running)

### Build from Source
1. Clone the repository.
2. Open `NvidiaGammaReplica.sln` in Visual Studio 2022 or JetBrains Rider.
3. Restore NuGet packages.
4. Build the solution in **Release** mode.

## Usage
1. Run `NvidiaGammaReplica.exe`.
2. Select the **Display** you wish to adjust from the dropdown or click **Identify** to verify your screens.
3. Move the sliders to achieve your desired color profile.
4. (Optional) Right-click a **Preset** button to save your current setup.
5. Close the window to minimize to the system tray.

## License
Nvidia Gamma Replica is licensed under the MIT License. See the `LICENSE` file for more information.

---
*Note: Nvidia Gamma Replica is a third-party utility and is not affiliated with NVIDIA Corporation.*
