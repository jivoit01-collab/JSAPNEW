using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Microsoft.Extensions.Logging;

namespace JSAPNEW.Services.Implementation
{
   

    public class SshService
    {
        private readonly ILogger<SshService> _logger;
        private readonly string _sshHost = "103.89.44.176"; // Replace with your remote server IP
        private readonly string _sshUsername = "Admin"; // Replace with your SSH username
        private readonly string _sshPassword = "Manav@2024"; // Replace with your SSH password

        public SshService(ILogger<SshService> logger)
        {
            _logger = logger;
        }

        public bool DownloadFileFromServer(string remoteFilePath, string localFilePath)
        {
            try
            {
                using (var client = new SftpClient(_sshHost, _sshUsername, _sshPassword))
                {
                    client.Connect();

                    if (!client.Exists(remoteFilePath))
                    {
                        _logger.LogWarning($"Remote file not found: {remoteFilePath}");
                        return false;
                    }

                    using (var fileStream = System.IO.File.Create(localFilePath))
                    {
                        client.DownloadFile(remoteFilePath, fileStream);
                    }

                    client.Disconnect();
                    _logger.LogInformation($"File downloaded successfully from {remoteFilePath} to {localFilePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file from SSH: {ex.Message}");
                return false;
            }
        }
    }

}
