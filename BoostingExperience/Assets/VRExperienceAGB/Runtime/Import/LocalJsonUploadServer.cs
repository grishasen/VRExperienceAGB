using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VRExperienceAGB.Import
{
    /// <summary>Explicitly started, token-protected LAN upload session. One bounded request at a time.</summary>
    public sealed class LocalJsonUploadServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Thread worker;
        private readonly string cache, token, screenshots;
        private readonly Func<string,string,bool,bool> accept;
        private readonly Func<string> status;
        private volatile bool stopped;
        public int Port { get; }
        public string Token => token;
        public string Address { get; }
        public LocalJsonUploadServer(string cache,Func<string,string,bool,bool> accept,Func<string> status,string screenshots=null)
        {
            this.cache=cache;this.accept=accept;this.status=status;this.screenshots=screenshots;Directory.CreateDirectory(cache);
            var random=new byte[4];using(var rng=System.Security.Cryptography.RandomNumberGenerator.Create())rng.GetBytes(random);
            token=(BitConverter.ToUInt32(random,0)%100000000).ToString("D8");listener=new TcpListener(IPAddress.Any,0);listener.Start(2);
            Port=((IPEndPoint)listener.LocalEndpoint).Port;
            string host=NetworkInterface.GetAllNetworkInterfaces().OrderBy(n=>n.Name=="wlan0"?0:1)
                .SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address)
                .FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))?.ToString()??"127.0.0.1";
            Address="http://"+host+":"+Port+"/";
            worker=new Thread(Run){IsBackground=true,Name="VR JSON upload"};worker.Start();
        }
        private void Run()
        {
            while(!stopped)try {
                using(var client=listener.AcceptTcpClient()) {
                    client.ReceiveTimeout=10000;client.SendTimeout=10000;
                    using(var stream=client.GetStream())try{Handle(stream);}catch(Exception){try{Reply(stream,400,"Invalid upload request.");}catch(Exception){}}
                }
            }catch(SocketException){if(stopped)return;}catch(ObjectDisposedException){return;}
        }
        private static string Line(Stream stream,ref int total)
        {
            var bytes=new MemoryStream();int b;
            while((b=stream.ReadByte())!=-1){if(++total>16384)throw new InvalidDataException();if(b==10)break;if(b!=13)bytes.WriteByte((byte)b);}
            if(b==-1)throw new EndOfStreamException();return Encoding.ASCII.GetString(bytes.ToArray());
        }
        private void Handle(NetworkStream stream)
        {
            int total=0;string[] first=Line(stream,ref total).Split(' ');if(first.Length!=3)throw new InvalidDataException();
            var uri=new Uri("http://localhost"+first[1]);
            var query=uri.Query.TrimStart('?').Split('&').Select(p=>p.Split(new[]{'='},2)).Where(p=>p.Length==2).ToDictionary(p=>p[0],p=>Uri.UnescapeDataString(p[1]));
            long length=0;bool chunked=false;string line;
            while((line=Line(stream,ref total)).Length>0) {
                int colon=line.IndexOf(':');if(colon<1)throw new InvalidDataException();string key=line.Substring(0,colon),value=line.Substring(colon+1).Trim();
                if(key.Equals("Content-Length",StringComparison.OrdinalIgnoreCase))length=long.Parse(value);
                if(key.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase))chunked=true;
            }
            if(!query.TryGetValue("token",out string supplied) || supplied!=token){
                Thread.Sleep(200);
                Reply(stream,first[0]=="GET" && uri.AbsolutePath=="/"?200:403,"<!doctype html><html lang='en'><meta name='viewport' content='width=device-width'><title>Headset import</title><body style='font:20px system-ui;padding:40px'><h1>Connect to your headset</h1><form><label>Enter the 8-digit code shown in VR<br><input name='token' inputmode='numeric' pattern='[0-9]{8}' required maxlength='8' style='font:inherit;margin:16px 0'></label><br><button style='font:inherit'>Connect</button></form></body></html>","text/html");return;
            }
            if(first[0]=="GET" && uri.AbsolutePath=="/"){Reply(stream,200,Page(),"text/html");return;}
            if(first[0]=="GET" && uri.AbsolutePath=="/status"){Reply(stream,200,status());return;}
            if(first[0]=="GET" && uri.AbsolutePath=="/screenshots") {
                var files=screenshots!=null && Directory.Exists(screenshots)?Directory.GetFiles(screenshots,"forest-*.png").OrderByDescending(Path.GetFileName).Take(100):Enumerable.Empty<string>();
                string links=string.Join("",files.Select(f=>"<p><a download href='/screenshot?token="+token+"&amp;name="+Uri.EscapeDataString(Path.GetFileName(f))+"'>"+WebUtility.HtmlEncode(Path.GetFileName(f))+"</a></p>"));
                Reply(stream,200,"<!doctype html><html lang='en'><meta name='viewport' content='width=device-width'><title>Screenshots</title><body style='font:20px system-ui;padding:30px'><h1>Saved screenshots</h1><p>Press X on the left controller to capture the current view. Refresh this page after taking a screenshot.</p>"+(links.Length==0?"<p>No screenshots yet.</p>":links)+"</body></html>","text/html");return;
            }
            if(first[0]=="GET" && uri.AbsolutePath=="/screenshot") {
                if(screenshots==null || !query.TryGetValue("name",out string name) || name!=Path.GetFileName(name) || name.Contains("\\") || !name.StartsWith("forest-",StringComparison.Ordinal) || !name.EndsWith(".png",StringComparison.Ordinal)) {Reply(stream,404,"Not found.");return;}
                string path=Path.Combine(screenshots,name);
                if(!File.Exists(path)){Reply(stream,404,"Not found.");return;}
                using(var file=File.OpenRead(path)) {
                    if(file.Length>64*1024*1024){Reply(stream,413,"Screenshot exceeds download limit.");return;}
                    byte[] header=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nContent-Disposition: attachment; filename=\"screenshot.png\"\r\nContent-Length: "+file.Length+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");
                    stream.Write(header,0,header.Length);file.CopyTo(stream);
                }return;
            }
            if(first[0]!="POST" || uri.AbsolutePath!="/upload"){Reply(stream,404,"Not found.");return;}
            if(chunked || length<=0 || length>ModelFileLibrary.MaximumBytes){Reply(stream,413,"Choose a JSON file up to 32 MiB.");return;}
            if(!query.TryGetValue("kind",out string kind) || (kind!="model" && kind!="profile")){Reply(stream,400,"Choose model or profile.");return;}
            string temporary=Path.Combine(cache,"vr-upload-"+Guid.NewGuid().ToString("N")+".json");bool retained=false;
            try {
                using(var file=File.Create(temporary)) {
                    byte[] buffer=new byte[16384];long remaining=length;
                    while(remaining>0){int read=stream.Read(buffer,0,(int)Math.Min(buffer.Length,remaining));if(read==0)throw new EndOfStreamException();file.Write(buffer,0,read);remaining-=read;}
                }
                retained=!stopped && accept(temporary,query.TryGetValue("name",out string name)?name:"import.json",kind=="profile");
                Reply(stream,retained?202:409,retained?"Received. Validation is starting; watch the status below.":"The upload queue is full. Put on the headset to process received files, then retry.");
            }finally{if(!retained && File.Exists(temporary))File.Delete(temporary);}
        }
        private string Page() => @"<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width'><title>Model forest import</title><style>body{font:18px system-ui;max-width:680px;margin:50px auto;padding:20px;color:#182c36;background:#eef4f6}button,input,select{font:inherit;padding:12px;margin:8px 0}button{background:#195a70;color:white;border:0;border-radius:8px}pre{white-space:pre-wrap;background:white;padding:18px;border-radius:8px}small{display:block}</style><h1>Import into your headset</h1><p>Import a model first, then its profiles. Files stay on the headset in separate Models and Profiles folders. You can upload a model and its profiles in order, then put on the headset to finish importing.</p><select id='kind'><option value='model'>Model JSON</option><option value='profile'>Profile JSON</option></select><br><input type='file' id='file' accept='.json,application/json'><br><button id='send'>Upload JSON</button><p id='message'></p><p><a id='screenshots'>Download screenshots</a></p><h2>Headset status</h2><pre id='status'>Connected</pre><small>Maximum 32 MiB. Use the same Wi-Fi network. The temporary link expires when you stop receiving in the headset.</small><script>const token=new URLSearchParams(location.search).get('token');document.querySelector('#screenshots').href='/screenshots?token='+token;const send=document.querySelector('#send');send.onclick=async()=>{const f=document.querySelector('#file').files[0];if(!f)return;if(f.size>33554432){document.querySelector('#message').textContent='Maximum 32 MiB';return;}send.disabled=true;try{const r=await fetch('/upload?token='+token+'&kind='+document.querySelector('#kind').value+'&name='+encodeURIComponent(f.name),{method:'POST',body:f});document.querySelector('#message').textContent=await r.text();}catch(e){document.querySelector('#message').textContent='Connection lost. Reopen the link shown in the headset.';}finally{send.disabled=false;}};setInterval(async()=>{try{const r=await fetch('/status?token='+token);document.querySelector('#status').textContent=await r.text();}catch(e){document.querySelector('#status').textContent='Session closed or headset disconnected.';}},1500);</script></html>";
        private static void Reply(Stream stream,int code,string text,string type="text/plain")
        {
            byte[] body=Encoding.UTF8.GetBytes(text);
            byte[] header=Encoding.ASCII.GetBytes("HTTP/1.1 "+code+" Result\r\nContent-Type: "+type+"; charset=utf-8\r\nContent-Length: "+body.Length+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'\r\nConnection: close\r\n\r\n");
            stream.Write(header,0,header.Length);stream.Write(body,0,body.Length);
        }
        public void Dispose(){stopped=true;listener.Stop();}
    }
}
