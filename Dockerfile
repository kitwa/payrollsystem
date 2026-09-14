# Single-deployment image: builds the Angular SPA, copies it into the API's wwwroot, then publishes
# the API so `dotnet Payroll.Api.dll` serves both the API and the SPA from one container.

# ---- Frontend build ----
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY Frontend/package*.json ./
RUN npm ci
COPY Frontend/. ./
RUN npx ng build --configuration production

# ---- Backend build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app
EXPOSE 8080

# copy csproj files and restore as distinct layers
COPY Backend/src/Payroll.Api/Payroll.Api.csproj src/Payroll.Api/
COPY Backend/src/Payroll.Application/Payroll.Application.csproj src/Payroll.Application/
COPY Backend/src/Payroll.Domain/Payroll.Domain.csproj src/Payroll.Domain/
COPY Backend/src/Payroll.Infrastructure/Payroll.Infrastructure.csproj src/Payroll.Infrastructure/
COPY Backend/src/Payroll.Shared/Payroll.Shared.csproj src/Payroll.Shared/
RUN dotnet restore src/Payroll.Api/Payroll.Api.csproj

# copy everything else and build
COPY Backend/. ./
COPY --from=frontend-build /frontend/dist/client/. src/Payroll.Api/wwwroot/
RUN dotnet publish src/Payroll.Api/Payroll.Api.csproj -c Release -o out

# ---- Runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /app/out .
ENTRYPOINT [ "dotnet", "Payroll.Api.dll" ]
