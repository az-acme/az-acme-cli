FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out --no-self-contained

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0

# Label the container
LABEL repository="https://github.com/az-acme/action"
LABEL homepage="https://azacme.dev"

# Label as GitHub action
LABEL com.github.actions.name="Az-Acme"
# Limit to 160 characters
LABEL com.github.actions.description="The simplest ACME Issuer for Azure Key Vault"

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["./az-acme"]