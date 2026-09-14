# Client

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.27.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests#### add new certificate cliniquecarehub.co.za
sudo certbot certonly --cert-name cliniquecarehub.co.za -d cliniquecarehub.co.za -d www.cliniquecarehub.co.za

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

## Docker
docker run --detach --name payrollsa --env "MARIADB_ROOT_PASSWORD=V9!kR7@qL2#xN8$pT4^mW6&zH1*eY3" -p 3306:3306 mariadb:latest

docker run --detach --name payrollsa --env 'MARIADB_ROOT_PASSWORD=HippoFixer1502@' --env 'MARIADB_DATABASE=root' --env 'MARIADB_USER=payrollsa' --env 'MARIADB_PASSWORD=V9!kR7@qL2#xN8$pT4^mW6&zH1*eY3' -p 3306:3306 mariadb:latest



# On the Server

#### add new certificate for subdomain payroll.kibokohouse.com

sudo certbot certonly --cert-name payrollsa -d payrollsa.kibokohouse.com

## run docker on the server 

docker stop payrollsa && docker rm payrollsa
docker pull dominichdocker/payrollsa:latest


###  run sql
CREATE DATABASE IF NOT EXISTS payrollsa
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'payrollsa'@'%' IDENTIFIED BY 'V9!kR7@qL2#xN8$pT4^mW6&zH1*eY3';

GRANT ALL PRIVILEGES ON payrollsa.* TO 'payrollsa'@'%';

FLUSH PRIVILEGES;

### run the docker image

docker run -d --name payrollsa --network root_reseau -e ASPNETCORE_URLS=http://+:80 -e DB_HOST=mariadb -e DB_NAME=payrollsa -e DB_USER=payrollsa -e DB_PASSWORD='V9!kR7@qL2#xN8$pT4^mW6&zH1*eY3' dominichdocker/payrollsa:latest

docker logs payrollsa

docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' payrollsa 

# Add to site available

cd /etc/nginx/sites-available
nano docker-proxy

sudo systemctl stop nginx
sudo systemctl restart nginx
systemctl status nginx.service
