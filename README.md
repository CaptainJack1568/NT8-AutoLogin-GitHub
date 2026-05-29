# NT8 AutoLogin V1.0 Manual

## Quick Download

If you want to use the tool directly without building the source code, download:

[`downloads/NT8-AutoLogin-V1.0.zip`](downloads/NT8-AutoLogin-V1.0.zip)

The zip package currently contains:

- `NT8 AutoLogin.exe`
- `NT8 AutoLogin.exe.config`
- `NT8 AutoLogin.pdb`

After extraction, end users can launch `NT8 AutoLogin.exe` directly.

## Contents

1. Project Overview
2. Target Environment
3. Core Features
4. Quick Start
5. GUI Overview
6. Command-Line Usage
7. Technical Approach and Implementation Notes
8. Source Structure
9. Local Settings, Credential Storage, and Security Notes
10. Cleanup Scope and Behavioral Boundaries
11. Build, Run, and Distribution
12. Suggestions for Further Development
13. FAQ
14. Open-Source License

---

## 1. Project Overview

`NT8 AutoLogin` is an open-source Windows utility for **NinjaTrader 8.1.x**.

It is designed for a very specific local automation workflow:

1. Clean local cache and selected local database folders before launch
2. Launch NinjaTrader with a higher process priority
3. Detect the sign-in window and its controls
4. Fill credentials and trigger sign-in automatically

Current version: **V1.0**

This project is intended to serve as:

- a **ready-to-use desktop utility**
- a **readable open-source sample project**
- a lightweight example of **WinForms + UI Automation + local encrypted credentials**

---

## 2. Target Environment

Recommended environment:

- Operating system: Windows
- Target platform: NinjaTrader 8.1.x
- Runtime: .NET Framework 4.8
- App type: WinForms desktop application
- Modes:
  - GUI mode
  - Command-line mode

Default NinjaTrader executable path:

```text
C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe
```

If your installation path is different, the GUI now supports:

- manually editing the path field
- clicking `Browse...` to pick `NinjaTrader.exe`
- clicking `Default Path` to restore the built-in default location

The GUI will **automatically remember the last valid path after a successful `Priority Launch`** and load it the next time the app starts.

Command-line mode still supports passing a custom path as the third argument.

---

## 3. Core Features

### 3.1 Local Folder Cleanup

