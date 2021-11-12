﻿using Grpc.Core;
using Grpc.Net.Client;
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
        private string _urlPM = "";
        private bool _debugMode = false;
        private string _schedulerHost = "";
        private int _schedulerPort;
        private string _workers = "";
        private string _operators = "";
        private string _populateData = "";
        private string _storageNodes = "";
        private List<string> _commands = new List<string>();
        private bool _sentInits = false;
        private string _previousCommand = "";
        private DIDAPuppetMasterService.DIDAPuppetMasterServiceClient _client = null;
        DIDAProcessCreationService.DIDAProcessCreationServiceClient _processClient = null;
        private Dictionary<string, DIDAProcessCreationService.DIDAProcessCreationServiceClient> _usedClientsMap = new Dictionary<string, DIDAProcessCreationService.DIDAProcessCreationServiceClient>();
        private Dictionary<string, DIDAProcessCreationService.DIDAProcessCreationServiceClient> _storageNodesMap = new Dictionary<string, DIDAProcessCreationService.DIDAProcessCreationServiceClient>();
        private DebugService _debugService = new DebugService();

        public Form1()
        {
            InitializeComponent();
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            int port = 10001;
            string host = "localhost";
            _urlPM = "http://" + host + ":" + port.ToString();

            Server server = new Server
            {
                Services = { DIDADebugService.BindService(_debugService) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) } //need to do ipconfig when testing on lab -- disabled call
            };

            _debugService.addForm(this);
            server.Start();
            server.ShutdownAsync().Wait();

        }

        private bool isInit(string command)
        {
            if (command.Equals("scheduler") || command.Equals("worker") || command.Equals("storage"))
                return true;
            else
                return false;
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
                    Console.WriteLine(text);

                    foreach (string command in text.Split('\n'))
                    {
                        _commands.Add(command);
                    }

                    textBox1.Text = _commands[0];
                }
                catch (IOException)
                {
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_commands.Count != 0)
            {
                string[] tempSplit = _commands[0].Split(" ");
                if (isInit(_previousCommand) && !isInit(tempSplit[0]))
                    _sentInits = true;

                if (_sentInits)
                {
                    startFunc();
                    _sentInits = false;
                }
                textBox1.Text = _commands[0];
                commandParser(_commands[0]);

                _previousCommand = tempSplit[0];
                _commands.RemoveAt(0);
                if (_commands.Count != 0)
                    textBox1.Text = _commands[0];
                else
                    textBox1.Text = "EOF -- no more commands to execute";
            }
        }

        private void commandParser(string command)
        {
            string[] instance = command.Split(' ');
            string arguments;
            string fileName;
            switch (instance[0])
            {
                case "scheduler":
                    //instance[1] -- é o server_id
                    arguments = instance[2]; //url
                    string[] handleConnection = arguments.Split(":");
                    _schedulerHost = handleConnection[1][2..];
                    _schedulerPort = Int32.Parse(handleConnection[2]);
                    fileName = "DIDASchedulerUI";
                    processCreationService(fileName, arguments, "scheduler");
                    GrpcChannel channel = GrpcChannel.ForAddress("http://" + _schedulerHost + ":" + _schedulerPort);
                    _client = new DIDAPuppetMasterService.DIDAPuppetMasterServiceClient(channel);
                    break;

                case "storage":
                    arguments = instance[1] + " " + instance[2] + " " + instance[3]; //id url delay
                    _storageNodes += arguments + ";";
                    fileName = "DIDAStorageUI";
                    processCreationService(fileName, arguments, "storage");
                    break;

                case "worker":
                    arguments = instance[1] + " " + instance[2] + " " + instance[3]; //id url delay
                    _workers += arguments + ";";
                    fileName = "DIDAWorkerUI";
                    processCreationService(fileName, arguments, "worker");
                    break;

                case "populate":
                    _populateData = openFile("populate", instance[1]); //instance[1] file to open
                    _client.sendPostInitAsync(new DIDAPostInitRequest { Data = _populateData, Type = "populate" });
                    break;

                case "client":
                    _operators = openFile("client", instance[2]); //instance[2] file to open
                    _client.sendPostInitAsync(new DIDAPostInitRequest { Data = _operators, Type = "client" + " " + instance[1] }); //instance[1] client_id
                    break;

                case "status\r":
                    _client.sendPostInitAsync(new DIDAPostInitRequest { Data = "data", Type = "status" });
                    break;
                
                case "listServer":
                    string server = instance[1].Split("\r")[0]; //server_id
                    if (_storageNodesMap.ContainsKey(server))
                        _client.sendPostInitAsync(new DIDAPostInitRequest { Data = server, Type = "listServer" });  
                    else
                        MessageBox.Show("Server not alive!");
                    break;

                case "listGlobal\r":
                    _client.sendPostInitAsync(new DIDAPostInitRequest { Data = "data", Type = "listGlobal" });
                    break;

                case "debug\r":
                    _debugMode = true;
                    break;

                case "crash":
                    string serverId = instance[1].Split("\r")[0];
                    //not async to have confirmation for the removal of the node crashed
                    var reply = _storageNodesMap[serverId].crashServerAsync(new DIDACrashRequest { ServerId = serverId });
                    //if (reply.Ack.Equals("ack"))
                    _storageNodesMap.Remove(serverId);
                    _client.sendPostInitAsync(new DIDAPostInitRequest { Data = serverId, Type = "crash" });
                    break;

                case "wait":
                    int timeToSleep = Int32.Parse(instance[1].Split("\r")[0]);
                    MessageBox.Show(timeToSleep.ToString());
                    System.Threading.Thread.Sleep(timeToSleep);
                    break;
            }
        }

        private void processCreationService(string fileName, string args, string type)
        {
            //  verify if already exists -> not -> create new client add to dict
            //  send fileName and args to PCS
            string[] toParse = args.Split(" ");
            string host = "";
            if (type.Equals("scheduler"))
                host = toParse[0].Split(":")[1][2..];

            if (type.Equals("worker") || type.Equals("storage"))
                host = toParse[1].Split(":")[1][2..];

            if (!_usedClientsMap.Keys.Contains(host))
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + host + ":" + 10000);
                _processClient = new DIDAProcessCreationService.DIDAProcessCreationServiceClient(channel);
                _usedClientsMap.Add(host, _processClient);                    
            }

            if (type.Equals("storage"))
                _storageNodesMap.Add(toParse[0], _processClient);

            _usedClientsMap[host].sendProcessAsync(new DIDAProcessSendRequest { FileName = fileName, Args = args });
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void startFunc()
        {
            _client.sendFile(new DIDAFileSendRequest { Workers = _workers, StorageNodes = _storageNodes});
            if (_debugMode)
                _client.sendPostInit(new DIDAPostInitRequest { Data = _urlPM, Type = "debug" });
        }

        public string openFile(string type, string fileName)
        {
            string ops = "";
            string currWorkingDir = Directory.GetCurrentDirectory(); 
            string path = Path.GetFullPath(Path.Combine(currWorkingDir, @"..\..\..\..\scripts\", fileName)); //please load the scripts to a specific folder

            string[] paths = path.Split("\r");

            foreach (string line in System.IO.File.ReadLines(paths[0]))
            {
                if (type.Equals("client"))
                {
                    string[] handleOps = line.Split(" ");
                    ops += handleOps[1] + " " + handleOps[2] + ";";
                } 

                if (type.Equals("populate"))
                {
                    string[] handleOps = line.Split(",");
                    ops += handleOps[0] + " " + handleOps[1] + ";";
                }
            }

            return ops;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            foreach (string s in _commands)
            {
                string[] tempSplit = s.Split(" ");
                if (isInit(_previousCommand) && !isInit(tempSplit[0]))
                    _sentInits = true;

                if (_sentInits)
                {
                    startFunc();
                    _sentInits = false;
                }

                commandParser(s);
                _previousCommand = tempSplit[0];
            }          
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        public void addtoDebug(string text)
        {
            textBox2.AppendText(text + Environment.NewLine);
        }
    }
    public class DebugService : DIDADebugService.DIDADebugServiceBase
    {
        private Form1 _form;
        public DebugService()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        }

        public override Task<DIDASendDebugReply> sendDebug(DIDASendDebugRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendDebugImpl(request));
        }

        public DIDASendDebugReply sendDebugImpl(DIDASendDebugRequest request)
        {
            _form.addtoDebug(request.Data);
            return new DIDASendDebugReply { Ack = "ack" };
        }

        public void addForm(Form1 form)
        {
            _form = form;
        }
    }
}
