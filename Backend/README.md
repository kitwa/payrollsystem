## Publish the application on docker (optional)

### Docker
- create ripo on docker called payrollsa
### Client 
ng build
### API
- dotnet build
- docker build -t dominichdocker/payrollsa .
- docker login
- docker push dominichdocker/payrollsa:latest