using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppSSLManager.Models;

namespace WebAppSSLManager
{
    public static class WebAppSSLManager
    {
        private static ILogger _logger;

        //Runs once per month, on the 1st day of the month at 12 midnight
        [FunctionName("WebAppSSLManager")]
        public static async Task Run([TimerTrigger("0 0 0 1 * *"
#if DEBUG
            , RunOnStartup=true
#endif
            )]TimerInfo myTimer,
            [SendGrid(ApiKey = "SendGridKey")] IAsyncCollector<SendGridMessage> messageCollector,
            ILogger logger)
        {
            _logger = logger;
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            Settings.Init(logger);

            MailHelper.Init(logger, messageCollector);
            await MailHelper.SendEmailForActivityStartedAsync();

            AzureHelper.Init(logger);

            var errors = new List<(string hostname, string errorMessage)>();
            var appProperties = await BuildAppPropertiesListAsync();

            if (appProperties != null && appProperties.Any())
            {
                await CertificatesHelper.InitAsync(logger, CertificateMode.Production); //Change this to Staging for development/test purposes

                foreach (var appProperty in appProperties)
                {
                    AzureHelper.InitAppProperty(appProperty);
                    CertificatesHelper.InitAppProperty(appProperty);

                    try
                    {
                        //Request certificate and install it if all is ok
                        if (await CertificatesHelper.GetCertificateAsync())
                            await AzureHelper.AddCertificateAsync();
                    }
                    catch (Exception ex)
                    {
                        var message = $"Unable to complete the processing for {appProperty.Hostname}";
                        logger.LogError(ex, message);
                        await MailHelper.SendEmailForErrorAsync(ex, message);
                        errors.Add((hostname: appProperty.Hostname, errorMessage: ex.Message));
                    }
                }
            }

            AzureHelper.Dispose();
            await MailHelper.SendEmailForActivityCompletedAsync(errors);
        }

        private static async Task<IEnumerable<AppProperty>> BuildAppPropertiesListAsync()
        {
            try
            {
                var appPropertiesStr = await AzureHelper.ReadFileFromBlobStorageToStringAsync(Constants.AppPropertiesFileName);
                var appProps = JsonConvert.DeserializeObject<List<AppProperty>>(appPropertiesStr);
                return appProps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while building/deserializing App Properties list. Cannot proceed.");
                await MailHelper.SendEmailForErrorAsync(ex, "Error while building/deserializing App Properties list. Cannot proceed.");
                return null;
            }
        }
    }
}
