# ===== Estágio de build =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia primeiro só os arquivos de projeto para aproveitar o cache do restore
COPY CooperativaSolution/Cooperativa.Domain/Cooperativa.Domain.csproj CooperativaSolution/Cooperativa.Domain/
COPY CooperativaSolution/Cooperativa.Application/Cooperativa.Application.csproj CooperativaSolution/Cooperativa.Application/
COPY CooperativaSolution/Cooperativa.Infrastructure/Cooperativa.Infrastructure.csproj CooperativaSolution/Cooperativa.Infrastructure/
COPY CooperativaSolution/Cooperativa.Tests/Cooperativa.Tests.csproj CooperativaSolution/Cooperativa.Tests/
COPY CooperativaSolution/Cooperativa.Web/Cooperativa.Web.csproj CooperativaSolution/Cooperativa.Web/
RUN dotnet restore CooperativaSolution/Cooperativa.Web/Cooperativa.Web.csproj

# Copia o restante do código (inclui Cooperativa.Data, referenciado por link pelo Infrastructure)
COPY CooperativaSolution/ CooperativaSolution/
RUN dotnet publish CooperativaSolution/Cooperativa.Web/Cooperativa.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ===== Estágio de runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# As imagens .NET 8+ já incluem o usuário não-root "app" (UID 1654).
# Cria o home para o Data Protection do ASP.NET conseguir gravar as chaves de cookie.
RUN mkdir -p /home/app/.aspnet \
    && chown -R 1654:1654 /home/app
ENV HOME=/home/app

COPY --from=build --chown=1654:1654 /app/publish .

USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Cooperativa.Web.dll"]

