using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

internal sealed class UpdateRelease {
    internal Version Version;
    internal string Url;
}
internal static class Updates {
    internal const string ReleasesUrl="https://github.com/plashsma/localsave/releases";
    internal static bool Due(string lastCheck,DateTime now) {
        DateTime previous;
        return !DateTime.TryParse(lastCheck,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out previous)
            || previous.ToUniversalTime()>now || now-previous.ToUniversalTime()>=TimeSpan.FromHours(24);
    }
    internal static UpdateRelease Parse(string json,Version current,bool previews) {
        if(json==null || json.Length>524288) throw new InvalidDataException("Update response is too large.");
        JavaScriptSerializer serializer=new JavaScriptSerializer {MaxJsonLength=524288,RecursionLimit=32};
        object[] list=serializer.DeserializeObject(json) as object[];
        if(list==null) throw new InvalidDataException("Invalid release response.");
        UpdateRelease best=null;
        foreach(object item in list) {
            Dictionary<string,object> data=item as Dictionary<string,object>;
            if(data==null || !data.ContainsKey("draft") || !data.ContainsKey("prerelease") || !data.ContainsKey("tag_name") || !data.ContainsKey("html_url")) continue;
            if(!(data["draft"] is bool) || !(data["prerelease"] is bool) || (bool)data["draft"] || (!previews && (bool)data["prerelease"])) continue;
            string tag=data["tag_name"] as string,url=data["html_url"] as string; Version version;
            if(String.IsNullOrEmpty(tag) || tag.Length>40 || !tag.StartsWith("v",StringComparison.Ordinal) || !Version.TryParse(tag.Substring(1),out version)) continue;
            // Never open an arbitrary URL supplied by the release response.
            if(url!="https://github.com/plashsma/localsave/releases/tag/"+tag) continue;
            Version normalized=new Version(version.Major,version.Minor,Math.Max(0,version.Build));
            Version installed=new Version(current.Major,current.Minor,Math.Max(0,current.Build));
            if(normalized<=installed || (best!=null && normalized<=best.Version)) continue;
            best=new UpdateRelease {Version=normalized,Url=url};
        }
        return best;
    }
    internal static UpdateRelease Check(Version current,bool previews) {
        // Older compiler target defaults can negotiate obsolete protocols even on Framework 4.8.
        ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        HttpWebRequest request=(HttpWebRequest)WebRequest.Create("https://api.github.com/repos/plashsma/localsave/releases?per_page=20");
        request.UserAgent="LocalSave-update-check";request.Accept="application/vnd.github+json";
        request.Timeout=10000;request.ReadWriteTimeout=10000;request.AllowAutoRedirect=false;
        using(HttpWebResponse response=(HttpWebResponse)request.GetResponse()) {
            if(response.StatusCode!=HttpStatusCode.OK || response.ContentLength>524288) throw new InvalidDataException("Invalid update response.");
            using(Stream stream=response.GetResponseStream()) using(MemoryStream buffer=new MemoryStream()) {
                System.Diagnostics.Stopwatch deadline=System.Diagnostics.Stopwatch.StartNew();
                byte[] block=new byte[4096];int count;
                while((count=stream.Read(block,0,block.Length))>0) {
                    if(deadline.Elapsed>TimeSpan.FromSeconds(20)) throw new TimeoutException("Update check timed out.");
                    if(buffer.Length+count>524288) throw new InvalidDataException("Update response is too large.");
                    buffer.Write(block,0,count);
                }
                return Parse(Encoding.UTF8.GetString(buffer.ToArray()),current,previews);
            }
        }
    }
}
