
# Set base image and give the build stage a name
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

# Switch container working directory
WORKDIR /app

# Expose ports on container (metadata)
EXPOSE 9021/tcp

# Set base image and give the build stage a name
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# COPY FILES AND RESTORE PROJECT DEPENDENCIES
# Switch container working directory
WORKDIR /src

# COPY ENTIRE PROJECT AND BUILD IT
# Copy all app files to current directory
COPY . ./

# Copy project file ending in .csproj file
# WebService/ is relative directory; /WebService/ is absolut directory: here /src/WebService
# COPY ["WebService/WebService.csproj", "WebService/"] 

# Ensure we install all specified dependencies
RUN dotnet restore "WebService/WebService.csproj"

# Switch to .csproj folder
WORKDIR /src/WebService

# Build app into app folder
RUN dotnet build "WebService.csproj" -c Release -o /app/build

# Initialize new build stage, setting base image
FROM build AS publish

# Execute command in a new layer on top of the current image and commit
RUN dotnet publish "WebService.csproj" -c Release -o /app/publish

FROM base AS final

# Switch to container app directory
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "WebService.dll"]