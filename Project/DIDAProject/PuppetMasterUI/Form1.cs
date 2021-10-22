﻿using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMasterUI
{
    public partial class Form1 : Form
    {
        private string _schedulerHost;
        private int _schedulerPort;
        private string _workers;
        private string _operators;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                try
                {
                    string text = File.ReadAllText(file);
                    Console.WriteLine(text); // only to debug

                    string[] commands = text.Split('\n');
                    foreach (string s in commands)
                    {
                        commandParser(s);
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            commandParser(textBox1.Text);
            //MessageBox.Show(textBox1.Text + "\r\n" + "Command executed.");
        }

        private void commandParser(string command)
        {
            string[] instance = command.Split(' ');
            string arguments;
            string fileName;
            switch (instance[0])
            {
                case "scheduler":
                    Console.WriteLine("entered scheduler\r\n");
                    arguments = instance[1];
                    string[] handleConnection = arguments.Split(":");
                    _schedulerHost = handleConnection[1].Substring(2);
                    _schedulerPort = Int32.Parse(handleConnection[2]);
                    fileName = "DIDASchedulerUI";
                    processCreationService(fileName, arguments);
                    break;

                case "storage":
                    Console.WriteLine("entered storage\r\n");
                    arguments = instance[1] + instance[2];
                    fileName = "DIDAStorageUI";
                    processCreationService(fileName, arguments);
                    break;

                case "worker":
                    Console.WriteLine("entered worker\r\n");
                    arguments = instance[1] + instance[2];
                    _workers += arguments + "\r\n";
                    fileName = "DIDAWorkerUI";
                    processCreationService(fileName, arguments);
                    break;

                case "populate":
                    Console.WriteLine("entered populate\r\n");
                    break;

                case "client":
                    Console.WriteLine("entered client\r\n");
                    clientOpenFile(instance[2]);
                    _operators = instance[1] + "\r\n";
                    break;
            }
        }

        private static void processCreationService(string fileName, string args)
        {
            try
            {
                string execName = fileName + ".exe";
                string directory = System.IO.Directory.GetParent(Environment.CurrentDirectory).ToString();
                Console.WriteLine(@directory + "\r\n");
                string applicationPath = Path.GetFullPath(Path.Combine(directory, @"..\..\..\", fileName, @"bin\Debug\netcoreapp3.1\", execName));
                Console.WriteLine(applicationPath + "\r\n");

                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = applicationPath;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.Arguments = args;
                    process.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + _schedulerHost + ":" + _schedulerPort);
            DIDAPuppetMasterService.DIDAPuppetMasterServiceClient client = new DIDAPuppetMasterService.DIDAPuppetMasterServiceClient(channel);

            var reply = client.sendFile(new DIDAFileSendRequest { Workers = _workers, Operations = "operations" });

            if (reply.Ack.Equals("ack"))
                MessageBox.Show("Scheduler received all necessary infomation.");

            Console.ReadLine();
        }

        public List<string> clientOpenFile(string fileName)
        {
            List<string> ops = new List<string>();
            string currWorkingDir = Directory.GetCurrentDirectory(); //maybe use Desktop or Downloads
            string path = Path.GetFullPath(Path.Combine(currWorkingDir, @"..\..\..\..\scripts\", fileName));

            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                byte[] b = new byte[1024];
                UTF8Encoding temp = new UTF8Encoding(true);
                Console.WriteLine("vou imprimir");
                while (fs.Read(b, 0, b.Length) > 0)
                {
                    Console.WriteLine(temp.GetString(b));
                    string[] handleOps = temp.GetString(b).Split(" ");
                    ops.Add(handleOps[1]);
                }
            }

            return ops;
        }
    }
}