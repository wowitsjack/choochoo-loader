# ChooChoo

ChooChoo is a Proton/WINE gaming focused trainer and DLL loader featuring a dynamic UI and extensive functionality. Designed for seamless operation under both Windows and Proton/Wine environments, it provides users with an intuitive way to manage and launch games, trainers, and additional executables or DLLs.

It is designed for launching trainers (FLiNG etc), patchers, and ensuring that they can all be recognised and see each other in the Proton/WINE context, and perform the memory access required to load cheats, or do DLL injection.

### 📥 Download ChooChoo

Get the latest version of **ChooChoo** from the official releases page on GitHub. Click the big green button below to download:

[![Download ChooChoo](https://img.shields.io/badge/Download-ChooChoo-success?style=for-the-badge&logo=github)](https://github.com/wowitsjack/choochoo-loader/releases)


### HOW-TO (Steam Deck):

### Adding ChooChoo to Steam:

- Open **Steam** on your **Steam Deck** in Desktop Mode.  
- Navigate to **Games** and select **Add a Non-Steam Game to My Library**.  
- Browse to `choochoo.exe` and add it to your library.  

### Launching ChooChoo:

- Open **Properties** for **ChooChoo** in your Steam Library.  
- Enable **Compatibility Mode** and select a Proton version (Proton 9+ is recommended).  
- Launch **ChooChoo** from your Steam Library.

  

### HOW-TO (macOS with Whisky):

### Adding ChooChoo to Whisky:

- Install **Whisky** on your **macOS** system if you have not already.  
- Open **Whisky** and create a new **bottle**.  
- Inside the bottle, use the **"Run Executable"** option to browse to `choochoo.exe` and add it.  

### Launching ChooChoo:

- Open **Whisky** and select the bottle where **ChooChoo** is installed.  
- Enable **DXVK and other compatibility settings** if needed.  
- Click **Run** to launch **ChooChoo** inside Whisky.  



## Features

- **Game & Trainer Launching**  
  Easily configure and launch a game alongside a trainer.

- **Multiple Additional EXEs/DLLs**  
  Supports launching up to 5 additional executables or dynamically loading DLLs into the current process.

- **Profile System**  
  - Save and load multiple profiles for different configurations.
  - Editable combo box for quick profile selection.
  - Profiles are stored in the `profiles` folder.

- **Auto Launcher**  
  - Automatically launches the last-used configuration on startup.
  - Displays a countdown modal with a cancel option.
  - Saves current settings to `profiles/last.ini`.

- **Recent Files (MRU) Support**  
  - Remembers recently used game, trainer, and additional EXE/DLL paths.
  - MRU entries are stored in `recent.ini`.

- **Command Line Handling**  
  - Parses full command-line input and determines the working directory dynamically.
  - Supports launching `.bat`, `.cmd`, and `.com` scripts via `cmd.exe /C`.
  - Loads `.dll` files using `LoadLibraryW`.

- **Windows UI with Proton/Wine Compatibility**  
  - Custom UI with GroupBox background fixes for Wine/Proton.
  - Uses `CreateJobObjectW` to ensure all launched processes terminate on exit.

## Compilation

ChooChoo is designed to be compiled with `gcc` (MinGW) and requires Windows libraries.

### **Build Instructions (MinGW GCC)**

```sh
gcc choochoo.c -mwindows -o choochoo.exe -lcomctl32 -lshlwapi
```

### **Build Dependencies**
- Windows API (`windows.h`)
- Common Controls Library (`comctl32.lib`)
- Shell API (`shlwapi.lib`)

## Usage

1. **Run `choochoo.exe`**  
   Launches the main UI where you can configure game, trainer, and additional executables.

2. **Select Paths**  
   - Use the **Game Path** and **Trainer Path** fields to set the primary executable files.
   - Add up to **5 additional EXEs or DLLs**, enabling/disabling them as needed.

3. **Profiles**  
   - Save and load profiles from the right-side **Profiles** panel.
   - Click **Refresh** to reload the available profile list.

4. **Auto Launching**  
   - Enable the **Auto Launcher** checkbox to automatically start the last-used configuration.
   - The auto-launch countdown can be canceled before execution.

5. **Launching**  
   - Press the **Launch** button to start the configured game, trainer, and additional processes.
   - Status logs indicate success (`LAUNCHED`) or failure (`FAILED`).

## File Structure

```
ChooChoo/
│── choochoo.exe             # Compiled executable
│── choochoo.c               # Source code
│── profiles/                # Saved profiles folder
│   ├── last.ini             # Auto-launch profile
│   ├── custom_profile.ini   # User-created profiles
│── recent.ini               # MRU (Most Recently Used) paths
│── autolaunch.ini           # Stores auto-launch settings
```

## License

This project is open-source under the **MIT License**.

## Notes

- **Proton/Wine Compatibility**: ChooChoo includes UI fixes for Wine-based environments, ensuring better rendering of group boxes.
- **MRU Handling**: The application maintains a history of previously used paths for quick access.
- **Process Cleanup**: Uses Windows Job Objects to ensure all launched processes are terminated when the main application exits.

## Contributing

Contributions are welcome! Feel free to submit issues, pull requests, or feature requests.