The tool can delete and recreate selected directories under `Documents\NinjaTrader 8\`.

This is useful for:

- clearing cache
- clearing temporary data
- resetting selected local database folders before launch

### 3.2 High-Priority Launch

After launching NinjaTrader, the program attempts to set its process priority to `High`.

Notes:

- This is a best-effort optimization
- If the environment does not allow the priority change, the auto sign-in flow still continues

### 3.3 Auto Sign-In

The sign-in automation does **not** depend on fixed screen coordinates.

Instead, it uses **Windows UI Automation** to:

- wait for NinjaTrader to become interactive
- inspect the sign-in window
- identify username / password fields and the sign-in button
- fill values and invoke the button

### 3.4 Encrypted Local Credentials

In GUI mode, the first `Priority Launch` prompts for credentials if none are saved.

Credentials are stored in:

```text
%AppData%\NT8 AutoLogin\credentials.dat
```

They are encrypted with Windows **DPAPI (CurrentUser)** before being written to disk.

### 3.5 Executable Path Memory and Quick Selection

To better support non-default installation folders, the GUI now includes path helpers:

- a `Browse...` button to select `NinjaTrader.exe` directly
- a `Default Path` button to restore the built-in default location
- automatic path memory only after `Priority Launch` succeeds

The path setting is stored in:

```text
%AppData%\NT8 AutoLogin\settings.ini
```

This avoids having to re-enter the NinjaTrader path every time the tool starts.

### 3.6 Command-Line Automation

If username and password are passed on the command line, the tool will:

1. perform the full cleanup
2. launch NinjaTrader
3. fill the sign-in information
4. exit without opening the main window

---

## 4. Quick Start

### 4.1 GUI Mode

Step 1: Launch the app

- Double-click `NT8 AutoLogin.exe`

Step 2: Verify the NinjaTrader path

- Check the bottom path field
- If NinjaTrader is installed outside the default location, use `Browse...` to select `NinjaTrader.exe`
- Use `Default Path` if you want to switch back to the standard install folder
- After a successful `Priority Launch`, the application automatically remembers that valid path

Step 3: Choose an action

- `Clear Data`: cleanup only
- `Priority Launch`: launch NinjaTrader and auto sign-in
- `Reset`: remove the saved local credential file

Step 4: Enter credentials on first use

- If no credentials are stored locally, `Priority Launch` opens the credential dialog
- Username is optional
- Password is required

### 4.2 Command-Line Mode

Basic usage:

```text
NT8 AutoLogin.exe "username" "password"
```

With a custom NinjaTrader path:

```text
NT8 AutoLogin.exe "username" "password" "D:\Path\NinjaTrader.exe"
```

To reset saved credentials:

```text
NT8 AutoLogin.exe --reset
```

---

## 5. GUI Overview

The main window has three functional areas.

### 5.1 Header

The top band summarizes the utility purpose, such as:

- clear local data
- priority launch
- auto sign-in

### 5.2 Left Action Panel

There are three buttons:

- `Clear Data`
  - cleanup only
  - does not launch NinjaTrader

- `Priority Launch`
  - does not clean first
  - launches NinjaTrader
  - loads or prompts for credentials
  - performs auto sign-in

- `Reset`
  - deletes the saved local credential file
  - forces re-entry on the next priority launch

### 5.3 Right Log Panel

This panel displays operational messages such as:

- start messages
- cleanup progress
- folder recreation
- running-instance detection
- launch progress
- auto sign-in completion
- error messages

The top of the log area also keeps the Discord community link pinned so it remains visible even while log content grows or is cleared.

### 5.4 Bottom Path Field

The bottom path area contains:

- the `NinjaTrader.exe` path field
- `Browse...`
  - opens a file picker so the user can locate `NinjaTrader.exe` directly
- `Default Path`
  - restores the built-in default path with one click

Behavior rules:

- editing the path does not save it immediately
- the path is saved only after a successful `Priority Launch`
- the next app launch prefers the last successful path over the built-in default

It remains the main runtime setting users should verify before use.

---

## 6. Command-Line Usage

### 6.1 Syntax

```text
NT8 AutoLogin.exe "username" "password" ["FullPathToNinjaTrader.exe"]
```

### 6.2 Behavior

When the app receives at least two arguments, it switches to command-line mode:

1. validate the NinjaTrader path
2. run the full cleanup
3. launch NinjaTrader
4. fill credentials automatically
5. exit without opening the GUI

### 6.3 Typical Use Cases

- local desktop shortcut
- personal batch script
- lightweight desktop automation

### 6.4 Risk Note

Passwords passed on the command line may appear in:

- process argument lists
- diagnostic logs
- system auditing tools

Because of that, command-line mode is better suited for personal/local use than shared environments.

---

## 7. Technical Approach and Implementation Notes

This is one of the most important parts of the project from an open-source perspective.

### 7.1 Overall Technical Stack

The project mainly relies on:

- **WinForms**
  - lightweight Windows desktop UI
- **.NET Framework 4.8**
  - compatibility with classic Windows environments
- **Windows UI Automation**
  - sign-in window inspection and control discovery
- **DPAPI**
  - local credential encryption
- **System.Diagnostics.Process**
  - process launch and priority management

### 7.2 Why This Project Avoids Fixed Click Coordinates

A coordinate-based automation approach is fragile because it breaks when:

- screen resolution changes
- DPI scaling changes
- the window moves
- multiple monitors are involved
- the sign-in layout changes

Using UI Automation instead makes the tool:

- independent of screen coordinates
- independent of window position
- easier to maintain
- more suitable as an open-source reference

### 7.3 Sign-In Detection Flow

The sign-in workflow is roughly:

```text
Launch NinjaTrader
  -> wait until the process becomes interactive
  -> get the main window handle
  -> scan Edit / Button controls in the sign-in window
  -> infer username field, password field, and sign-in button
  -> fill username and password
  -> invoke the sign-in button
