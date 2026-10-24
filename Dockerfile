FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# ソース変更時も依存関係の復元レイヤーを再利用する。
COPY MisskeyPoster/MisskeyPoster.csproj MisskeyPoster/
RUN dotnet restore MisskeyPoster/MisskeyPoster.csproj
COPY MisskeyPoster/ MisskeyPoster/
RUN dotnet publish MisskeyPoster/MisskeyPoster.csproj \
    -c Release --no-restore -o /app /p:UseAppHost=false

# Chiseledにはシェルがないため、書き込み用ディレクトリをビルド側で用意する。
RUN mkdir -p /media

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
# 設定と実行ファイルをroot所有のまま保護し、画像だけを別領域へ保存する。
COPY --from=build /app /app
COPY --from=build --chown=app:app /media /media
WORKDIR /media
ENV ASPNETCORE_CONTENTROOT=/app \
    ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/MisskeyPoster.dll"]
