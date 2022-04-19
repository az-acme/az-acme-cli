# AZ ACMI 

ACMI compliant certificate management CLI tool that has been designed to be highly opinionated and focused on managing the lifecycle of TLS certificates within the Microsoft Azure ecosystem. 

> Documentation is still a work in progress and will continue to evolve as the first release is offically dropped.

While there are many ACMI clients that exist, ```az-acme``` is different in that it has been designed to align with the following principals.

- Replicate certificate management capabilities for ACMI based certificate issues that exist natively between Azure Key Vault and Digicert / GlobalSign.
- Store certificates in Azure Key Vault to enable existing Azure integrations between services and Key Vault to operate as expected, and aligned with existing Azure best practices.
- Separate TLS certificate management processes from Azure compute resources. This means only ACME DNS challenges are supported.

The following outlines the context of ```az-acme``` within the wider context. To certificate consumers, there is no difference between using a certificate managed by a native integration (Digicert / GlobalSign) and those obtained from an ACMI provider(s).

![AZ ACME Context](./docs/context.drawio.svg)

## Register with Issuer

A one off stage required as part of the ACMI protocol is registering with the issuer. This proceess results in a private key which is stored within Azure Key Vault as a secret named per the ```--account-secret``` parameter. Registering supports both standard and External Account Binding (EAB) registration approaches based on the ACMI issuer that is used.

The following shows a registration with the production Lets Encrypt server.

```bash
./az-acme register \
        --key-vault-uri https://<key vault name>.vault.azure.net/ \
        --server https://acme-v02.api.letsencrypt.org/directory \
        --account-secret reg-stg-lets-encrypt \
        --email <your-email-address> \
        --agree-tos \
        --verbose
```

For services that use EAB, such as ZeroSSL, additional parameters can be provided.

```bash
./az-acme register \
        --key-vault-uri https://<key vault name>.vault.azure.net/ \
        --server https://acme.zerossl.com/v2/DV90 \
        --account-secret reg-stg-lets-encrypt \
        --email <your-email-address> \
        --eab-kid <key id from provider> \
        --eab-hmac-key <key from provider> \
        --eab-algo <key from provider> \
        --agree-tos \
        --verbose
```

Registration can be safely run multiple times, it will only perform the registration if the secret does not exist in the Azure Key Vault, or the ```--force-registration``` switch has been provided.

## Ordering Certificate

Ordering a certificate is as simple one liner! 

```bash
./az-acme order --verbose \
        --key-vault-uri https://<key vault name>.vault.azure.net/ \
        --certificate cert-le-stg-101 \
        --server https://acme-v02.api.letsencrypt.org/directory \
        --subject stgle01.azacme.dev \
        --sans stgle002.azacme.dev stgle003.azacme.dev \
        --account-secret reg-le-stg-azacme-dev \
        --azure-dns-zone <full resource id>
```

The output of the above will result in output similar to the below.

![Order](./docs/force-order.gif)

When executing **without** the ```--force-order``` switch, the order is only submitted to the ACME provider if the certificate does not exist within Azure Key Vault, or will expire within the specified number of days (see ```--renew-within``` parameter, defaults to 30 days) from the time of executution.

This approach makes it simple to run the certificate ordering / renewal process as a nightly operation via existing operational tool chails such as Azure DevOps Pipelines, or GitHub Actions.

![Order](./docs/skip-order.gif)

## Tested ACME Issuers

As more issuers are tested they will be added below.

- Let's Encrypt (Production & Staging)
- ZeroSSL (With EAB)

## Shout Outs

This project wouldn't be possible without other projects, a shout out to some of the core projects ```az-acme``` leverages:

- https://github.com/fszlin/certes
- https://github.com/spectreconsole/spectre.console

