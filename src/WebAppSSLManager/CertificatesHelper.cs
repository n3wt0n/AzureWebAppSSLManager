using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppSSLManager.Models;

namespace WebAppSSLManager
{
    public static class CertificatesHelper
    {
        private static ILogger _logger;
        private static string _hostname;
        private static string _hostnameFriendly;
        private static string _basedomain;

        private static string _pfxFileName;
        private static string _pemFileName;

        private static AcmeContext _acme;

        private static CsrInfo _certinfo;

        public static async Task InitAsync(ILogger logger, CertificateMode certificateMode)
        {
            _logger = logger;

            _logger.LogInformation($"Initializing LetsEncrypt bits");

            //ACCOUNT
            _logger.LogInformation("    Creating or Retrieving account");

            var accountKeyFileName = Constants.AccountKeyFileName;
            if (Settings.UseStaging)
                accountKeyFileName = $"staging{accountKeyFileName}";

            IAccountContext account;

            if (await AzureHelper.CheckIfFileExistsBlobStorageAsync(accountKeyFileName))
            {
                _logger.LogInformation("        Retrieving existing account");

                // Load the saved account key
                var pemKey = await AzureHelper.ReadFileFromBlobStorageToStringAsync(accountKeyFileName);
                var accountKey = KeyFactory.FromPem(pemKey);
                _acme = new AcmeContext(certificateMode == CertificateMode.Production ? WellKnownServers.LetsEncryptV2 : WellKnownServers.LetsEncryptStagingV2, accountKey);
                account = await _acme.Account();
            }
            else
            {
                _logger.LogInformation("        Creating new account");
                _acme = new AcmeContext(certificateMode == CertificateMode.Production ? WellKnownServers.LetsEncryptV2 : WellKnownServers.LetsEncryptStagingV2);
                account = await _acme.NewAccount(Settings.CertificateOwnerEmail, true);

                // Save the account key for later use
                var pemKey = _acme.AccountKey.ToPem();
                await AzureHelper.SaveFileToBlobStorageAsync(accountKeyFileName, pemKey);
            }

            _logger.LogInformation($"    Account set - { account.Location }");
            _logger.LogInformation(Environment.NewLine);

            _logger.LogInformation("Loading Certificate Info for Issuance");
            _certinfo = await BuildCertificaeInfoAsync();
            _logger.LogInformation("    Certificate Info loaded");
            _logger.LogInformation(Environment.NewLine);
        }

        private static async Task<CsrInfo> BuildCertificaeInfoAsync()
        {
            try
            {
                var certInfoStr = await AzureHelper.ReadFileFromBlobStorageToStringAsync(Constants.CertInfoFileName);
                var certInfo = JsonConvert.DeserializeObject<CsrInfo>(certInfoStr);
                return certInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building/deserializing Certificte Info file. Cannot proceed.");
                await MailHelper.SendEmailForErrorAsync(ex, "Error while building/deserializing Certificte Info file. Cannot proceed.");
                throw;
            }
        }

        public static void InitAppProperty(AppProperty appProperty)
        {
            _hostname = appProperty.Hostname;
            _hostnameFriendly = appProperty.HostnameFriendly;
            _basedomain = appProperty.BaseDomain;

            _pfxFileName = appProperty.PfxFileName;
            _pemFileName = appProperty.PemFileName;
        }

        public static async Task<bool> GetCertificateAsync()
        {
            //ORDER
            //Place a certificate order (DNS validation is required for wildcard certificates, For non-wildcard certificate, HTTP challenge is also available. We use DNS)
            _logger.LogInformation($"Creating the order for {_hostname}");

            var order = await _acme.NewOrder(new[] { _hostname });

            _logger.LogInformation($"   Order for {_hostname} created");
            _logger.LogInformation(Environment.NewLine);

            _logger.LogInformation($"Authorizing order for {_hostname}");

            var authz = (await order.Authorizations()).First();
            var dnsChallenge = await authz.Dns();
            var dnsTxt = _acme.AccountKey.DnsTxt(dnsChallenge.Token);

            var recordFullName = $"_acme-challenge.{_hostnameFriendly}";

            _logger.LogInformation($"   DNS authorization process initiated");
            _logger.LogInformation($"       To handle this Challenge, creating a DNS record with these details:");
            _logger.LogInformation($"           DNS Record Name.....: {recordFullName}");
            _logger.LogInformation($"           DNS Record Type.....: TXT");
            _logger.LogInformation($"           DNS Record Value....: {dnsTxt}");

            var recordName = recordFullName.Replace($".{_basedomain}", "");

            await AzureHelper.RemoveDNSVerificationTXTRecord(recordName); //to be sure we start clean
            await AzureHelper.CreateDNSVerificationTXTRecord(recordName, dnsTxt);

            _logger.LogInformation($"   Waiting {Settings.WaitTimeBeforeValidate.TotalSeconds} seconds before validating DNS authorization challenge...");
            await Task.Delay(Settings.WaitTimeBeforeValidate);

            _logger.LogInformation($"   Validating DNS authorization challenge. Can take up to 90 seconds...");
            var validatedChallege = await dnsChallenge.Validate();
            var waitUntil = DateTime.Now.AddSeconds(90);

            while (validatedChallege.Status != Certes.Acme.Resource.ChallengeStatus.Valid && DateTime.Now < waitUntil)
            {
                Thread.Sleep(1 * 1000);
                validatedChallege = await dnsChallenge.Validate();
            }

            if (validatedChallege.Status == Certes.Acme.Resource.ChallengeStatus.Valid)
            {
                _logger.LogInformation($"   DNS authorization challenge successfully completed");
                _logger.LogInformation(Environment.NewLine);
                _logger.LogInformation($"Retrieving Certificate");

                _logger.LogInformation($"   Downloading the certificate");
                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

                _certinfo.CommonName = _hostname;
                var cert = await order.Generate(_certinfo, privateKey);

                _logger.LogInformation($"   Exporting full chain certification");
                var certPem = cert.ToPem();
                await AzureHelper.SaveFileToBlobStorageAsync(_pemFileName, certPem);

                _logger.LogInformation($"   Exporting PFX");
                var pfxBuilder = cert.ToPfx(privateKey);
                var pfxBytes = pfxBuilder.Build(_hostname, Settings.CertificatePassword);
                await AzureHelper.SaveFileToBlobStorageAsync(_pfxFileName, pfxBytes);

                _logger.LogInformation(Environment.NewLine);
                _logger.LogInformation($"Finalizing");
                await AzureHelper.RemoveDNSVerificationTXTRecord(recordName);
            }
            else
            {
                _logger.LogError("AUTHORIZAION PROCESS INVALID");
                return false;
            }

            return true;
        }
    }
}
