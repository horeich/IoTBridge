// // (c) 2024 HOREICH GmbH

using Horeich.Services.Diagnostics;
using Microsoft.Azure.KeyVault;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Horeich.Services.Runtime
{
    public class KeyVault
    {
        // Key Vault details and access
        private readonly string _name;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private ILogger _log;
        private readonly KeyVaultClient _keyVaultClient;
        private const string KEY_VAULT_URI = "https://{0}.vault.azure.net/secrets/{1}";

        public KeyVault(string name, string clientId,  string clientSecret, ILogger logger)
        {
            _name = name;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _log = logger;
            _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(this.GetToken));
        }

        /// <summary>
        /// Get secret from key vault
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public string GetSecret(string secretKey)
        {
            secretKey = secretKey.Split(':').Last();
            var uri = string.Format(KEY_VAULT_URI, _name, secretKey);

            try
            {
                return _keyVaultClient.GetSecretAsync(uri).Result.Value;
            }
            catch (Exception)
            {
               _log.Error($"Secret {secretKey} not found in Key Vault.");
                return null;
            }
        }

        ///the method that will be provided to the KeyVaultClient
        private async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(_clientId, _clientSecret);  
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                _log.Debug($"Failed to obtain authentication token from key vault.");
                Console.WriteLine("Failed to init key vault");
                throw new System.InvalidOperationException("Failed to obtain the JWT token");
            }
            return result.AccessToken;
        }
    }
}

// using Horeich.Services.Diagnostics;
// using Azure.Security.KeyVault.Secrets;
// using Azure.Identity;

// using Microsoft.IdentityModel.Clients.ActiveDirectory;
// using System;
// using System.Linq;
// using System.Threading.Tasks;

// namespace Horeich.Services.Runtime
// {
//     public class KeyVault
//     {
//         // Key Vault details and access
//         private readonly string _name;
//         private readonly string _clientId;
//         private readonly string _clientSecret;
//         private ILogger _log;
//         private readonly SecretClient _secretClient;
//         private const string KEY_VAULT_URI = "https://{0}.vault.azure.net/secrets/{1}";

//         public KeyVault(string name, string clientId,  string clientSecret, ILogger logger)
//         {
//             _name = name;
//             _clientId = clientId;
//             _clientSecret = clientSecret;
//             _log = logger;
            
//             // Note: creates a new HTTP client internally
//             _secretClient = new SecretClient(new Uri(KEY_VAULT_URI), new DefaultAzureCredential()); //new KeyVaultClient.AuthenticationCallback(this.GetToken));
//         }

//         /// <summary>
//         /// Get secret from key vault
//         /// </summary>
//         /// <param name="secretKey"></param>
//         /// <returns></returns>
//         public string GetSecret(string secretKey)
//         {
//             secretKey = secretKey.Split(':').Last();
//             var uri = string.Format(KEY_VAULT_URI, _name, secretKey);

//             try
//             {
//                 KeyVaultSecret secret = _secretClient.GetSecretAsync(secretKey).Result.Value;
//                 return secret.Value;
//                 //return _keyVaultClient.GetSecretAsync(uri).Result.Value;
//             }
//             catch (Exception)
//             {
//                _log.Error($"Secret {secretKey} not found in Key Vault.");
//                 return null;
//             }
//         }

//         ///the method that will be provided to the KeyVaultClient
//         private async Task<string> GetToken(string authority, string resource, string scope)
//         {
//             // var authContext = new AuthenticationContext(authority);
//             // ClientCredential clientCred = new ClientCredential(_clientId, _clientSecret);  
//             // AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

//             // if (result == null)
//             // {
//             //     _log.Debug($"Failed to obtain authentication token from key vault.");
//             //     Console.WriteLine("Failed to init key vault");
//             //     throw new System.InvalidOperationException("Failed to obtain the JWT token");
//             // }
//             // return result.AccessToken;
//             return "test";
//         }
//     }
// }