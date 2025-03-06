# build the app.
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build-env
WORKDIR /MisskeyPoster

## copy source code to the working dir.
COPY ./MisskeyPoster/ .
#
## restore as distinct layers.
RUN dotnet restore MisskeyPoster.csproj
#
## build and publish the app as a release.
RUN dotnet publish MisskeyPoster.csproj -c Release -o /app
#
### set up the container.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
#
## set the working dir.
WORKDIR /app

## copy the built app from the build-env.
COPY --from=build-env /app ./

EXPOSE 8080

# command to run the app.
ENTRYPOINT ["dotnet", "MisskeyPoster.dll"]
