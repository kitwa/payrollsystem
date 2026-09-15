## Publish the application on docker (optional)

### Docker
- create ripo on docker called payrollsa
### Client 
ng build

### Pyaroll.Api

- dotnet build

### Backend

- docker build -t dominichdocker/payrollsa .

- docker push dominichdocker/payrollsa:latest


### On server 

docker stop payrollsa && docker rm payrollsa