# Build the application
#FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS build
ARG TARGETARCH
WORKDIR /app

# restore dependencies
COPY ./src/faas-metrics.csproj ./
RUN dotnet restore -a $TARGETARCH

COPY ./src/. ./

# generate random password for self-signed certificate
RUN cat /dev/urandom | LC_ALL=C tr -dc 'a-zA-Z0-9' | fold -w 12 | head -n 1 > certpassword
RUN truncate -s -1 certpassword

# generate self-signed certificate
#RUN dotnet dev-certs https -ep dist/cert.pfx -p $(cat certpassword) -v

# configure certificate
RUN sed -i'' s/'CertFilename = string.Empty/CertFilename = "cert.pfx"'/ HttpsOptions.cs
RUN sed -i'' s/'CertPassword = string.Empty/CertPassword = "'$(cat certpassword | base64)'"'/ HttpsOptions.cs

# build application
RUN dotnet publish -c release -a $TARGETARCH -o dist faas-metrics.csproj

# The runner for the application
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as final

RUN addgroup faas-metrics && useradd -G faas-metrics metrics-user

WORKDIR /app
COPY --from=build /app/dist/ ./
RUN chown metrics-user:faas-metrics -R .

USER metrics-user

ENTRYPOINT [ "dotnet", "faas-metrics.dll" ]
