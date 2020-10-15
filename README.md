# Azure WebApp SSL Manager

![CI Badge](https://github.com/n3wt0n/AzureWebAppSSLManager/workflows/CI/badge.svg)
[![License](https://img.shields.io/github/license/n3wt0n/AzureWebAppSSLManager.svg)](https://github.com/n3wt0n/AzureWebAppSSLManager/blob/master/LICENSE)

Azure WebApp SSL Manager is an Azure Function that acquires and manages **free** SSL certificates for Azure Web App and Azure Function App hosted applications.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fn3wt0n%2FAzureWebAppSSLManager%2Fmaster%2FARM%2520Template%2Ftemplate.json) [![Visualize](http://armviz.io/visualizebutton.png)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fn3wt0n%2FAzureWebAppSSLManager%2Fmaster%2FARM%2520Template%2Ftemplate.json)

> **Pre-Deployment**  
> Before deploying this solution to Azure, you need to create a Service Principal (see below).  
> Unfortunately due to Azure ARM templates limitation it is not possible for me to include this step in the deployment.  
>
> Also, be sure to create your SendGrid API Key (see below).
  
> **Post-Deployment**  
> After deploying the solution to Azure, make sure to set up the required configuration.  
> See the *Application Properties Configuration File* section below for more information

## Overview

Azure WebApp SSL Manager is an Azure Function that acquires and manages **free** SSL certificates for applications hosted on Azure Web Apps and Azure Function Apps

Main Tasks perfomed:

- Order/Renewal of SSL certificates from [Let's Encrypt](https://letsencrypt.org/) free trusted CA
- Validation of the order using Azure DNS TXT record
- Download of the certificates and save them on Azure Blob Storage
- Installation of the certificates on Azure App Service Web App or Function App
- Association of the certificates to the Web App or Function App hostname bindings

## Supported Azure Resources

Currently this solutions supports:

- Azure Web Apps
- Azure Web Apps Slots
- Azure Function Apps
- Azure Function Apps Slots

## Dependencies

This project depends on few other project:

- [Certes](https://github.com/fszlin/certes) for the interface with Let's Encrypt services via ACME
- [SendGrid](https://sendgrid.com/) for the email processing

## Prerequisites

In order to succesfully use this application, you need a number of Prerequisites.

- Either:
  - At least one Azure Web App, with at least one custom domain assigned, running in Basic, Standard or Premium tier, OR
  - At least one Azure Function App, with at least one custom domain assigned, running in Consumption tier or in an App Service Basic, Standard or Premium tier
- A Blob storage account to save the App Properties configuration (see below) and to save the certificates
- The DNS for the custom domain must be managed via an Azure DNS Zone (in the same subscription of the resources)
- A Service Principal and it's config values
- A SendGrid account and a valid SendGrid API Key

> Remember to bind all the hostnames you want to add certificates to to the App Service in advance.

#### Service Principal

You need to have a Service Principal to be able to performa management operations on Azure, like uploading the certificate to the App Service or managing the DNS Zone.

If you don't have a Service Principal, you can create one with the following command via Azure CLI. You can execute the command from any PC with the Azure CLI installed or from the [Azure Shell](http://shell.azure.com)

```shell
az ad sp create-for-rbac
```

You need to have enough privileges in your Azure AAD to be able to succesfully create a Service Principal.

Visit the [Azure Docs page](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest) for more information about Azure Service Principals creation.

Once created, **take note of the output values**, especially the password because it won't be possible to retrieve it later.
The output will look like this:

```json
{
  "appId": "xx15d42-f9xx-45xx-xx9a-3dxxxxxxxxf2",
  "displayName": "azure-cli-xxx-08-xx-07-xx-37",
  "name": "http://azure-cli-xxxx-08-xx-07-xx-37",
  "password": "7xxxxxx-xxxe-4xxx-xxxf-exxxxxxxxxx4",
  "tenant": "9xxxxxx0-cxxx-xxx4-bxxx-cxxxxxxxxxx3"
}
```

If you experience any problem with Service Principal, take a look at [this wiki page](../../wiki/About-Service-Principals).

#### SendGrid key

AzureWebAppSSLManager sends emails using the *Microsoft.Azure.WebJobs.Extensions.SendGrid* extension, which needs a valid SendGrid API Key.

To obtain an API Key, you can follow the [official SendGrid documentation](https://sendgrid.com/docs/ui/account-and-settings/api-keys/)

## Configuration

To be able to run, AzureWebAppSSLManager needs the following configuration settings.

They can be created in the Azure Web App configuration section when deployed, or in the *local.settings.json* file when debugging.

```json
    "CertificateOwnerEmail": "YOUR_NAME@EMAIL.XXX",
    "CertificatePassword": "YOUR_PASSWORD",
    "SubscriptionID": "SUBSCRIPTION_ID",
    "ServicePrincipalClientID": "SERVICE_PRINCIPAL_APP_ID",
    "ServicePrincipalClientSecret": "SERVICE_PRINCIPAL_PASSWORD",
    "ServicePrincipalTenantID": "SERVICE_PRINCIPAL_TENANT",
    "AzureStorageAccountConnectionString": "AZURE_STORAGE_FULL_CONNECTION_STRING",
    "SendGridKey": "SENDGRID_KEY",
    "EmailSender": "SENDER@YOURSERVICE.EXT",
    "UseStaging": "[True|False]",
	"BatchSize": [<0 for no batching> | <int>],
	"TimeBeforeExpiryToRenew": "days.hours:minutes:seconds"
```

The config settings for the Service Principal are the ones from the output of the Service Principal creation above.

BatchSize is optional and defaults to 0.

TimeBeforeExpiryToRenew is optional and defaults to "30.00:00:00" (renew certificates 30 days before they expire).

## Application Properties Configuration File

Currently AzureWebAppSSLManager retrieves the list of certificates to generate and install from a json file stored in a blob storage accout.

An example of the file structure can be found in the *[appproperties.json](../master/SampleJsonConfig/appproperties.json)* example file.

The file needs to be saved in a blob container with name as in the constant "AppPropertiesFileName" of the *[Constants.cs](../master/src/WebAppSSLManager/Models/Constants.cs)* class.

## Certificate Information Configuration File

The app retrieves some information neeed for the certificate creation from a json file stored in a blob storage accout.

An example of the file structure can be found in the *[certinfo.json](../master/SampleJsonConfig/certinfo.json)* example file.

The file needs to be saved in a blob container with name as in the constant "CertInfoFileName" of the *[Constants.cs](../master/src/WebAppSSLManager/Models/Constants.cs)* class.

## Limitations

Currently an instance of AzureWebAppSSLManager can manage Web Apps and Function Apps in a single subscription.
If you need/want to manage App Service Web Apps  and Function Apps in multiple subscriptions, you would need to deploy one instance of this Function per subscription.

## Support

If you have any issue with this project please let me know through the [Issues page](https://github.com/n3wt0n/AzureWebAppSSLManager/issues) and I'll try to fix the problem as soon as possible!

If you want to contribute to this project, feel free to create a Pull Request and I will review it.
