FROM mcr.microsoft.com/dotnet/sdk:3.1 as build
WORKDIR /build/
COPY ./nuget.config ./
COPY ./TTBot.sln ./
COPY ./TTBot/TTBot.csproj ./TTBot/
RUN dotnet restore 
COPY ./ ./
RUN dotnet publish -o /publish/

FROM mcr.microsoft.com/dotnet/runtime:3.1 as runtime
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libgdiplus \
        fontconfig \
     && rm -rf /var/lib/apt/lists/*
COPY ./TTBot/Assets/Fonts/* /usr/share/fonts/truetype
RUN fc-cache -f -v
COPY --from=build /publish/ /app/
WORKDIR /app/
ENTRYPOINT ["dotnet", "TTBot.dll"]
