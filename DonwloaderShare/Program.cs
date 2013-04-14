using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Cache;
using System.Reflection;
using System.Diagnostics;

namespace DonwloaderShare
{
    class Program
    {

        enum Resp
        {
            Erro, Baixando, Aguardar, Ok
        }

        static void Main(string[] args)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
            Console.Clear();
            Console.WriteLine("RapidShare Downloader Share!");
            Console.WriteLine("by marciusbezerra@gmail.com");
            if (args.Length == 0)
            {
                Console.WriteLine("Use DonwloaderShare list.txt [NomeDaConexao - caso vc queira desconectar para trocar o IP]");
                Console.WriteLine("Precione alguma tecla sair.");
                Console.ReadKey();
                return;
            }
            StreamReader sr = new StreamReader(args[0]);
            try
            {
                string link = null;
                while ((link = sr.ReadLine()) != null)
                {
                    if (link.Trim() == string.Empty) continue;
                    bool down = false;
                    int tent = 1;
                    while (tent < 10 && !down)
                    {
                        Console.WriteLine();
                        Console.WriteLine("-------------------------------------------------------------------");
                        Console.WriteLine("Tentativa: {0}", tent);
                        down = DownloadRapidShareFile(path,  link, args.Length == 2 ? args[1] : null);
                        //if (!sr.EndOfStream)
                        //{
                        //    Console.WriteLine("Aguardando 2 minutos...");
                        //    Thread.Sleep(120000);
                        //}
                        tent++;
                    }
                }
            }
            finally
            {
                sr.Close();
            }
            Console.WriteLine("*** fim ***");
            Console.ReadKey();
        }

        private static bool DownloadRapidShareFile(string path, string url, string conexao)
        {
            Console.WriteLine("->{0}:", url);
            url = RequestFileGet(url);
            if (url.Trim() == string.Empty)
            {
                Console.WriteLine("Arquivo não mais existente!");
                return true;
            }
            Resp r = RequestFilePost(ref url);
            switch (r)
            {
                case Resp.Erro:
                    Console.WriteLine("Deu erro, retentando em 2 minutos...");
                    Thread.Sleep(120000);
                    DownloadRapidShareFile(path, url, conexao);
                    break;
                case Resp.Baixando:
                    Console.WriteLine("Aguardando arquivo baixando, retentando em 2 minutos...");
                    Thread.Sleep(120000);
                    break;
                case Resp.Aguardar:
                    Console.WriteLine("Pediu para aguardar...");
                    Console.WriteLine("Aguardando 2 minutos...");
                    Thread.Sleep(120000);
                    Console.WriteLine("Tentando baixar...");
                    bool sucesso = BaixarArquivo(path, url);
                    if (!sucesso && !String.IsNullOrEmpty(conexao))
                    {
                        Desconectar(conexao);
                        Thread.Sleep(20000);
                        Conectar(conexao);
                        return BaixarArquivo(path, url);
                    }
                    return sucesso;
                case Resp.Ok:
                    Console.WriteLine("Tudo Ok...");
                    Console.WriteLine("Tentando baixar...");
                    return BaixarArquivo(path, url);
            }
            return false;
        }

