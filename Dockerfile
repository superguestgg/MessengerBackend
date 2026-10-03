# Backend and the built frontend in one image. MongoDB is not included:
# pass its address in Mongo__ConnectionString (see README, "Docker").

# 1. Frontend: built into MessengerWeb/wwwroot and served by the backend on the same domain.
FROM node:22-slim AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build:host

# 2. Backend. Project files first, so the restore layer is reused while only code changes.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY MessengerWeb/MessengerWeb.csproj MessengerWeb/
COPY Messenger.Infrastructure.Mongo/Messenger.Infrastructure.Mongo.csproj Messenger.Infrastructure.Mongo/
COPY Messenger.Users/Messenger.Users.csproj Messenger.Users/
COPY Messenger.Users.Contracts/Messenger.Users.Contracts.csproj Messenger.Users.Contracts/
COPY Messenger.Chats/Messenger.Chats.csproj Messenger.Chats/
RUN dotnet restore MessengerWeb/MessengerWeb.csproj
COPY . .
COPY --from=frontend /src/MessengerWeb/wwwroot MessengerWeb/wwwroot
RUN dotnet publish MessengerWeb/MessengerWeb.csproj -c Release -o /app --no-restore -p:UseAppHost=false

# 3. Runtime: ASP.NET only, no SDK or Node; runs as the image's non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MessengerWeb.dll"]
