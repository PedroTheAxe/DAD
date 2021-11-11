using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMasterUI
{

    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*int port = 10001;
            Console.WriteLine("PORT " + port);

            Server server = new Server
            {
                Services = { DIDAPuppetMasterService.BindService(new PuppetMasterService()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            Console.WriteLine("SERVER: " + server);
            server.Start();*/

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            //server.ShutdownAsync().Wait();
            Console.ReadKey();
        }
    }
}
