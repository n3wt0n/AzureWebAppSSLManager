﻿using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebAppSSLManager.Models;

namespace WebAppSSLManager
{
    public static class AzureHelper
    {
        private static ILogger _logger;
        private static IAzure _azure;
        private static CloudBlobClient _blobClient;
        private static CloudBlobContainer _blobContainer;
        private static string _dnsZoneName;
        private static string _dnsResGroup;
        private static string _resourceName;
        private static string _resourceResGroup;
        private static string _resourcePlanResGroup;
        private static string _hostname;
        private static string _hostnameFriendly;
        private static string _pfxFileName;
        private static ResourceType _resourceType;
        private static string _slotName;

        public static void Init(ILogger logger)
        {
            _logger = logger;

            _logger.LogInformation($"Initializing Azure bits");
            var credentials = SdkContext.AzureCredentialsFactory
                                    .FromServicePrincipal(Settings.ServicePrincipalClientID,
                                    Settings.ServicePrincipalClientSecret,
                                    Settings.ServicePrincipalTenantID,
                                    AzureEnvironment.AzureGlobalCloud);

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(Settings.SubscriptionID);

            _logger.LogInformation($"   Selected subscription: {_azure.SubscriptionId}");
            _logger.LogInformation(Environment.NewLine);

            var storageAccount = CloudStorageAccount.Parse(Settings.AzureStorageAccountConnectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(Constants.CertificateBlobContainer);
        }

        public static void InitAppProperty(AppProperty appProperty)
        {
            _dnsZoneName = appProperty.AzureDnsZoneName.Trim();
            _dnsResGroup = appProperty.AzureDnsResGroup.Trim();
            _resourceName = appProperty.ResourceName.Trim();
            _resourceResGroup = appProperty.ResourceResGroup.Trim();
            _resourcePlanResGroup = appProperty.PlanResourceGroup.Trim();
            _hostname = appProperty.Hostname.Trim();
            _hostnameFriendly = appProperty.HostnameFriendly;
            _pfxFileName = appProperty.PfxFileName;

            if (appProperty.IsFunctionApp && appProperty.IsSlot)
            {
                _resourceType = ResourceType.FunctionAppSlot;
                _slotName = appProperty.SlotName.Trim();
            }
            else if (appProperty.IsSlot)
            {
                _resourceType = ResourceType.WebAppSlot;
                _slotName = appProperty.SlotName.Trim();
            }
            else if (appProperty.IsFunctionApp)
                _resourceType = ResourceType.FunctionApp;
            else
                _resourceType = ResourceType.WebApp;
        }

        public static async Task CreateDNSVerificationTXTRecord(string name, string value)
        {
            try
            {
                _logger.LogInformation($"Updating DNS zone by adding the '{name}' TXT record...");

                var rootDnsZone = _azure.DnsZones.ListByResourceGroup(_dnsResGroup).Where(z => z.Name == _dnsZoneName.ToLower()).SingleOrDefault();
                rootDnsZone = await rootDnsZone.Update()
                            .DefineTxtRecordSet(name)
                                .WithText(value)
                                .WithTimeToLive(1)
                                .Attach()
                            .ApplyAsync();

                _logger.LogInformation($"   Added TXT record '{name}' to DNS zone {rootDnsZone.Name}");
                _logger.LogInformation(Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while creating TXT record '{name}' to DNS zone {_dnsResGroup}");
                throw;
            }
        }

        public static async Task RemoveDNSVerificationTXTRecord(string name)
        {
            try
            {
                _logger.LogInformation($"Updating DNS zone by Removing the '{name}' TXT record...");

                var rootDnsZone = _azure.DnsZones.ListByResourceGroup(_dnsResGroup).Where(z => z.Name == _dnsZoneName.ToLower()).SingleOrDefault();
                rootDnsZone = await rootDnsZone.Update()
                                .WithoutTxtRecordSet(name)
                                .ApplyAsync();

                _logger.LogInformation($"   Removed TXT record '{name}' to DNS zone {rootDnsZone.Name}");
                _logger.LogInformation(Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while removing TXT record '{name}' to DNS zone {_dnsResGroup}");
                throw;
            }
        }

        /// <summary>
        /// Add the certificate to the resource on Azure
        /// </summary>
        /// <returns></returns>
        public static async Task AddCertificateAsync()
        {
            _logger.LogInformation($"Adding certificate to App Service and registering the Bindings");

            ISet<string> hostnamesInternal;
            var hostnamesList = new List<string>();
            Region region;
            IResource resource;

            switch (_resourceType)
            {
                case ResourceType.WebAppSlot:
                    var slot = await _azure.WebApps.ListByResourceGroup(_resourceResGroup).Where(w => w.Name.Equals(_resourceName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault().DeploymentSlots.GetByNameAsync(_slotName);
                    hostnamesInternal = slot.HostNames;

                    region = slot.Region;
                    resource = slot;

                    break;
                case ResourceType.FunctionApp:
                    var functionApp = _azure.AppServices.FunctionApps.ListByResourceGroup(_resourceResGroup).Where(fa => fa.Name.Equals(_resourceName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
                    hostnamesInternal = functionApp.HostNames;

                    region = functionApp.Region;
                    resource = functionApp;

                    break;
                case ResourceType.FunctionAppSlot:
                    var functionAppSlot = await _azure.AppServices.FunctionApps.ListByResourceGroup(_resourceResGroup).Where(fa => fa.Name.Equals(_resourceName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault().DeploymentSlots.GetByNameAsync(_slotName);
                    hostnamesInternal = functionAppSlot.HostNames;

                    region = functionAppSlot.Region;
                    resource = functionAppSlot;

                    break;
                case ResourceType.WebApp:
                default:
                    var webApp = _azure.WebApps.ListByResourceGroup(_resourceResGroup).Where(w => w.Name.Equals(_resourceName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
                    hostnamesInternal = webApp.HostNames;

                    region = webApp.Region;
                    resource = webApp;

                    break;
            }

            if (_hostname.StartsWith("*."))
                hostnamesList.AddRange(hostnamesInternal.Where(h => h.EndsWith($".{_hostnameFriendly}")));
            else
                hostnamesList.Add(_hostname);

            //Retrieving old certificate, if any
            _logger.LogInformation($"   Retrieving old certificate, if any");

            var oldCertificates = _azure.AppServices.AppServiceCertificates.ListByResourceGroup(_resourcePlanResGroup).Where(c => c.HostNames.Contains(_hostname));
            _logger.LogInformation($"   Found {oldCertificates.Count()}");

            _logger.LogInformation($"   Uploading Certificate");

            var pfxByteArrayContent = await ReadFileFromBlobStorageToByteArrayAsync(_pfxFileName);

            var certificate = await _azure.AppServices.AppServiceCertificates
                                        .Define($"{_hostname}_{DateTime.UtcNow.ToString("yyyyMMdd")}")
                                        .WithRegion(region)
                                        .WithExistingResourceGroup(_resourcePlanResGroup)
                                        .WithPfxByteArray(pfxByteArrayContent)
                                        .WithPfxPassword(Settings.CertificatePassword)
                                        .CreateAsync();

            var certificateThumbPrint = certificate.Thumbprint;
            _logger.LogInformation($"   Certificate Uploaded");
            _logger.LogInformation($"   Bindings to process: {hostnamesList.Count}");

            foreach (var hostname in hostnamesList)
            {
                try
                {
                    switch (_resourceType)
                    {
                        case ResourceType.WebAppSlot:
                            var slot = resource as IDeploymentSlot;
                            _logger.LogInformation($"       Updating '{hostname}' on WebApp Slot '{slot.Name}'");

                            slot = await slot
                                    .Update()
                                        .DefineSslBinding()
                                            .ForHostname(hostname)
                                            .WithExistingCertificate(certificateThumbPrint)
                                            .WithSniBasedSsl()
                                            .Attach()
                                    .ApplyAsync();
                            break;
                        case ResourceType.FunctionApp:
                            var functionApp = resource as IFunctionApp;
                            _logger.LogInformation($"       Updating '{hostname}' on FunctionApp '{functionApp.Name}'");

                            functionApp = await functionApp
                                            .Update()
                                                .DefineSslBinding()
                                                    .ForHostname(hostname)
                                                    .WithExistingCertificate(certificateThumbPrint)
                                                    .WithSniBasedSsl()
                                                    .Attach()
                                            .ApplyAsync();
                            break;
                        case ResourceType.FunctionAppSlot:
                            var functionAppSlot = resource as IFunctionDeploymentSlot;
                            _logger.LogInformation($"       Updating '{hostname}' on FunctionApp Slot '{functionAppSlot.Name}'");

                            functionAppSlot = await functionAppSlot
                                                .Update()
                                                    .DefineSslBinding()
                                                        .ForHostname(hostname)
                                                        .WithExistingCertificate(certificateThumbPrint)
                                                        .WithSniBasedSsl()
                                                        .Attach()
                                                .ApplyAsync();
                            break;
                        case ResourceType.WebApp:
                        default:
                            var webApp = resource as IWebApp;
                            _logger.LogInformation($"       Updating '{hostname}' on WebApp '{webApp.Name}'");

                            webApp = await webApp
                                        .Update()
                                        .DefineSslBinding()
                                            .ForHostname(hostname)
                                            .WithExistingCertificate(certificateThumbPrint)
                                            .WithSniBasedSsl()
                                            .Attach()
                                        .ApplyAsync();
                            break;
                    }

                    _logger.LogInformation($"       Done");
                }
                catch (Microsoft.Azure.Management.AppService.Fluent.Models.DefaultErrorResponseException ex)
                {
                    _logger.LogError(ex, $"Error updating binding for '{hostname}' with certificate '{certificateThumbPrint}'");
                    throw;
                }
                catch (Microsoft.Rest.TransientFaultHandling.HttpRequestWithStatusException ex2)
                {
                    _logger.LogError(ex2, $"Error updating binding for '{hostname}' with certificate '{certificateThumbPrint}'");
                    throw;
                }
                catch (Exception ex3)
                {
                    _logger.LogError(ex3, $"Error updating binding for '{hostname}' with certificate '{certificateThumbPrint}'");
                    throw;
                }
            }
            _logger.LogInformation($"   All bindings processed and secured with SSL");

            if (oldCertificates.Any())
            {
                _logger.LogInformation($"   Removing old certificates");

                foreach (var oldCert in oldCertificates)
                {
                    if (oldCert.Thumbprint != certificate.Thumbprint)
                    {
                        await _azure.AppServices.AppServiceCertificates.DeleteByIdAsync(oldCert.Id);
                        _logger.LogInformation($"       Removed old '{oldCert.Name}' certificate");
                    }
                    else
                        _logger.LogWarning($"       Can't remove '{oldCert.Name}' certificate because has the same Thumbprint than current one.");
                }
            }

            _logger.LogInformation(Environment.NewLine);
            _logger.LogInformation($"All the operations completed.");
        }

        public static async Task SaveFileToBlobStorageAsync(string filename, string textContent)
        {
            var container = _blobClient.GetContainerReference(Constants.CertificateBlobContainer);

            var blob = container.GetBlockBlobReference(filename);

            await blob.UploadTextAsync(textContent);
        }

        public static async Task SaveFileToBlobStorageAsync(string filename, byte[] byteContent)
        {
            var blob = _blobContainer.GetBlockBlobReference(filename);

            await blob.UploadFromByteArrayAsync(byteContent, 0, byteContent.Length);
        }

        public static async Task<byte[]> ReadFileFromBlobStorageToByteArrayAsync(string filename)
            => (await ReadFileFromBlobStorageToStreamAsync(filename)).ToArray();

        public static async Task<MemoryStream> ReadFileFromBlobStorageToStreamAsync(string filename)
        {
            var blob = _blobContainer.GetBlockBlobReference(filename);

            var ms = new MemoryStream();
            await blob.DownloadToStreamAsync(ms);

            return ms;
        }

        public static async Task<string> ReadFileFromBlobStorageToStringAsync(string filename)
        {
            var blob = _blobContainer.GetBlockBlobReference(filename);

            return await blob.DownloadTextAsync();
        }

        public static async Task<bool> CheckIfFileExistsBlobStorageAsync(string filename)
        {
            var blob = _blobContainer.GetBlockBlobReference(filename);

            return await blob.ExistsAsync();
        }

        public static void Dispose()
        {
            _logger = null;
            _blobContainer = null;
            _blobClient = null;
            _azure = null;
        }
    }
}
