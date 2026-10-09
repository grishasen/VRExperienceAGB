using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    public class LocalJsonUploadServerTests
    {
        private string cache,received;
        private LocalJsonUploadServer server;
        [SetUp] public void Open(){cache=Path.Combine(Path.GetTempPath(),"vr-http-"+Guid.NewGuid().ToString("N"));server=new LocalJsonUploadServer(cache,(p,n,b)=>{received=p;return true;},()=>"Ready",Path.Combine(cache,"Screenshots"));}
        [TearDown] public void Close(){server.Dispose();Directory.Delete(cache,true);}
        private string Request(string target,string body="{}",string length=null,string method="POST")
        {
            using(var client=new TcpClient("127.0.0.1",server.Port)) {
                client.ReceiveTimeout=3000;using(var stream=client.GetStream()) {
                    var bytes=Encoding.UTF8.GetBytes(method+" "+target+" HTTP/1.1\r\nHost: localhost\r\nContent-Length: "+(length??Encoding.UTF8.GetByteCount(body).ToString())+"\r\n\r\n"+body);
                    stream.Write(bytes,0,bytes.Length);using(var reader=new StreamReader(stream))return reader.ReadToEnd();
                }
            }
        }
        [Test] public void UploadRequiresSessionCodeAndRetainsExactBytes()
        {
            Assert.That(Request("/upload?kind=model&token=wrong"),Does.Contain("403"));Assert.That(received,Is.Null);
            string json="{\"name\":\"Example\"}";
            Assert.That(Request("/upload?kind=model&token="+server.Token+"&name=sample.json",json),Does.Contain("202"));
            Assert.That(File.ReadAllText(received),Is.EqualTo(json));
        }
        [Test] public void OversizeAndInvalidKindDoNotCreateCachedFiles()
        {
            Assert.That(Request("/upload?kind=model&token="+server.Token,"",(ModelFileLibrary.MaximumBytes+1L).ToString()),Does.Contain("413"));
            Assert.That(Request("/upload?kind=other&token="+server.Token),Does.Contain("400"));
            Assert.That(Directory.GetFiles(cache),Is.Empty);
        }
        [Test] public void ScreenshotDownloadsRequireCodeAndCannotReadOtherFiles()
        {
            Directory.CreateDirectory(Path.Combine(cache,"Screenshots"));
            File.WriteAllText(Path.Combine(cache,"Screenshots","forest-example.png"),"PNG fixture bytes");
            File.WriteAllText(Path.Combine(cache,"private.json"),"private");
            Assert.That(Request("/screenshots",method:"GET"),Does.Contain("403"));
            Assert.That(Request("/screenshots?token="+server.Token,method:"GET"),Does.Contain("forest-example.png"));
            Assert.That(Request("/screenshot?token="+server.Token+"&name=forest-example.png",method:"GET"),Does.Contain("PNG fixture bytes"));
            foreach(string name in new[]{"..%2Fprivate.json","%2Fprivate.json","pending-forest-example.png","forest-..%5Cprivate.png"})
                Assert.That(Request("/screenshot?token="+server.Token+"&name="+name,method:"GET"),Does.Contain("404"));
        }
    }
}
