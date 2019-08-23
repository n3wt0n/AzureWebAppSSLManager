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
        private static string _webAppName;
        private static string _webAppResGroup;
        private static string _hostname;
        private static string _hostnameFriendly;
        private static string _pfxFileName;

        public static void Init(ILogger log)
        {
            _logger = log;

            var subscriptionId = Environment.GetEnvironmentVariable("SubscriptionID");
            if (string.IsNullOrWhiteSpace(subscriptionId))
                log.LogError("SubscriptionID environment variable is null");

            var clientId = Environment.GetEnvironmentVariable("ServicePrincipalClientID");
            if (string.IsNullOrWhiteSpace(clientId))
                log.LogError("ServicePrincipalClientID environment variable is null");

            var clientSecret = Environment.GetEnvironmentVariable("ServicePrincipalClientSecret");
            if (string.IsNullOrWhiteSpace(clientSecret))
                log.LogError("ServicePrincipalClientSecret environment variable is null");

            var tenantId = Environment.GetEnvironmentVariable("ServicePrincipalTenantID");
            if (string.IsNullOrWhiteSpace(tenantId))
                log.LogError("ServicePrincipalTenantID environment variable is null");


            var storageConnectionString = Environment.GetEnvironmentVariable("AzureStorageAccountConnectionString");
            if (string.IsNullOrWhiteSpace(storageConnectionString))
                log.LogError("AzureStorageAccountConnectionString environment variable is null");

            _logger.LogInformation($"Initializing Azure bits");
            var credentials = SdkContext.AzureCredentialsFactory
                                    .FromServicePrincipal(clientId,
                                    clientSecret,
                                    tenantId,
                                    AzureEnvironment.AzureGlobalCloud);
            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);

            _logger.LogInformation($"   Selected subscription: {_azure.SubscriptionId}");
            _logger.LogInformation(Environment.NewLine);

            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(Constants.CertificateBlobContainer);
        }

        public static void InitAppProperty(AppProperty appProperty)
        {
            _dnsZoneName = appProperty.AzureDnsZoneName;
            _dnsResGroup = appProperty.AzureDnsResGroup;
            _webAppName = appProperty.AzureWebAppName;
            _webAppResGroup = appProperty.AzureWebAppResGroup;
            _hostname = appProperty.Hostname;
            _hostnameFriendly = appProperty.HostnameFriendly;
            _pfxFileName = appProperty.PfxFileName;
        }

        public static async Task CreateDNSVerificationTXTRecord(string name, string value)
        {
            try
            {
                _logger.LogInformation($"Updating DNS zone by adding the {name} TXT record...");

                var rootDnsZone = _azure.DnsZones.ListByResourceGroup(_dnsResGroup).Where(z => z.Name == _dnsZoneName.ToLower()).SingleOrDefault();
                rootDnsZone = await rootDnsZone.Update()
                            .DefineTxtRecordSet(name)
                                .WithText(value)
                                .WithTimeToLive(1)
                                .Attach()
                            .ApplyAsync();

                _logger.LogInformation($"   Added TXT record {name} to DNS zone {rootDnsZone.Name}");
                _logger.LogInformation(Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while creating TXT record {name} to DNS zone {_dnsResGroup}");
                throw;
            }
        }

        public static async Task RemoveDNSVerificationTXTRecord(string name)
        {
            try
            {
                _logger.LogInformation($"Updating DNS zone by Removing the {name} TXT record...");

                var rootDnsZone = _azure.DnsZones.ListByResourceGroup(_dnsResGroup).Where(z => z.Name == _dnsZoneName.ToLower()).SingleOrDefault();
                rootDnsZone = await rootDnsZone.Update()
                                .WithoutTxtRecordSet(name)
                                .ApplyAsync();

                _logger.LogInformation($"   Removed TXT record {name} to DNS zone {rootDnsZone.Name}");
                _logger.LogInformation(Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while removing TXT record {name} to DNS zone {_dnsResGroup}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the webapp</param>
        /// <param name="hostname">single hostname (www.domain.ext) or wildcard (*.domain.ext)</param>
        /// <param name="pfxFileName"></param>
        /// <returns></returns>
        public static async Task AddCertificateToWebAppAsync()
        {
            _logger.LogInformation($"Adding certificate to App Service and registering the Bindings");
            var webApp = _azure.WebApps.ListByResourceGroup(_webAppResGroup).Where(w => w.Name == _webAppName.ToLower()).SingleOrDefault();

            var hostnamesList = new List<string>();
            if (_hostname.StartsWith("*."))
                hostnamesList.AddRange(webApp.HostNames.Where(h => h.Contains(_hostnameFriendly)));
            else
                hostnamesList.Add(_hostname);

            //Retrieving old certificate, if any
            _logger.LogInformation($"   Retrieving old certificate, if any");

            var oldCertificates = _azure.AppServices.AppServiceCertificates.ListByResourceGroup(_webAppResGroup).Where(c => c.HostNames.Contains(_hostname));
            _logger.LogInformation($"   Found {oldCertificates.Count()}");

            _logger.LogInformation($"   Upoading Certificate");

            var pfxByteArrayContent = await ReadFileFromBlobStorageToByteArrayAsync(_pfxFileName);

            var certificate = await _azure.AppServices.AppServiceCertificates
                                        .Define($"{_hostname}_{DateTime.UtcNow.ToString("yyyyMMdd")}")
                                        .WithRegion(webApp.Region)
                                        .WithExistingResourceGroup(webApp.ResourceGroupName)
                                        .WithPfxByteArray(pfxByteArrayContent)
                                        .WithPfxPassword(Constants.DefaultCertPassword)
                                        .CreateAsync();

            var certificateThumbPrint = certificate.Thumbprint;
            _logger.LogInformation($"   Certificate Uploaded");
            _logger.LogInformation($"   Bindings to process: {hostnamesList.Count}");

            foreach (var hostname in hostnamesList)
            {
                try
                {
                    var subdomain = hostname.Remove(hostname.IndexOf('.'));
                    var domain = hostname.Replace($"{subdomain}.", "");

                    _logger.LogInformation($"       Updating {hostname}");

                    webApp = await webApp
                                    .Update()
                                    .WithThirdPartyHostnameBinding(domain, subdomain)
                                    .DefineSslBinding()
                                        .ForHostname(hostname)
                                        .WithExistingCertificate(certificateThumbPrint)
                                        .WithSniBasedSsl()
                                        .Attach()
                                    .ApplyAsync();

                    _logger.LogInformation($"       Done");
                }
                catch (Microsoft.Azure.Management.AppService.Fluent.Models.DefaultErrorResponseException ex)
                {
                    _logger.LogError(ex, $"Error updating binding for {hostname} with certificate {certificateThumbPrint}");
                    throw;
                }
                catch (Microsoft.Rest.TransientFaultHandling.HttpRequestWithStatusException ex2)
                {
                    _logger.LogError(ex2, $"Error updating binding for {hostname} with certificate {certificateThumbPrint}");
                    throw;
                }
                catch (Exception ex3)
                {
                    _logger.LogError(ex3, $"Error updating binding for {hostname} with certificate {certificateThumbPrint}");
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
                        _logger.LogInformation($"       Removed old {oldCert.Name} certificate");
                    }
                    else
                        _logger.LogWarning($"       Can't remove {oldCert.Name} certificate because has the same Thumbprint than current one.");
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
