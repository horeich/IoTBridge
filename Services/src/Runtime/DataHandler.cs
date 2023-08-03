// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Horeich.Services.Diagnostics;
using Horeich.Services.Exceptions;

// using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Horeich.Services.Runtime
{
    public interface IDataHandler
    {
        string GetString(string key, string defaultValue = "");
        bool GetBool(string key, bool defaultValue = false);
        int GetInt(string key, int defaultValue = 0);
        float GetSingle(string key, float defaultValue = 0.0f);
    }

    /// <summary>
    /// The data handler class tries to get data from configuration file. Alternatively,
    /// data is fetched from key vault.
    /// </summary>
    public class DataHandler : IDataHandler
    {
        private readonly IConfigurationRoot _configuration;
        private readonly ILogger _log;
        private KeyVault _keyVault;

        // Constants
        /// <summary>
        /// Constants are formatted to be read from JSON file
        /// </summary>
        private const string CLIENT_ID = "KeyVault:AADClientId";
        private const string CLIENT_SECRET = "KeyVault:AADClientSecret";
        private const string KEY_VAULT_NAME = "KeyVault:Name";

        public DataHandler(ILogger logger)
        {
            _log = logger;

            // Read configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); 
            _configuration = configBuilder.Build();

            // Set up Key Vault
            SetUpKeyVault();
        }

        /// <summary>
        /// Get value from 1. config file, 2. key vault -> resolve environment variables
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetString(string key, string defaultValue = "")
        {
            var value = GetSecrets(key, defaultValue);
            ReplaceEnvironmentVariables(ref value, defaultValue);
            return value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = this.GetSecrets(key, defaultValue.ToString()).ToLowerInvariant();

            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };

            if (knownTrue.Contains(value)) return true;
            if (knownFalse.Contains(value)) return false;

            return defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(this.GetSecrets(key, defaultValue.ToString()));
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}'", e);
            }
        }

        public float GetSingle(string key, float defaultValue = 0.0f)
        {
            try
            {
                return Convert.ToSingle(this.GetSecrets(key, defaultValue.ToString()));
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{key}'", e);
            }
        }

        /// <summary>
        /// Resolve environment variables and set up key vault
        /// </summary>
        private void SetUpKeyVault()
        {
            var clientId = GetEnvironmentVariable(CLIENT_ID, string.Empty);
            var clientSecret = GetEnvironmentVariable(CLIENT_SECRET, string.Empty);
            var keyVaultName = GetEnvironmentVariable(KEY_VAULT_NAME, string.Empty);

            // Initailize key vault
            _keyVault = new KeyVault(keyVaultName, clientId, clientSecret, this._log);
        }

        private string GetSecrets(string key, string defaultValue = "")
        {
            string value = string.Empty;

            // Try to fetch value locally
            value = GetLocalVariable(key, defaultValue);

            // Try to fetch value from Key Vault
            if (string.IsNullOrEmpty(value))
            {
                _log.Info($"Value for secret {key} not found in local env." +
                    $" Trying to get the secret from KeyVault.");
                value = _keyVault.GetSecret(key);
            }

            // Default to empty string
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        private string GetSecretsFromKeyVault(string key) {
            return _keyVault.GetSecret(key);
        }

        private string GetLocalVariable(string key, string defaultValue = "")
        {
            return _configuration.GetValue(key, defaultValue);
        }

        /// <summary>
        /// Resolve environment variable
        /// e.g. the key vault credentials are read from environment variables
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetEnvironmentVariable(string key, string defaultValue = "")
        {
            // Key - value pair from configuration
            var value = _configuration.GetValue(key, defaultValue);

            // Resolve enironment variable
            // The default value is the value from the configuration
            ReplaceEnvironmentVariables(ref value, defaultValue);
            return value;
        }

        private void ReplaceEnvironmentVariables(ref string value, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(value)) return;

            this.ProcessMandatoryPlaceholders(ref value);

            this.ProcessOptionalPlaceholders(ref value, out bool notFound);

            if (notFound && string.IsNullOrEmpty(value))
            {
                value = defaultValue;
            }
        }

        private void ProcessMandatoryPlaceholders(ref string value)
        {
            // Pattern for mandatory replacements: ${VAR_NAME}
            const string PATTERN = @"\${([a-zA-Z_][a-zA-Z0-9_]*)}";

            // Search
            var keys = (from Match m in Regex.Matches(value, PATTERN)
                        select m.Groups[1].Value).Distinct().ToArray();

            // Replace
            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (keys.Contains(x.Key))
                {
                    value = value.Replace("${" + x.Key + "}", x.Value.ToString());
                }
            }

            // Non replaced placeholders cause an exception
            keys = (from Match m in Regex.Matches(value, PATTERN)
                    select m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                // var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                // this._log.Error("Environment variables not found", () => new { varsNotFound });
                //throw new InvalidConfigurationException("Environment variables not found: " + varsNotFound);
            }
        }

        private void ProcessOptionalPlaceholders(ref string value, out bool notFound)
        {
            notFound = false;

            // Pattern for optional replacements: ${?VAR_NAME}
            const string PATTERN = @"\${\?([a-zA-Z_][a-zA-Z0-9_]*)}";

            // Search
            var keys = (from Match m in Regex.Matches(value, PATTERN)
                        select m.Groups[1].Value).Distinct().ToArray();

            // Replace
            foreach (DictionaryEntry x in Environment.GetEnvironmentVariables())
            {
                if (keys.Contains(x.Key))
                {
                    value = value.Replace("${?" + x.Key + "}", x.Value.ToString());
                }
            }

            // Non replaced placeholders cause an exception
            keys = (from Match m in Regex.Matches(value, PATTERN)
                    select m.Groups[1].Value).ToArray();
            if (keys.Length > 0)
            {
                // Remove placeholders
                value = keys.Aggregate(value, (current, k) => current.Replace("${?" + k + "}", string.Empty));

                var varsNotFound = keys.Aggregate(", ", (current, k) => current + k);
                this._log.Info("Environment variables not found");
                //this._log.Info("Environment variables not found", () => new { varsNotFound });

                notFound = true;
            }
        }
    }
}
