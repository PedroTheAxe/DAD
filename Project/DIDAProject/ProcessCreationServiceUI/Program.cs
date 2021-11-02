using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProcessCreationServiceUI
{
    public class ProcessCreationService : DIDAProcessCreationService.DIDAProcessCreationServiceBase
    {
        private Dictionary<string, int> _storageProcessMap = new Dictionary<string, int>();

        public override Task<DIDAProcessSendReply> sendProcess (DIDAProcessSendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendProcessImpl(request));
        }

        public DIDAProcessSendReply sendProcessImpl(DIDAProcessSendRequest request)
        {

            //var tcs = new TaskCompletionSource<DIDAProcessSendReply>();

            try
            {
                string execName = request.FileName + ".exe";
                string directory = System.IO.Directory.GetParent(Environment.CurrentDirectory).ToString();
                Console.WriteLine(@directory + "\r\n");
                string applicationPath = Path.GetFullPath(Path.Combine(directory, @"..\..\..\", request.FileName, @"bin\Debug\netcoreapp3.1\", execName));
                Console.WriteLine(applicationPath + "\r\n");

                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = applicationPath;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.Arguments = request.Args;
                    //process.Exited += (sender, args) =>
                    //{
                    //    tcs.SetResult(process.ExitCode);
                    //    process.Dispose();
                    //};
                    process.Start();

                    if (request.FileName.Equals("DIDAStorageUI"))
                    {
                        string[] processArgs = request.Args.Split(" ");
                        string serverId = processArgs[0];
                        _storageProcessMap.Add(serverId, process.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return new DIDAProcessSendReply { Ack = "ack" };
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for pcs 
            
            string host = "localhost";
            Console.WriteLine(host);

            int port = 10000;
            Console.WriteLine(port);

            Server server = new Server
            {
                Services = { DIDAProcessCreationService.BindService(new ProcessCreationService()) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadLine();
            server.ShutdownAsync().Wait();
        }
    }
}
