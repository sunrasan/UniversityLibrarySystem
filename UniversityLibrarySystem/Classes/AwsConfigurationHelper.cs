using System;
using System.Web;

namespace UniversityLibrarySystem
{
    /// <summary>
    /// AwsConfigurationHelper - Loads configuration from environment variables
    /// which are populated from AWS Systems Manager Parameter Store and AWS Secrets Manager
    /// at application startup (e.g., via ECS task definition environment variables or
    /// Kubernetes ConfigMap/Secret injection).
    /// 
    /// Usage pattern for AWS SSM Parameter Store:
    ///   1. Store parameters in SSM: /UniversityLibrarySystem/{env}/DbConnectionString
    ///   2. Reference in ECS task definition as environment variables
    ///   3. Read here via Environment.GetEnvironmentVariable()
    /// 
    /// Usage pattern for AWS Secrets Manager:
    ///   1. Store secrets in Secrets Manager: UniversityLibrarySystem/{env}/DbCredentials
    ///   2. Reference in ECS task definition as secrets (injected as env vars)
    ///   3. Read here via Environment.GetEnvironmentVariable()
    /// </summary>
    public static class AwsConfigurationHelper
    {
        /// <summary>
        /// Gets the database connection string from environment variable.
        /// Set DB_CONNECTION_STRING from AWS Secrets Manager secret.
        /// </summary>
        public static string GetDbConnectionString()
        {
            return Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? System.Configuration.ConfigurationManager.ConnectionStrings["ULSConnectionString"]?.ConnectionString
                ?? throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is not set. Configure it from AWS Secrets Manager.");
        }

        /// <summary>
        /// Gets the Entity Framework connection string from environment variable.
        /// Set DB_EF_CONNECTION_STRING from AWS Secrets Manager secret.
        /// </summary>
        public static string GetEfConnectionString()
        {
            return Environment.GetEnvironmentVariable("DB_EF_CONNECTION_STRING")
                ?? System.Configuration.ConfigurationManager.ConnectionStrings["ULSDBEntities"]?.ConnectionString
                ?? throw new InvalidOperationException("DB_EF_CONNECTION_STRING environment variable is not set. Configure it from AWS Secrets Manager.");
        }

        /// <summary>
        /// Gets the SMS API key from environment variable.
        /// Set SMS_API_KEY from AWS Secrets Manager secret.
        /// </summary>
        public static string GetSmsApiKey()
        {
            return Environment.GetEnvironmentVariable("SMS_API_KEY") ?? "";
        }

        /// <summary>
        /// Gets the SMS API URL from environment variable.
        /// Set SMS_API_URL from AWS Systems Manager Parameter Store.
        /// </summary>
        public static string GetSmsApiUrl()
        {
            return Environment.GetEnvironmentVariable("SMS_API_URL") ?? "https://api.sms.ir/v1/send";
        }

        /// <summary>
        /// Gets the SMS verify API URL from environment variable.
        /// Set SMS_VERIFY_API_URL from AWS Systems Manager Parameter Store.
        /// </summary>
        public static string GetSmsVerifyApiUrl()
        {
            return Environment.GetEnvironmentVariable("SMS_VERIFY_API_URL") ?? "https://api.sms.ir/v1/send/verify";
        }

        /// <summary>
        /// Gets the application environment (dev/staging/production).
        /// Set ASPNET_ENVIRONMENT from ECS task definition or Kubernetes ConfigMap.
        /// </summary>
        public static string GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNET_ENVIRONMENT") ?? "Production";
        }

        /// <summary>
        /// Gets the AWS region for SDK clients.
        /// Set AWS_REGION from ECS task definition (automatically set in ECS).
        /// </summary>
        public static string GetAwsRegion()
        {
            return Environment.GetEnvironmentVariable("AWS_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")
                ?? "us-east-1";
        }
    }
}
