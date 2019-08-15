# AzureWebAppSSLManager
Azure WebApp SSL Manager is an Azure Function that manages SSL certificates for Azure Web App hosted applications.

Main Tasks perfomed:
- Order SSL certificates from Let's Encrypt free CA
- Validates the order using Azure DNS TXT record
- Download the certificates and save them on Azure Blob Storage
- Install the certificates on Azure App Service Web App
- Associate the certificates to the hostname bindings

## Dependencies
This project depends on few other project:
- [Certes](https://github.com/fszlin/certes) for the interface with Let's Encrypt services via ACME
- [SendGrid](https://sendgrid.com/) for the email processing

## Prerequisites
In order to succesfully use this application, you need a number of Prerequisites.
- At least one Azure Web App with at least one custom domain assigned
- The DNS for the custom domain must be managed via an Azure DNS Zone
- A Service Principal and it's config values
- A SendGrid account and a valid SendGrid API Key

#### Service Principal
You need to have a Service Principal to be able to performa management operations on Azure, like uploading the certificate to the App Service or managing the DNS Zone.

If you don't have a Service Principal, you can create one with the following command via Azure CLI:
```
az ad sp create-for-rbac
```

You need to have enough privileges in your Azure AAD to be able to succesfully create a Service Principal.

Visit the [Azure Docs page](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest) for more information about Azure Service Principals creation.

Once created, **take note of the output values**, especially the password because it won't be possible to retrieve it later.
The output will look like this;
```json
{
  "appId": "xx15d42-f9xx-45xx-xx9a-3dxxxxxxxxf2",
  "displayName": "azure-cli-xxx-08-xx-07-xx-37",
  "name": "http://azure-cli-xxxx-08-xx-07-xx-37",
  "password": "7xxxxxx-xxxe-4xxx-xxxf-exxxxxxxxxx4",
  "tenant": "9xxxxxx0-cxxx-xxx4-bxxx-cxxxxxxxxxx3"
}
```

#### SendGrid key
AzureWebAppSSLManager sends emails using the ** extension, which needs a valid SendGrid API Key.

To obtain an API Key, you can follow the [official SendGrid documentation](https://sendgrid.com/docs/ui/account-and-settings/api-keys/)

## Configuration
To be able to run, AzureWebAppSSLManager needs the following configuration settings.

They can be created in the Azure Web App configuration section when deployed, or in the *local.settings.json* file when debugging.

```json
    "CertificateOwnerEmail": "YOUR_NAME@EMAIL.XXX",
    "SubscriptionID": "SUBSCRIPTION_ID",
    "ServicePrincipalClientID": "SERVICE_PRINCIPAL_APP_ID",
    "ServicePrincipalClientSecret": "SERVICE_PRINCIPAL_PASSWORD",
    "ServicePrincipalTenantID": "SERVICE_PRINCIPAL_TENANT",
    "AzureStorageAccountConnectionString": "AZURE_STORAGE_FULL_CONNECTION_STRING",
    "SendGridKey": "SENDGRID_KEY",
    "EmailSender": "SENDER@YOURSERVICE.EXT
```
The config settings for the Service Princil are the one from the output of the Service Principal creation above.

## Application Properties Configuration File
Currently AzureWebAppSSLManager retrieves the list of certificates to generate and install from a json file stored in a blob storage accout.

An example of the file structure can be found in the *[appproperties.json](../master/SampleJsonConfig/appproperties.json)* example file.

The file needs to be saved in a blob container with name as in the constant "" of the *[Constants.cs](../master/src/WebAppSSLManager/Models/Contants.cs)* class;

## Limitations
Currently an instance of AzureWebAppSSLManager can manage Web Apps in a single subscriptiont.
If you need/want to manage App Service Web Apps in multiple subscriptions, you would need to deploy one instance of the function per subscription.

## Support ###
If you encounter some issues trying this library, please let me know through the [Issues page](https://github.com/n3wt0n/AzureWebAppSSLManager/issues) and I'll fix the problem as soon as possible!
