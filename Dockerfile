# syntax=docker/dockerfile:1.3

# Create a stage for building the application.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

COPY src /source

WORKDIR /source

# This is the architecture you’re building for, which is passed in by the builder.
# Placing it here allows the previous steps to be cached across architectures.
ARG TARGETARCH

# generate random password for self-signed certificate
RUN cat /dev/urandom | LC_ALL=C tr -dc 'a-zA-Z0-9' | fold -w 12 | head -n 1 > certpassword
RUN sed -i '$ d' certpassword

# generate self-signed certificate
#RUN dotnet dev-certs https -ep dist/cert.pfx -p $(cat certpassword) -v

# configure certificate
RUN sed -i'' s/'CertFilename = string.Empty/CertFilename = "cert.pfx"'/ HttpsOptions.cs
RUN sed -i'' s/'CertPassword = string.Empty/CertPassword = "'$(cat certpassword | base64)'"'/ HttpsOptions.cs

# Build the application.
# Leverage a cache mount to /root/.nuget/packages so that subsequent builds don't have to re-download packages.
# If TARGETARCH is "amd64", replace it with "x64" - "x64" is .NET's canonical name for this and "amd64" doesn't
#   work in .NET 6.0.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

# Create a new stage for running the application that contains the minimal
# runtime dependencies for the application. This often uses a different base
# image from the build stage where the necessary files are copied from the build
# stage.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app .

# Switch to a non-privileged user (defined in the base image) that the app will run under.
# See https://docs.docker.com/go/dockerfile-user-best-practices/
# and https://github.com/dotnet/dotnet-docker/discussions/4764
USER $APP_UID

ENTRYPOINT ["dotnet", "faas-metrics.dll"]
