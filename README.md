# az-acme-cli
CLI for obtaining, and renewing TLS certificates from Acme compliant authorities to Azure Key Vault



dotnet run . -- register --key-vault-uri https://kvazacmedev.vault.azure.net/ `
         --account-secret prod-lets-encrypt `
         --email lets-encrypt@azacme.dev `
         --agree-terms
         --verbose
         

dotnet run . -- order --key-vault-uri https://kvazacmedev.vault.azure.net/ `
         --account-secret prod-lets-encrypt `
         --certificate sample-azacme-dev `
         --subject sample.azacme.dev `
         --verbose `
         --dns-zone /subscriptions/ab3e2754-6d21-462f-935b-6c8880489ea6/resourceGroups/rg-dns/providers/Microsoft.Network/dnszones/azacme.dev `
         --aad-tenant 6e319734-19af-4abf-84d6-df253e46a6f8
        

 dotnet run . -- order --key-vault-uri https://kvazacmedev.vault.azure.net/ --certificate cert-le-stg-101 --server https://acme-staging-v02.api.letsencrypt.org/directory --subject stgle01.azacme.dev --sans stgle002.azacme.dev stgle003.azacme.dev --account-secret reg-le-stg-azacme-dev --azure-dns-zone /subscriptions/ab3e2754-6d21-462f-935b-6c8880489ea6/resourceGroups/rg-dns/providers/Microsoft.Network/dnszones/azacme.dev --verbose --force-order


        --zone azacme.io


        https://acme-v02.api.letsencrypt.org/directory
        https://acme-staging-v02.api.letsencrypt.org/directory