```

Important details:

- `WaitForInputIdle` is used to wait for an interactive state
- `AutomationElement.FromHandle` is used to get the window root
- controls are filtered by `ControlType.Edit` and `ControlType.Button`
- only visible and enabled controls are considered
- common button names are matched first:
  - `Log In`
  - `Login`
  - `Sign In`

### 7.4 Credential Encryption Method

Credentials are not stored as plaintext.

The program:

1. converts username and password to UTF-8 bytes
2. encrypts them using `ProtectedData.Protect(..., CurrentUser)`
3. writes username length, encrypted username, password length, and encrypted password to a binary file

This approach is useful because:

- it avoids inventing a custom crypto layer
- it binds decryption to the current Windows user context
- it is a good fit for a local utility

### 7.5 Cleanup Strategy

The cleanup behavior is not “delete files one by one.”

Instead, it:

- deletes the target directory
- recreates it as an empty directory

The project also includes a safety guard:

- every target path must remain inside `Documents\NinjaTrader 8\`
- unexpected paths abort the cleanup

### 7.6 Single-Instance Protection

Because NinjaTrader is typically single-instance, the tool checks for an existing running process before critical actions.

That prevents:

- partial cleanup on locked directories
- accidental interaction with an already active session
- undefined behavior from repeated launches

---

## 8. Source Structure

The project is intentionally compact.

```text
NT8 AutoLogin-main-EN/
├── NT8 AutoLogin.sln
├── README.md
├── README.txt
└── NT8 AutoLogin/
    ├── NT8 AutoLogin.csproj
    ├── Program.cs
    ├── MainForm.cs
    ├── CredentialsForm.cs
    ├── App.config
    ├── NinjaTrader.ico
    └── Properties/
        └── AssemblyInfo.cs
