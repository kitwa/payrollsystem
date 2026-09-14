FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app
EXPOSE 8080

# copy csproj files and restore as distinct layers
COPY src/Payroll.Api/Payroll.Api.csproj src/Payroll.Api/
COPY src/Payroll.Application/Payroll.Application.csproj src/Payroll.Application/
COPY src/Payroll.Domain/Payroll.Domain.csproj src/Payroll.Domain/
COPY src/Payroll.Infrastructure/Payroll.Infrastructure.csproj src/Payroll.Infrastructure/
COPY src/Payroll.Shared/Payroll.Shared.csproj src/Payroll.Shared/
RUN dotnet restore src/Payroll.Api/Payroll.Api.csproj

# copy everything else and build
COPY . ./
RUN dotnet publish src/Payroll.Api/Payroll.Api.csproj -c Release -o out

# build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "Payroll.Api.dll" ]
