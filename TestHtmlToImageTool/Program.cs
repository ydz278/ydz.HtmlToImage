using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHtmlToImageTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = @"C:\Users\ydz\Desktop\新建文件夹\index.html";
            string destPath = @"D:\IAMGE\1";
            string imageName = "ydz.png";
            //截图工具路径
            string toolExeFilePath =
                @"D:\工具库\htlm转图片工具\ydz.HtmlToImage\ydz.HtmlToImage\bin\Debug\ydz.HtmlToImageTool.exe";
            HtmlToImage(url,destPath,imageName,toolExeFilePath);
            Console.WriteLine($"截图成功,图片路径:{Path.Combine(destPath,imageName)}");
            Console.ReadKey();
        }
        public static void HtmlToImage(string url, string destDirPath,string imagePath, string toolExeFilePath)
        {
            var direc = Path.GetDirectoryName(toolExeFilePath.Replace(Path.GetFileName(toolExeFilePath),
                ""));
            Process process = Process.Start(new ProcessStartInfo(toolExeFilePath, $"{url} {destDirPath} {imagePath}")
            {
                WindowStyle = ProcessWindowStyle.Maximized,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = direc,
                RedirectStandardError = true
            });
            process.WaitForExit();
        }
    }
}
