using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Microsoft.Rest;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Azure.Core;

namespace StopElasticPoolFunction
{
    public static class CheckAndStopElasticPool
    {
        private static readonly string subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        private static readonly string resourceGroupName = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP_NAME");
        private static readonly string aksClusterName = Environment.GetEnvironmentVariable("AKS_CLUSTER_NAME");
        private static readonly string sqlServerName = Environment.GetEnvironmentVariable("SQL_SERVER_NAME");
        private static readonly string elasticPoolName = Environment.GetEnvironmentVariable("ELASTIC_POOL_NAME");

        [FunctionName("CheckAndStopElasticPool")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CheckAndStopElasticPool function processed a request.");

            try
            {
                log.LogInformation("Checking Kubernetes cluster status.");

                if (!await IsKubernetesClusterRunningAsync(log))
                {
                    log.LogInformation("Kubernetes cluster is down. Stopping the Azure SQL elastic pool.");
                    await StopElasticPoolAsync(log);
                    return new OkObjectResult("Elastic pool stopped because Kubernetes cluster is down.");
                }
                else
                {
                    log.LogInformation("Kubernetes cluster is running. No action needed.");
                    return new OkObjectResult("Kubernetes cluster is running. No action needed.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        private static async Task<bool> IsKubernetesClusterRunningAsync(ILogger log)
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
                                          Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"),
                                          Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
                                          AzureEnvironment.AzureGlobalCloud);
                var azure = Microsoft.Azure.Management.Fluent.Azure
                                .Authenticate(credentials)
                                .WithSubscription(subscriptionId);

                var aksCluster = await azure.KubernetesClusters.GetByResourceGroupAsync(resourceGroupName, aksClusterName);
                return aksCluster.ProvisioningState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                log.LogError($"Error checking Kubernetes cluster status: {ex.Message}");
                return false;
            }
        }

        private static async Task StopElasticPoolAsync(ILogger log)
        {
            try
            {
                var tokenCredential = new DefaultAzureCredential();
                var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
                var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext);

                var sqlManagementClient = new SqlManagementClient(new TokenCredentials(accessToken.Token))
                {
                    SubscriptionId = subscriptionId
                };

                var elasticPool = await sqlManagementClient.ElasticPools.GetAsync(resourceGroupName, sqlServerName, elasticPoolName);

                var updateParams = new ElasticPool
                {
                    Location = elasticPool.Location,
                    Sku = new Sku
                    {
                        Name = "Basic",
                        Tier = "Basic"
                    }
                };

                await sqlManagementClient.ElasticPools.CreateOrUpdateAsync(resourceGroupName, sqlServerName, elasticPoolName, updateParams);
            }
            catch (Exception ex)
            {
                log.LogError($"Error stopping elastic pool: {ex.Message}");
            }
        }
    }
}
