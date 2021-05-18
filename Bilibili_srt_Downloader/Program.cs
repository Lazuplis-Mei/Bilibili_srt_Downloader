using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;


namespace Bilibili_srt_Downloader
{

    class PageList
    {
        public int code;
        public string message;
        public List<PageInfo> data;
    }

    class PageInfo
    {
        public int cid;
        public string part;
    }

    public class SubtitlesInfo
    {
        public string id;
        public string subtitle_url;
    }

    public class Subtitle
    {
        public List<SubtitlesInfo> subtitles;
    }

    public class Data
    {
        public Subtitle subtitle;
    }

    public class Page
    {
        public int code;
        public string message;
        public Data data;
    }

    public class BodyItem
    {
        public double from;
        public double to;
        public string content;
    }

    public class SubtitleItem
    {
        public List<BodyItem> body;
    }

    class Program
    {
        static string HttpGetPage(string Url)
        {
            HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(Url);
            obj.Method = "GET";
            obj.ReadWriteTimeout = 10000;
            obj.Timeout = 3000;
            obj.ContentType = "text/html;charset=UTF-8";
            obj.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";
            HttpWebResponse httpWebResponse = (HttpWebResponse)obj.GetResponse();
            Stream stream = httpWebResponse.GetResponseStream();
            if (httpWebResponse.Headers["Content-Encoding"] != null)
            {
                string text = httpWebResponse.Headers["Content-Encoding"];
                if (text.ToLower().Contains("gzip"))
                {
                    stream = new GZipStream(stream, CompressionMode.Decompress);
                }
                else if (text.ToLower().Contains("deflate"))
                {
                    stream = new DeflateStream(stream, CompressionMode.Decompress);
                }
            }
            StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
            string result = streamReader.ReadToEnd();
            streamReader.Close();
            stream.Close();
            return result;
        }

        const string PageListUrl = "https://api.bilibili.com/x/player/pagelist?aid={0}";
        const string PageInfoUrl = "https://api.bilibili.com/x/player/v2?aid={0}&cid={1}";

        static void Main(string[] args)
        {
            Console.WriteLine("输入视频av号:");
            var aid = Console.ReadLine();
            if (int.TryParse(aid, out _))
            {
                var webPage = HttpGetPage(string.Format(PageListUrl, aid));
                var pageList = JsonConvert.DeserializeObject<PageList>(webPage);
                if (pageList.code == 0)
                {
                    Directory.CreateDirectory(aid);
                    Console.WriteLine("getting pages...");
                    foreach (var pageinfo in pageList.data)
                    {
                        var fileName = pageinfo.part;
                        foreach (var c in Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, ' ');
                        }
                        fileName = aid + "\\" + fileName;
                        Console.WriteLine($"detected file:{fileName}");
                        webPage = HttpGetPage(string.Format(PageInfoUrl, aid, pageinfo.cid));
                        var page = JsonConvert.DeserializeObject<Page>(webPage);
                        if (page.code == 0)
                        {
                            var subtitles = page.data.subtitle.subtitles;
                            if (subtitles.Count > 0)
                            {
                                var subtitleInfo = subtitles.First();
                                webPage = HttpGetPage("http:" + subtitleInfo.subtitle_url);
                                var subtitle = JsonConvert.DeserializeObject<SubtitleItem>(webPage);
                                using (var file = File.CreateText(fileName + ".srt"))
                                {
                                    int i = 1;

                                    foreach (var item in subtitle.body)
                                    {
                                        file.WriteLine(i);
                                        var starttime = TimeSpan.FromSeconds(item.from);
                                        var start = starttime.ToString();
                                        if (starttime.Milliseconds == 0)
                                        {
                                            start += ",000";
                                        }
                                        else
                                        {
                                            start = start.Substring(0, 12).Replace('.', ',');
                                        }
                                        var endtime = TimeSpan.FromSeconds(item.to);
                                        var end = endtime.ToString();
                                        if (endtime.Milliseconds == 0)
                                        {
                                            end += ",000";
                                        }
                                        else
                                        {
                                            end = end.Substring(0, 12).Replace('.', ',');
                                        }
                                        file.WriteLine($"{start} --> {end}");
                                        file.WriteLine(item.content);
                                        file.WriteLine();
                                        i++;
                                    }
                                }
                                Console.WriteLine($"written subtitle id:{subtitleInfo.id}");
                            }
                        }
                        else
                        {
                            Console.WriteLine(page.message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(pageList.message);
                }
            }
            else
            {
                Console.WriteLine("请输入纯数字");
            }
        }
    }
}