```

### 8.1 `Program.cs`

Responsible for:

- application entry point
- command-line branching
- executable-path settings load/save
- credential save / load / reset
- folder cleanup
- NinjaTrader launch
- UI Automation sign-in logic
- exception handling

### 8.2 `MainForm.cs`

Responsible for:

- main window layout
- button behavior
- log output
- path input field
- browse / restore-default path actions
- automatic path memory after successful launch
- GUI interaction flow

### 8.3 `CredentialsForm.cs`

Responsible for:

- first-time credential entry
- credential re-entry after reset

### 8.4 `AssemblyInfo.cs`

Responsible for metadata such as:

- product name
- title
- version

### 8.5 `App.config`

Currently used for:

- .NET Framework startup configuration
- WinForms DPI awareness

---

## 9. Local Settings, Credential Storage, and Security Notes

### 9.1 Path Settings Location

```text
%AppData%\NT8 AutoLogin\settings.ini
```

This file currently stores the last `NinjaTrader.exe` path that was used successfully in GUI mode.

### 9.2 Path Memory Rules

The GUI follows these rules:

- on startup, it first tries to load the last successful path from `settings.ini`
- if no saved path exists, it falls back to the built-in default path
- the saved path is updated only after `Priority Launch` succeeds
- clicking `Browse...` or `Default Path` changes the current field value, but does not save it by itself

If the previously saved path later becomes invalid, the app still shows it and reports the problem in the log so the user can choose a new one.

### 9.3 Credential Storage Location

```text
%AppData%\NT8 AutoLogin\credentials.dat
```

### 9.4 Credential File Contents

The file stores:

- encrypted username
- encrypted password

It is not a plaintext text file.

### 9.5 Encryption Scope

The implementation uses:

- Windows DPAPI
- `CurrentUser` scope

That means:

- the credentials are intended for the same machine / same Windows user
- they are not stored as generic plaintext
- the approach is suited for a single-user local utility

### 9.6 Practical Security Boundary

DPAPI improves local protection, but it is not a complete secret-management system.

The application still needs to:

- decrypt data in memory
- write the values into NinjaTrader sign-in controls

So this should be viewed as a **reasonable local security measure**, not enterprise-grade secret storage.

---

## 10. Cleanup Scope and Behavioral Boundaries

The current cleanup targets are:

- `cache`
- `db\cache`
- `db\day`
- `db\minute`
- `db\tick`
- `tmp`
- `trace`
- `log`

All of them are resolved under:

```text
Documents\NinjaTrader 8\
```

### 10.1 What This Means in Practice

This is not just a lightweight cache clear.

Because selected `db` subdirectories are also recreated, local data may be removed.

Possible effects:

- local cache invalidation
- historical data requiring re-download
- slower first startup afterward

### 10.2 Design Boundary

This project does **not**:

- modify files inside the NinjaTrader installation directory
- modify the Windows registry
- modify services
- upload credentials to a network endpoint

It only performs:

- local folder cleanup
- local process launch
- local UI automation
- local encrypted credential file read/write

---

## 11. Build, Run, and Distribution

### 11.1 Running the Compiled App

The target machine must support **.NET Framework 4.8**.

If needed, install it from Microsoft's official download page:

<https://dotnet.microsoft.com/download/dotnet-framework/net48>

### 11.2 Build from the Command Line

Run this from the solution directory:

```powershell
dotnet build "NT8 AutoLogin.sln" -c Release
```

Output folder:

```text
NT8 AutoLogin\bin\Release\net48\
```

### 11.3 Build with Visual Studio

Recommended IDE:

- Visual Studio 2022

Recommended workload:

- `Desktop development with .NET`

### 11.4 Typical End-User Distribution Files

- `NT8 AutoLogin.exe`
- `NT8 AutoLogin.exe.config`

Debug symbols:

- `NT8 AutoLogin.pdb`

Most end users do not need the `pdb` file.

---

## 12. Suggestions for Further Development

If you plan to extend this open-source project, these are good starting points.

### 12.1 Improve Control Identification Rules

If NinjaTrader changes its sign-in UI in the future, the first places to adjust will likely be:

- `LoginButtonNames`
- username/password field inference
- control filtering rules

### 12.2 Improve Logging

The current logging is primarily GUI-oriented.

Possible upgrades:

- file logging
- log levels
- a debug mode switch

### 12.3 Improve Configuration

The project now supports remembering the last successful `NinjaTrader.exe` path and provides `Browse... / Default Path` helpers in the GUI.

Potential additions:

- configurable timeouts
- toggle for high-priority launch
- toggle for password-only fill behavior
- configurable cleanup whitelist

### 12.4 Improve Credential Compatibility

If backward compatibility becomes important later, you could add:

- automatic legacy credential migration
- one-time startup migration logic

### 12.5 Improve Release Workflow

Possible upgrades:

- automatic version stamping
- installer packaging
- zip release scripts
- GitHub Release automation

---

## 13. FAQ

### Q1: Why did `Priority Launch` fail to sign in automatically?

Possible reasons:

- NinjaTrader startup was too slow
- the sign-in window structure changed
- the button text changed
- UI Automation could not identify the required controls

Check the log panel first.

### Q2: Why not use mouse clicks at fixed coordinates?

Because coordinate-based automation is fragile and breaks under different:

- resolutions
- DPI scales
- window positions
- monitor layouts

This project uses UI Automation because it is more stable and more maintainable.

### Q3: Why is the username optional?

Some NinjaTrader setups remember the username in the sign-in UI.

In that case, filling only the password is sufficient.

Current behavior:

- username present: fill username + password
- username blank: fill password only

### Q4: Why check whether NinjaTrader is already running before cleanup?

To avoid:

- locked directories during deletion
- accidental interaction with an already active session
- undefined behavior from repeated launches

### Q5: Why do I need to enter credentials again after reset?

Because `Reset` deletes:

```text
%AppData%\NT8 AutoLogin\credentials.dat
```

After that, there is nothing left to load.

### Q6: Why does the app still show my previous NinjaTrader path the next time I open it?

Because the GUI automatically saves the current valid `NinjaTrader.exe` path after a successful `Priority Launch` to:

```text
%AppData%\NT8 AutoLogin\settings.ini
```

This is intended to make non-default install locations more convenient. If you want to switch back, use `Default Path` in the bottom path area.

### Q7: Why keep both `README.md` and `README.txt`?

Because they serve different distribution scenarios:

- `README.md` is better for GitHub and online repository viewing
- `README.txt` is better for local zip distribution, terminals, and offline reading

---

## 14. Open-Source License

This project is released under the **MIT License**.

See the repository root:

```text
LICENSE
```

Within the terms of that license, you can:

- use
- modify
- redistribute
- use commercially

---

**NT8 AutoLogin V1.0 Manual — End**
