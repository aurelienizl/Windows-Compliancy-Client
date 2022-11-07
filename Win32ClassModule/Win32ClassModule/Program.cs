﻿using System.Threading;
using WindowsReportingClient;
using WindowsReportingClient.System;
using WindowsReportingClient.Win32_Class;
using WindowsReportingClient.Win32_Modules;


namespace WindowsReportingClient;

public static class Program
{

    public static void Main()
    {
        InitReportingTool();
        InitNetwork();
        InitUI();
    }

    #region UI

    public static void InitUI()
    {
        new MainWindow().ShowDialog();
    }

    #endregion

    #region Network
    private static Thread? NetworkThread;
    public static void InitNetwork()
    {

        ReportingThread?.Join(); //Wait reporter to finished

        if (NetworkThread is null)
        {
            NetworkThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Networking.UploadReport("127.0.0.1", 443);
            });
            NetworkThread.Start();
            return;
        }
        if (!NetworkThread.IsAlive)
        {
            NetworkThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Networking.UploadReport("127.0.0.1", 443);
            });
            NetworkThread.Start();
        }
    }
    #endregion

    #region Report
    private static List<Win32_Bios>? bios { get; set; }
    private static List<Win32_EncryptableVolume>? win32_EncryptableVolumes { get; set; }
    private static List<Win32_Tpm>? win32_Tpm { get; set; }
    private static List<Win32_Product>? win32_Products { get; set; }
    private static List<X509Cert>? X509CertList { get; set; }
    private static List<Win32_QuickFixEngineering>? win32_QFE { get; set; }
    private static List<Account>? accounts { get; set; }
    private static SystemInfo? sysinfo { get; set; }
    private static Thread? ReportingThread;
    public static void InitReportingTool()
    {
        if (ReportingThread is null)
        {
            LaunchReport();
            return;
        }
        if (!ReportingThread.IsAlive)
        {
            LaunchReport();
            return;
        }
        MessageBox.Show("Reporting tool is running");
    }
    public static void LaunchReport()
    {
        ReportingThread = new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            List<Thread> threads = new List<Thread>();

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                bios = Win32_Bios.GetBios();
            }));


            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                win32_EncryptableVolumes = Win32_EncryptableVolume.GetEncryptableVolume();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                win32_Tpm = Win32_Tpm.GetTpm();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                win32_Products = Win32_Product.GetProducts();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                X509CertList = X509Cert.GetX509Cert();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                win32_QFE = Win32_QuickFixEngineering.GetQuickFixEngineering();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                accounts = Account.GetLocalUsers();
            }));

            threads.Add(new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                sysinfo = SystemInfo.GetSystemInfo();
            }));


            foreach (var item in threads)
            {
                item.Start();
            }
            foreach (var item in threads)
            {
                item.Join();
            }

            var report = new Report(
                bios,
                win32_EncryptableVolumes,
                win32_Tpm,
                win32_Products,
                X509CertList,
                win32_QFE,
                accounts,
                sysinfo
            );

            Report.GenerateReport(report);
        });
        ReportingThread.Start();
    }
    #endregion

}