        private static bool BaixarArquivo(string path, string url)
        {
            try
            {
                string postData = "mirror=on&x=42&y=49";
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] buffer = encoding.GetBytes(postData);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "POST";
                myRequest.Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-ms-application, application/vnd.ms-xpsdocument, application/xaml+xml, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, application/x-silverlight, application/x-silverlight-2-b1, */*";
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506; InfoPath.2; .NET CLR 3.5.21022)";
                myRequest.KeepAlive = true;
                myRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                myRequest.ContentLength = buffer.Length;
                Stream newStream = myRequest.GetRequestStream();
                try
                {
                    newStream.Write(buffer, 0, buffer.Length);
                }
                finally
                {
                    newStream.Close();
                }
                myRequest.Timeout = 10000;
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                string file = Guid.NewGuid().ToString() + ".TMP";
                try
                {
                    Stream s = myResponse.GetResponseStream();
                    try
                    {
                        Regex r = new Regex("filename=(.*)$");
                        Match m = r.Match(myResponse.GetResponseHeader("Content-Disposition"));
                        if (!m.Success)
                        {
                            Console.WriteLine("Erro na tentativa de download...");
                            return false;
                        }
                        file = m.Groups[1].Value;
                        Console.WriteLine("Baixando {0}...", file);
                        FileStream fs = new FileStream(Path.Combine(path, file), FileMode.Create);
                        try
                        {

                            byte[] read = new byte[256];
                            int count = s.Read(read, 0, read.Length);
                            long progressP = 0;
                            long progressT = myResponse.ContentLength;
                            while (count > 0)
                            {
                                Console.SetCursorPosition(0, Console.CursorTop);
                                Console.Write("Baixando {0}%...", ((progressP += count) * 100) / progressT);
                                fs.Write(read, 0, count);
                                count = s.Read(read, 0, read.Length);
                            }
                            if (progressP != progressT)
                            {
                                Console.WriteLine("Erro ao baixar: a tamanho do arquivo não confere...");
                                return false;
                            }
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write("Baixado 100%................");
                            Console.WriteLine();
                        }
                        finally
                        {
                            fs.Close();
                        }
                    }
                    finally
                    {
                        s.Close();
                    }
                }
                finally
                {
                    myResponse.Close();
                }
                Console.WriteLine("Download {0} concluído!!", file);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao baixar: " + ex.Message);
                return false;
            }
        }

        private static void Conectar(string conexao)
        {
            Console.WriteLine("Reconectando...");
            DosCommand("rasdial.exe " + conexao);
        }

        private static void Desconectar(string conexao)
        {
            Console.WriteLine("Desconectando para trocar o ip...");
            DosCommand("rasdial.exe " + conexao + " /disconnect");
        }

        private static string RequestFileGet(string url)
        {
            HttpWebResponse myHttpWebResponse = null;
            Stream streamResponse = null;
            StreamReader streamRead = null;
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "GET";
                myRequest.Accept = "*/*";
                myRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506; InfoPath.2; .NET CLR 3.5.21022)";
                myRequest.KeepAlive = true;
                myHttpWebResponse = (HttpWebResponse)myRequest.GetResponse();
                streamResponse = myHttpWebResponse.GetResponseStream();
                streamRead = new StreamReader(streamResponse);
                string resposta = streamRead.ReadToEnd();
                Regex r = new Regex("<form id=\"ff\" action=\"(.*)\" method=\"post\">");
                Match m = r.Match(resposta);
                if (m.Success) return m.Groups[1].Value;
                r = new Regex("(.*)file could not be found(.*)");
                m = r.Match(resposta);
                if (m.Success) return string.Empty;
                return url;
            }
            catch
            {
                Console.WriteLine("Erro ao pegar a url inicial!");
                return url;
            }
            finally
            {
                if (streamRead != null) streamRead.Close();
                if (streamResponse != null) streamResponse.Close();
                if (myHttpWebResponse != null) myHttpWebResponse.Close();
            }
        }

        private static Resp RequestFilePost(ref string url)
        {
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                string postData = "dl.start=Free";
                byte[] buffer = encoding.GetBytes(postData);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "POST";
                myRequest.Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-ms-application, application/vnd.ms-xpsdocument, application/xaml+xml, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, application/x-silverlight, application/x-silverlight-2-b1, */*";
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.0.04506; InfoPath.2; .NET CLR 3.5.21022)";
                myRequest.KeepAlive = true;
                myRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                myRequest.ContentLength = buffer.Length;
                Stream newStream = myRequest.GetRequestStream();
                try
                {
                    newStream.Write(buffer, 0, buffer.Length);
                }
                finally
                {
                    newStream.Close();
                }
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myRequest.GetResponse();
                try
                {
                    Stream streamResponse = myHttpWebResponse.GetResponseStream();
                    try
                    {
                        StreamReader streamRead = new StreamReader(streamResponse);
                        try
                        {
                            string resposta = streamRead.ReadToEnd();
                            Regex r = new Regex("Your IP address (.*) is already downloading a file");
                            Match m = r.Match(resposta);
                            if (m.Success)
                                return Resp.Baixando;
                            else
                            {
                                r = new Regex("Please be patient");
                                m = r.Match(resposta);
                                if (m.Success)
                                {
                                    r = new Regex("<form name=\"dlf\" action=\"(.*)\" method=\"post\">");
                                    m = r.Match(resposta);
                                    if (m.Success) url = m.Groups[1].Value;
                                    return Resp.Aguardar;
                                }
                                else
                                {
                                    r = new Regex("<form name=\"dlf\" action=\"(.*)\" method=\"post\">");
                                    m = r.Match(resposta);
                                    if (m.Success)
                                    {
                                        url = m.Groups[1].Value;
                                        return Resp.Ok;
                                    }
                                    else
                                        return Resp.Erro;
                                }
                            }
                        }
                        finally
                        {
                            streamRead.Close();
                        }
                    }
                    finally
                    {
                        streamResponse.Close();
                    }
                }
                finally
                {
                    myHttpWebResponse.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao pagar o endereço de download: " + ex.Message);
                return Resp.Erro;
            }
        }

        static void DosCommand(string cmd)
        {
            ProcessStartInfo startinfo;
            Process process = null;
            string stdoutline;
            StreamReader stdoutreader;

            try
            {
                startinfo = new ProcessStartInfo();
                startinfo.FileName = "cmd.exe";
                startinfo.Arguments = "/C " + cmd;
                startinfo.UseShellExecute = false;
                startinfo.RedirectStandardOutput = true;
                startinfo.CreateNoWindow = true;
                process = Process.Start(startinfo);
                stdoutreader = process.StandardOutput;
                while ((stdoutline = stdoutreader.ReadLine()) != null)
                {
                    Console.WriteLine(stdoutline);
                }
                stdoutreader.Close();
                stdoutreader = null;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (process != null)
                {
                    // close process handle
                    process.Close();
                }

                //cleanup
                process = null;
                startinfo = null;
            }
        }
    }
}
