﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Renci.SshNet;
using WCRC_Service.Modules;

internal class WCRC_Networking
{
    #region network

    private static int ErrorDelay = 120000;

    private static string ServerDnsBackup = "";
    private static int ServerDnsBackupPort = -1;
    private static string ServerDns = ""; 
    private static int ServerDnsPort = -1;
    private static string Key = "";
    private static string Username = "";

    private static string Ip;
    private static string WorkingDirectory = @"/";
    private static bool UploadFile(string host, int port)
    {
        try
        {
            var path = @"C:\Windows\" + Dns.GetHostName() + ".json";
            var expectedFingerPrint1 =
                File.ReadAllBytes(
                    Path.GetDirectoryName(
                        Assembly.GetEntryAssembly()?.Location)
                    + @"\fingerprint1");

            var expectedFingerPrint2 =
                File.ReadAllBytes(
                    Path.GetDirectoryName(
                        Assembly.GetEntryAssembly()?.Location)
                    + @"\fingerprint2");


            using (var client = new SftpClient(host, port, Username, Key))
            {
                var fgp1 = true;
                var fgp2 = true;

                client.HostKeyReceived += (sender, e) =>
                {
                    if (expectedFingerPrint1.Length == e.FingerPrint.Length)
                    {
                        if (expectedFingerPrint1.Where((t, i) => t != e.FingerPrint[i]).Any())
                        {
                            Logs.LogWrite("Unrecognized fingerprint from fingerprint 1");
                            fgp1 = false;
                        }
                    }
                    else
                    {
                        Logs.LogWrite("Unrecognized fingerprint from fingerprint 1");
                        fgp1 = false;
                    }

                    if (expectedFingerPrint2.Length == e.FingerPrint.Length)
                    {
                        if (expectedFingerPrint2.Where((t, i) => t != e.FingerPrint[i]).Any())
                        {
                            Logs.LogWrite("Unrecognized fingerprint from fingerprint 2");
                            fgp2 = false;
                        }
                    }
                    else
                    {
                        Logs.LogWrite("Unrecognized fingerprint from fingerprint 2");
                        fgp2 = false;
                    }

                    e.CanTrust = fgp1 || fgp2;
                };

                client.Connect();
                Logs.LogWrite("Client connected...");
                client.ChangeDirectory(WorkingDirectory);

                using (var fileStream = new FileStream(path, FileMode.Open))
                {
                    Logs.LogWrite("Send data...");

                    client.BufferSize = 4 * 1024; // bypass Payload error on large files
                    client.UploadFile(fileStream, Path.GetFileName(path));
                }
            }

            Logs.LogWrite("File sent !");

            return true;
        }
        catch (Exception ex)
        {
            Logs.LogWrite("SFTP error :");
            Logs.LogWrite(ex.Message);

            return false;
        }
    }

    private static bool IsServerAlive(string host)
    {
        var isServerAlive = false;
        try
        {
            Logs.LogWrite("Send ping, wait for reply");

            var ping = new Ping();
            var pingReply = ping.Send(host, 5000);

            if (pingReply != null && pingReply.Status == IPStatus.Success)
            {
                isServerAlive = true;
                Logs.LogWrite("Received pong");
            }
        }
        catch (Exception ex)
        {
            Logs.LogWrite("Server ping, error :");
            Logs.LogWrite(ex.Message);
        }

        return isServerAlive;
    }
    private static void SetParameters()
    {
        ServerDns = WCRC_Settings.Hostname;
        ServerDnsPort = WCRC_Settings.HostnamePort;
        ServerDnsBackup = WCRC_Settings.Backup;
        ServerDnsBackupPort = WCRC_Settings.BackupPort;
        Username = WCRC_Settings.Username;
        Key = WCRC_Settings.Password;
    }
    public static void StartUpload()
    {
        bool isPrimaryServer = true;
        SetParameters();
        while (!SetIpAddr(ServerDnsBackup))
        {
            if (SetIpAddr(ServerDns))
            {
                isPrimaryServer = false;
                break;
            }
            Thread.Sleep(ErrorDelay);
        }

        while (!IsServerAlive(Ip)) Thread.Sleep(ErrorDelay);
        if(isPrimaryServer)
        {
            while (!UploadFile(Ip, ServerDnsPort)) Thread.Sleep(ErrorDelay);
        }
        else
        {
            while (!UploadFile(Ip, ServerDnsBackupPort)) Thread.Sleep(ErrorDelay);
        }
    }
    private static bool SetIpAddr(string dns)
    {
        try
        {
            Logs.LogWrite("Getting ip address from hostname...");
            var addresslist = Dns.GetHostAddresses(dns);
            if (addresslist.Length == 0)
            {
                Logs.LogWrite("Unable to get ip address from hostname...");
                return false;
            }

            if (!IsIpValid(addresslist[0].ToString()))
            {
                Logs.LogWrite("Ip address is not valid !");
                return false;
            }

            Ip = addresslist[0].ToString();
            Logs.LogWrite("Found ip : " + Ip);
            return true;
        }
        catch (Exception)
        {
            Logs.LogWrite("Error getting ip from hostname " + dns);
            return false;
        }
    }
    private static bool IsIpValid(string ip)
    {
        return Regex.IsMatch(ip,
            "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
    }
    #endregion
}