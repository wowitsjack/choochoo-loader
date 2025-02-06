# **🚂 ChooChoo**  
**Proton/WINE Trainer & DLL Loader**  
A sleek, gaming-focused tool that elegantly handles launches of games with trainers/mods (FLiNG, WeMod, etc.), patches, and up to 5 extra executables or DLLs, bypassing the issues of launching mods/patches in WINE/Proton enviroments.

<p align="center">
  <img src="choochoo.png" width="40%" alt="ChooChoo Logo" />
</p>

---

## **Download & Links**

[![Download ChooChoo](https://img.shields.io/badge/Download-ChooChoo-green?style=for-the-badge&logo=github)](https://github.com/wowitsjack/choochoo-loader/releases) 

[![GitHub Releases](https://img.shields.io/github/release/wowitsjack/choochoo-loader/all.svg?style=for-the-badge)](https://github.com/wowitsjack/choochoo-loader/releases)  
[![Platforms: macOS | Linux | Steam Deck | Windows ](https://img.shields.io/badge/Platforms-Windows%20|%20macOS%20|%20Linux%20|%20Steam%20Deck-blue?style=for-the-badge&logo=steam)](https://github.com/wowitsjack/choochoo-loader)  

---


### **Why is this Needed for Proton/WINE?**  

Running game trainers, patches, and DLL injectors in **Proton** or **WINE** can be problematic due to compatibility issues, anti-cheat false positives, and differences in Windows API implementations. Many game trainers and mods rely on system calls that work natively on Windows but fail under Proton/WINE.  

**ChooChoo** solves these issues by:  

✅ **Ensuring Proper Trainer Execution** – Many trainers rely on system-level hooks and memory modifications that fail in WINE. ChooChoo makes sure they load properly.  

✅ **DLL Injection Support** – Some patches, mods, or debuggers need to inject DLLs into the game process, which can fail in WINE without proper handling.  

✅ **Multiple Executable Launching** – Games requiring launchers, mod frameworks, or patches alongside the main executable can be difficult to set up in Proton.  

✅ **Proton/WINE UI Compatibility Fixes** – Prevents common graphical/UI bugs that occur when running trainers in a non-native Windows environment.  

✅ **Seamless Steam Deck Integration** – Works effortlessly with Steam’s Proton compatibility layer, making it easy to add trainers and patches on the go.  

Whether you are playing on **Linux**, **macOS (via Whisky)**, or **Steam Deck**, **ChooChoo** makes sure that your trainers, DLLs, and patches run just...work.

---

## **Features At a Glance**

| **Feature**                   | **Description**                                                                                   |
|------------------------------|---------------------------------------------------------------------------------------------------|
| **Dual Launch**              | Automatically start both game and trainer (or any EXE) together.                                  |
| **Additional EXEs/DLLs**     | Inject up to 5 extra binaries (patches, scripts, additional trainers).                            |
| **Profiles**                 | Save, load, and auto-launch configurations from custom profile files.                             |
| **Auto Launcher**            | Optional startup mode that launches your last-used profile immediately (with a configurable delay).|
| **Recent Files (MRU)**       | Quickly reselect your most recently used game or trainer executables.                             |
| **Proton & WINE Compatible** | Streamlined UI adjustments for smooth operation on Steam Deck, Wine, or Proton.                  |

---

## **Quick Start (Steam Deck)**

1. **Switch to Desktop Mode**  
   - Tap the Steam Deck’s **Power** button → **Switch to Desktop**.

2. **Add ChooChoo to Steam**  
   - Open Steam on your Deck (in Desktop Mode).  
   - Go to **Games** → **Add a Non-Steam Game to My Library** → Select `choochoo.exe`.

3. **Enable Proton**  
   - In your Steam Library, **right-click** on ChooChoo → **Properties** → **Compatibility**.  
   - Check **Force the use…** and pick a Proton version (Proton 9+ recommended).

4. **Configure & Launch**  
   - Click **Play** to open ChooChoo.  
   - Choose your **Game Path**, **Trainer Path**, and any extra DLLs/EXEs.  
   - (Optional) **Save a Profile** and enable **Auto Launcher**.  
   - Finally, hit **Launch**.
  
 🚀 **Quick Tip for Steam Deck Users!** 🎮  

If you're using **WINE/Proton** and can't find your Steam games, create a **symlink** to make them easily accessible:  

```sh
ln -s ~/.steam/steam/steamapps/common ~/STEAMGAMES
```
## WeMod/Trainer Not Launching (Fix) (Proton/WINE)  

If trainers like **WeMod** aren’t working, remove **Wine-Mono** and install .NET instead using **Protontricks** or **Heroic Launcher**.  

### Using Protontricks  

1. Install Protontricks (if not installed)  
   ```flatpak install com.github.Matoking.protontricks``` 

2. Open Protontricks & Select Your Game  
   ```protontricks --gui```  or launch it from the menu in Desktop mode.
   - Select your **game/trainer bottle**  
   - Choose **Run Wine Control Panel**  

3. Uninstall Wine-Mono  
   - In **Control Panel**, go to **Add/Remove Programs**  
   - Find **Wine Mono Windows Support**  
   - Click **Uninstall** and follow the prompts  

4. Install .NET Framework (if needed)  
   - Download .NET from **Microsoft’s official site**  
   - Inside Protontricks, select **Run Wine File Manager**  
   - Run the .NET installer inside the Wine bottle  

---

### Using Heroic Launcher  

1. Open **Heroic Launcher**  
2. Go to **Wine Manager** → **Wine Tools**  
3. Click **Run Wine Control Panel**  
4. Follow steps 3-4 from the Protontricks method  

Now trainers like **WeMod** should work properly on Steam Deck.


---

## **Quick Start (macOS with Whisky)**

1. **Install Whisky**  
   - Get the latest version of **Whisky** for macOS.

2. **Create a Bottle & Add ChooChoo**  
   - In Whisky, create a new **bottle**.  
   - Use **"Run Executable"** and pick `choochoo.exe` to place it in the bottle.

3. **Configure & Run**  
   - In the bottle’s settings, enable **DXVK** (and other needed compatibility tweaks).  
   - Press **Run** to start ChooChoo.  
   - Inside ChooChoo, set **Game Path**, **Trainer Path**, and extras.  
   - **Launch** to start your game + trainer simultaneously.
  
If trainers like **WeMod** are not working properly in **WINE/Whisky**, removing **Wine-Mono** and installing the official **.NET Framework** can help.  

## WeMod/Trainer Not Launching (Fix) (Proton/WINE) 

 **Open Whisky** 🍷  
   - Launch **Whisky** on macOS.  

 **Go to Bottle Configuration** ⚙️  
   - Open your game/trainer's **bottle** in Whisky and hit Bottle Configuration.  

 **Open Control Panel** 🖥️  
   - Inside the bottle, open **Control Panel**.  

 **Uninstall Wine-Mono** 🚫  
   - In **Control Panel**, go to **Applications** or **Add/Remove Programs**.  
   - Find **Wine Mono Windows Support**.  
   - Click **Uninstall** and follow the prompts.  

### **🔄 Install .NET Framework (Manually)**  

Since some trainers require .NET, download and install it manually:  

1. **Download the .NET Framework** from [Microsoft’s official site](https://dotnet.microsoft.com/en-us/download/dotnet-framework).  
2. Inside Whisky, **run the .NET installer** inside the bottle.  
3. Follow the installation steps as you would on Windows.  

(WeMod will want .NET 4.7.1)

After this, **WeMod and other trainers** should now work correctly in **WINE/Proton on macOS**! 🚀🎮  

---

## **Customization & Artwork**

- **Renaming in Steam:**  
  - Right-click ChooChoo in your Library → **Properties** → **Rename** (e.g., _"ChooChoo Trainer"_).

- **Decky Loader & SteamGridDB (Optional):**  
  - Install **Decky Loader** on Steam Deck.  
  - Add **SteamGridDB** plugin → Use it to replace ChooChoo’s artwork with custom images or icons.

---

## **Build & Compilation**

ChooChoo is primarily built for Windows (or Wine/Proton).  

**Dependencies**  
- Windows API (e.g., `windows.h`)  
- Common Controls (`comctl32.lib`)  
- Shell API (`shlwapi.lib`)
