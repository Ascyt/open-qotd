# ArtcordAdminBot

This project is made using C# and [DSharpPlus](https://dsharpplus.github.io/DSharpPlus/) and [SQLite](https://www.sqlite.org/index.html). 
It's a custom moderation Discord bot made for the [ArtCord Discord Server](https://discord.gg/ArtCord). 

## Project Structure

The project is structured as follows:

```
/ArtcordAdminBot
│
├── /Database
│   └── DatabaseHelper.cs      # Contains database operations and utility functions
│
├── /Documentation
│   ├── usage.md               # General usage guide for the bot
│   └── /Commands
│       ├── config.md          # Documentation for the config command group
│       ├── echo.md            # Documentation for the echo command
│       └── purge.md           # Documentation for the purge command
│
├── /Features
│   ├── EchoCommand.cs         # Command handler for echo functionality
│   ├── PurgeCommand.cs        # Command handler for purge functionality
│   └── /Helpers
│       └── MessageHelpers.cs  # Utility functions related to message handling
│
├── Program.cs                 # Entry point of the application, sets up and runs the bot
└── ArtcordAdminBot.csproj     # Project file, includes dependencies and build settings
```

## Setup

1. **Clone the Repository:**
   ```sh
   git clone https://github.com/SniffBakaSniff/ArtcordAdminBot
   cd ArtcordAdminBot
   ```

2. **Install Dependencies:**
   Make sure you have the [.NET SDK](https://learn.microsoft.com/en-us/dotnet/core/install/windows) installed. If not, install it via your package manager.

   For Arch Linux:
   ```sh
   sudo pacman -S dotnet-sdk
   ```

3. **Configure your bot's token:**
   Create an environment variable named "DISCORD_TOKEN" in your system, with the value being your bot's token.	 

   In Windows this can be done by searching for "Edit the system environment variables", and in the window that pops up pressing "Environment variables...", "New..." and putting in the information.

4. **Build the Project:**
   ```sh
   dotnet build
   ```

5. **Run the Bot:**
   ```sh
   dotnet run
   ```

Alternatively, you can open the project using [Visual Studio](https://visualstudio.microsoft.com/), which will handle everything except for step 1 and 3 for you. 

## Features

This folder contains the commands of the bot, and a sub-folder with helper classes.

### Commands
Precise description on commands can be found in the [Documentation/Commands](./Documentation) folder. The general usage of commands is described in [usage.md](./Documentation/usage.md).

- [config](./Documentation/Commands/config.md): Config options. Web interface for this is coming soon. 
- [echo](./Documentation/Commands/echo.md): Writes a message or an embed to the current channel or a different channel.
- [purge](./Documentation/Commands/purge.md): Purges a specified number of messages from the current channel.


## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
