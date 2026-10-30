FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["LoginCheck/LoginCheck.csproj", "LoginCheck/"]
COPY ["LoginCheck.Application/LoginCheck.Application.csproj", "LoginCheck.Application/"]
COPY ["LoginCheck.Domain/LoginCheck.Domain.csproj", "LoginCheck.Domain/"]
COPY ["LoginCheck.Infrastructure/LoginCheck.Infrastructure.csproj", "LoginCheck.Infrastructure/"]
RUN dotnet restore "LoginCheck/LoginCheck.csproj"

COPY . .
RUN dotnet publish "LoginCheck/LoginCheck.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoginCheck.dll"]
