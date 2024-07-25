#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ["common.props", "common.props"]
WORKDIR /src
COPY ["Autabee.OpcScoutWeb/Autabee.OpcScoutWeb.csproj", "Autabee.OpcScoutWeb/"]
COPY ["Autabee.OpcScout/Autabee.OpcScout.BlazorView.csproj", "Autabee.OpcScout/"]
COPY ["Autabee.OpcScout.MBRazorControl/Autabee.OpcScout.MBRazorControl.csproj", "Autabee.OpcScout.MBRazorControl/"]
COPY ["Autabee.OpcScout.RazorControls/Autabee.OpcScout.RazorControl.csproj", "Autabee.OpcScout.RazorControls/"]
COPY ["ManagedOpcClient/Autabee.Communication.ManagedOpcClient.csproj", "ManagedOpcClient/"]
RUN dotnet tool install Excubo.WebCompiler --global
RUN dotnet restore "Autabee.OpcScoutWeb/Autabee.OpcScoutWeb.csproj"
COPY . .
WORKDIR "/src/Autabee.OpcScoutWeb"
RUN dotnet build "Autabee.OpcScoutWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Autabee.OpcScoutWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "opcscout.dll"]
