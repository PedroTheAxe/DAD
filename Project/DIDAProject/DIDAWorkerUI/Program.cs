using Grpc.Core;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DIDAWorkerUI
{
    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {

        DIDASendRequest _request;
        public SchedulerService()
        {

        }

        public override Task<DIDASendReply> send(DIDASendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendImpl(request));
        }

        public DIDASendReply sendImpl(DIDASendRequest request)
        {
            Console.WriteLine(request);
            
            _request = request;
            string className = request.Request.Chain[request.Request.Next].Op.Classname;
            reflectionLoad(className, request);
            //dll Reflection comes here?

            DIDASendReply sendReply = new DIDASendReply
            {
                Ack = "ack"
            };

            return sendReply;
        }

        public void reflectionLoad(string className, DIDASendRequest request)
        {
            //TODO: check and maybe refactor
            string _dllNameTermination = ".dll";
            string _currWorkingDir = Directory.GetCurrentDirectory(); //maybe use Desktop or Downloads
            string savingPath = Path.GetFullPath(Path.Combine(_currWorkingDir, @"..\..\..\..\"));
            DIDAWorker.IDIDAOperator _objLoadedByReflection;

            Console.WriteLine("directory: " + savingPath);

            foreach (string filename in Directory.EnumerateFiles(savingPath))
            {
                Console.WriteLine("file in cwd: " + filename);
                if (filename.EndsWith(_dllNameTermination))
                {
                    Console.WriteLine(".ddl found");
                    Assembly _dll = Assembly.LoadFrom(filename);
                    Type[] _typeList = _dll.GetTypes();
                    foreach (Type type in _typeList)
                    {
                        Console.WriteLine("type contained in dll: " + type.Name);
                        if (type.Name == className)
                        {
                            Console.WriteLine("Found type to load dynamically: " + className);
                            _objLoadedByReflection = (DIDAWorker.IDIDAOperator)Activator.CreateInstance(type);
                            //foreach (MethodInfo method in type.GetMethods())
                            //{
                            //    Console.WriteLine("method from class " + className + ": " + method.Name);
                            //}
                            //_objLoadedByReflection.M("success!"); -- seria read, write or update...
                            
                            //DIDAWorker.DIDAMetaRecord meta = request.Request.Meta; //how to cast???
                            //_objLoadedByReflection.ProcessRecord(meta, request.Request.Input, "");
                        }
                    }
                }
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for scheduler 
            Console.WriteLine(args[1]);

            string[] decomposedArgs = args[1].Split(":");

            decomposedArgs[1] = decomposedArgs[1].Substring(2);
            string host = decomposedArgs[1];
            Console.WriteLine(host);

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new SchedulerService()) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();

            //CODE OF SEND TO OTHER WORKER (TODO GRPC, CHANNEL...)
            //request.chain[next].operator.ProcessRecord(DIDAMetaRecord, inputString,
            //previousOperatorOutput);
            //request.next = request.next + 1;
            //if (request.next < request.chainSize)
            //    forward request to(request.chain[next].host, request.chain[next].port);
        }
    }
}
