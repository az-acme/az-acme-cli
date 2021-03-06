# AZ ACMI 

> Refer to documentation at https://azacme.dev for detailed information.

While there are many ACMI clients that exist, ```az-acme``` is different in that it has been designed from the outset with a focus on Microsoft Azure and aligned to the following goals.

- Replicate certificate management capabilities for ACMI based certificate issuers that exist natively between Azure Key Vault and Digicert / GlobalSign.
- Store certificates in Azure Key Vault to enable existing Azure native integrations between services and Azure Key Vault to operate as expected.
- Separate TLS certificate management processes from Azure compute resources. This means *only* ACME DNS challenges are supported.

The following shows how ```az-acme``` fits within the wider certificate management context. To certificate consumers, there is no difference between using a certificate managed by an Azure Key Vault native issuer (Digicert / GlobalSign) and those obtained from an ACMI based issuer via ```az-acme```(s).

![AZ ACME Context](./docs/context.drawio.svg)

## Register with Issuer

A one off stage required as part of the ACMI protocol is registering with the issuer. As part of this process, a private key is generated to identify the client with the ACME server. This private key is stored within Azure Key Vault as a secret named as per the ```--account-secret``` parameter. Registering supports both standard and External Account Binding (EAB) registration approaches based on the ACMI issuer that is used.

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
        --agree-tos \
        --verbose
```

Registration can be safely run multiple times, it will only perform the generation of the private key and registration with ACME server if the secret does not exist in the Azure Key Vault, or the ```--force-registration``` flag has been set. 

**Note:** As secrets are managed in Azure Key Vault, if ```--force-registration``` is used a new version of the secret is created. Old versions are **not** removed by this process.

## Ordering Certificate

Ordering a certificate a simple one liner CLI command, which manages the full ordering and renewal process as outlined below.

```bash
./az-acme order \
        --key-vault-uri https://<key vault name>.vault.azure.net/ \
        --certificate cert-le-stg-101 \
        --server https://acme-v02.api.letsencrypt.org/directory \
        --subject stgle01.azacme.dev \
        --sans stgle002.azacme.dev stgle003.azacme.dev \
        --account-secret reg-le-stg-azacme-dev \
        --azure-dns-zone <full resource id> \
        --verbose
```

The output of the above will result in output similar to the below.

![Order](./docs/force-order.gif)

When executing **without** the ```--force-order``` flag, the order is only submitted to the ACME provider if the certificate does not exist within Azure Key Vault, or will expire within the specified number of days (see ```--renew-within-days``` parameter, defaults to 30 days) from the time of executution.

This approach makes it simple to run the certificate ordering / renewal process as a nightly operation via existing operational tool chails such as Azure DevOps Pipelines, or GitHub Actions.

![Order](./docs/skip-order.gif)

## Using Github Action


```yaml
# File: .github/workflows/order-certificates.yml

on: [push] # change to schedule

name: order-certificates

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: az-acme/setup-cli-action@v1
      with:
        version: 0.2

    - uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Register with Staging
      run: |
        az-acme register \
          --server https://acme-staging-v02.api.letsencrypt.org/directory \
          --key-vault-uri https://kvazacmedev.vault.azure.net/ \
          --account-secret letsencrypt-stg-registration \
          --email demo@azacme.dev \
          --agree-tos

    - name: Order or Renew
      run: |
        az-acme order \
          --server https://acme-staging-v02.api.letsencrypt.org/directory \
          --key-vault-uri https://kvazacmedev.vault.azure.net/ \
          --account-secret letsencrypt-stg-registration \
          --certificate wild-demo-certificate \
          --subject *.demo.azacme.dev \
          --dns-provider Azure \
          --azure-dns-zone /subscriptions/xxxx/resourceGroups/xxxx/providers/Microsoft.Network/dnszones/demo.azacme.dev \
          --renew-within-days 30

            
```



## Tested ACME Issuers

As more issuers are tested they will be added below.

- Let's Encrypt (Production & Staging)
- ZeroSSL (With EAB)


## Shout Outs

A shout out to the core projects ```az-acme``` is built upon.

- https://github.com/fszlin/certes
- https://github.com/spectreconsole/spectre.console
- https://github.com/commandlineparser/commandline
