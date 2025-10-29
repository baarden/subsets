# subsets
Plus One word game (was: subsets)

## Setup

The app expects the API and the Next.js web server (if present) to be running in the same domain.
To achieve this for web, the `config/nginx.conf` file provides a configuration that will run an Nginx server listening on `localhost:8080`, configured to send `/api` requests to localhost:5102, and all other requests to localhost:3000.
This should replace the existing `nginx.conf` file (execute `nginx -t` to find its location).

The `SubsetsAPI` app is configured to listen on `localhost:5102` when started with the `dotnet run` command. It expects that a Redis server is running (on `localhost:6379`).

The `subsets` Next.js server is configured to listen on `localhost:3000` when started with the `yarn web` command.

The app should be opened at localhost:8080 in this dev configuration.

## Running in production

To run the web app in production, issue these two commands from `/subsets`:

- `yarn web:prod`
- `yarn web:prod:serve`

The `config/ngrok.sh` file provides a command to route public traffic from an Ngrok domain to `localhost:8080` (the Nginx server).

The app should be opened at the domain specified in the ngrok config in this prod configuration.

## iOS development

Note that any dependencies with native code must be added to the `apps/expo` folder.

To build the mobile app for the first time:

```
cd apps/expo
npx expo prebuild --no-install
cd ios
pod install
```

To build the app when dependencies change:
```
cd apps/expo && yarn ios
```

To build the app in XCode: 
```
open apps/expo/ios/plusone.xcworkspace
```
