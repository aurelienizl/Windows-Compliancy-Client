﻿using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace Windows_Compliancy_Report_Client
{
    public class FileSender : IStartable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _filepath;
        private readonly FileService _fileService;

        public FileSender(string host, int port, string filepath)
        {
            _host = host;
            _port = port;
            _filepath = filepath;
            _fileService = new FileService();
        }

        public void Start()
        {
            FileStream fs = new FileStream(_filepath, FileMode.Open);

            try
            {
                int bufferSize = 1024;
                byte[] buffer, header;

                int bufferCount = Convert.ToInt32(Math.Ceiling(fs.Length / (double)bufferSize));

                TcpClient tcpClient = new TcpClient(_host, _port);
                tcpClient.SendTimeout = 5000;
                tcpClient.ReceiveTimeout = 5000;

                header = new byte[bufferSize];
                var fileHeaders = _fileService.GetFileInfo(_filepath);
                string headerStr = JsonConvert.SerializeObject(fileHeaders);

                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                tcpClient.Client.Send(header);

                for (int i = 0; i < bufferCount; i++)
                {
                    buffer = new byte[bufferSize];
                    int size = fs.Read(buffer, 0, bufferSize);
                    tcpClient.Client.Send(buffer, size, SocketFlags.Partial);
                }

                tcpClient.Client.Close();
                fs.Close();
            }
            catch (Exception e)
            {
                Program.window?.Writeline(e.Message, false);
                fs.Close();
                throw;
            }

        }


    }
}