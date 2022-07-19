using System;
using ProtoBuf;
using System.IO;
using System.IO.Pipes;
using THBimEngine.Domain.Model;

namespace XbimXplorer
{
    public class PipeDemo
    {
        public void Demo()
        {
            WaitForConnection();
        }

        private static void WaitForConnection()
        {
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("THDB2Push_TestPipe", PipeDirection.In);
            Console.WriteLine("等待CAD Push...");
            pipeServer.WaitForConnection();
            Console.WriteLine("connect!");
            try
            {
                ThTCHProject Project;
                Project = Serializer.Deserialize<ThTCHProject>(pipeServer);
                Console.WriteLine("读取数据成功！");
                Console.WriteLine($"解析楼层。。共{Project.Site.Building.Storeys.Count}层");
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            pipeServer.Dispose();
            WaitForConnection();
        }
    }
}
