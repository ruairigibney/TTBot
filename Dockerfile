FROM mcr.microsoft.com/dotnet/sdk:3.1 as build
WORKDIR /build/
COPY ./nuget.config ./
COPY ./TTBot.sln ./
COPY ./TTBot/TTBot.csproj ./TTBot/
RUN dotnet restore 
COPY ./* ./
RUN dotnet build -c Release -o /publish/

FROM mcr.microsoft.com/dotnet/runtime:3.1 as runtime
COPY --from=build /publish/ /app/
WORKDIR /app/
ENTRYPOINT ["dotnet", "TTBot.dll"]