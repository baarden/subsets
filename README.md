# subsets
Subsets word game

## Starting the app

At present the web app expects to be started in a particular configuration:

- The `SubsetsAPI` app is running, usually started with the `dotnet run` command (on localhost:5102).
- The `subsets` Next.js server is running, usually started with the `yarn web` command (on localhost:3000).
- A Redis server is running (on localhost:6379).
- An Nginx server is running (on localhost:8080), configured to send `/api` requests to localhost:5102,
  and all other requests to localhost:3000.
- An Ngrok tunnel is running and directing traffic to localhost:8080.

To run the app, send a request to the ngrok forwarding address.

To run the app in production, issue these two commands from `/subsets`:

- yarn web:prod
- yarn web:prod:serve

# Setting up the repo

The `CsvToPostgres` app will run the migrations to set up the backend database (use `dotnet run`).

The `SelectSubsets` app will generate puzzles and take feedback to narrow down the search space (use `dotnet run`).

You will need to manually set the `plusone.playdate` values in the Postgres database before the game server can be started.
