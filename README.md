# OpenQOTD

This project is made using C#, [DSharpPlus](https://dsharpplus.github.io/DSharpPlus/), [PostgreSQL](https://www.postgresql.org/) and [Docker](https://www.docker.com/). 

- [Documentation](https://open-qotd.ascyt.com/documentation)
- [Community Server](https://open-qotd.ascyt.com/community)
- [Add OpenQOTD to your server!](https://open-qotd.ascyt.com/add)

## Deployment

To deploy, you will need to have [git](https://git-scm.com/install/windows), [.NET](https://dotnet.microsoft.com/en-us/download), and [Docker Compose](https://www.docker.com/products/docker-desktop/) installed. This should work on any OS, but feel free to let me know if something doesn't work as expected.

1. Clone the repository and cd into it:
    ```
    git clone https://github.com/Ascyt/open-qotd
    cd open-qotd
    ```
2. Create a `.env`-file (in the repo root) and replace the parts in the square brackets with your information:
    ```env
    POSTGRES_PASSWORD=[choose a secure password for postgres]
    POSTGRES_PORT=[use 5432 if you're not running multiple instances at once]
    OPENQOTD_TOKEN=[your Discord bot's token]
    PGDATA_PATH=[wherever you want your data to be stored]
    ```
3. Start the database:
    ```
    docker compose up db -d
    ```
4. In `Bot/appsettings.json` (or `Bot/appsettings.defaults.json` if it doesn't exist), set the `EnableDbMigrationMode` flag to `true`:
    ```jsonc
    // ...
    "EnableDbMigrationMode": true,
    // ...
    ```
5. Install the .NET migration tool:
    ```
    dotnet tool install --global dotnet-ef
    ```
6. Restore .NET packages:
    ```
    cd Bot
    dotnet restore
    cd ..
    ```
7. Run the following to make migrations (and initialize the database tables):
    ```
    cd Bot
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    cd ..
    ```
8. In `Bot/appsettings.json`, set the `EnableDbMigrationMode` back to `false`:
    ```jsonc
        // ...
        "EnableDbMigrationMode": false,
        // ...
    ```
9. Stop the database container:
    ```
    docker compose down
    ```
10. From now on you can start the entire project with the following:
    ```
    docker compose up --build -d
    ```

For testing, it should work to just stop the `bot`-Container (in Docker Desktop or using CLI) and run the project in VS or something similar.

## Contributions

This project is open to contributions! Please note that the code might be a little rough around the edges and is missing documentation, so feel free to ask me personally for help. 
Feel free to check out the Issues tab for things that need to be implemented!

## License

This project is licensed under the GNU Affero General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
