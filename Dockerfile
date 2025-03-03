# build the app.
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build-env
WORKDIR /MisskeyPoster

RUN apt update
RUN apt install -y clang zlib1g-dev

# copy source code to the working dir.
COPY . .

# restore as distinct layers.
RUN dotnet restore MisskeyPoster.csproj

# build and publish the app as a release.
RUN dotnet publish MisskeyPoster.csproj -c Release -o out

## set up the container.
#FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
#
## set the working dir.
#WORKDIR /MisskeyPoster
#
## copy the built app from the build-env.
#COPY --from=build-env /MisskeyPoster/out .

# command to run the app.
ENTRYPOINT ["dotnet","MisskeyPoster.dll"]