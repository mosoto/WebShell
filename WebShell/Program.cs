using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebShell
{
    public class Program
    {
        public static void Main(string[] args)
        {
            String port = Environment.GetEnvironmentVariable("VCAP_APP_PORT");

            if (String.IsNullOrEmpty(port)) 
                throw new ArgumentException("Port not set");

            string listenerAddress = "http://*:" + port + "/";
            Console.WriteLine("Listening on " + listenerAddress);

            var server = new PowerShellServer();
            server.Start(listenerAddress);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
