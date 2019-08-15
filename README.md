# AzureWebAppSSLManager
Azure WebApp SSL Manager is an Azure Function that manages SSL certificates for Azure Web App hosted applications.

Main Tasks perfomed:
- Order SSL certificates from Let's Encrypt free CA
- Validates the order using Azure DNS TXT record
- Download the certificates and save them on Azure Blob Storage
- Install the certificates on Azure App Service Web App
- Associate the certificates to the hostname bindings

## Prerequisites
In order to succesfully use this application, y

## Dependencies
This project depends on few other project:
- [Certes](https://github.com/fszlin/certes) for the interface with Let's Encrypt services via ACME
- [Sendgrid](https://sendgrid.com/) for the email processing

## Configuration
asd

## Application Properties Configuration File
asd

## Limitations
Currently an instance of AzureWebAppSSLManager can manage Web Apps in a single subscriptiont.
If you need/want to manage App Service Web Apps in multiple subscriptions, you would need to deploy one instance of the function per subscription.
