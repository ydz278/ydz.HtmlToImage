using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;

namespace ydz.HtmlToImageTool
{
    class Program
    {
        static void Main(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
            var url = args[0];//本地html文件地址或者html的url地址
            var destPath = args[1];//E:\code\1 文件夹，
            var imageName = args[2];//图片名称
            AsyncContext.Run(async delegate
            {

                try
                {
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    var cefSharplog = Path.Combine(Environment.CurrentDirectory, "CefSharp\\Cache");
                    WriteLog(cefSharplog, "");
                    var settings = new CefSettings()
                    {
                        //默认情况下，CefSharp将使用内存缓存，您需要指定缓存文件夹来持久化数据
                        CachePath = cefSharplog
                    };

                    //执行依赖项检查以确保所有相关资源都在我们的输出目录中。
                    var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                    if (!success)
                    {
                        WriteLog("Unable to initialize CEF, check the log file", "");
                        throw new Exception("Unable to initialize CEF, check the log file.");
                    }

                    // 创建ChromiumWebBrowser实例
                    using (var browser = new ChromiumWebBrowser(url))
                    {
                        var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                        if (!initialLoadResponse.Success)
                        {
                            throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                        }
                        //等待浏览器渲染
                        await Task.Delay(500);
                        // 截屏
                        var bitmapAsByteArray = await browser.CaptureScreenshotAsync();
                        if (!Directory.Exists(destPath))
                        {
                            Directory.CreateDirectory(destPath);
                        }
                        // 要保存屏幕截图的文件路径
                        var screenshotPath = Path.Combine(destPath, imageName);

                        File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                        //// 告诉Windows启动保存的图像
                        //Process.Start(new ProcessStartInfo(screenshotPath)
                        //{
                        //    // .net core中默认情况下UseShellExecute为false
                        //    UseShellExecute = true
                        //});
                    }
                    WriteLog("图片生成成功", Path.Combine(destPath, imageName));
                    Cef.Shutdown();
                }
                catch (Exception e)
                {
                    WriteLog(e.Message, "");
                    Cef.Shutdown();
                }
            });
        }
        private static void WriteLog(string msg, string path)
        {
            var log = Path.Combine(Environment.CurrentDirectory, "log.txt");
            using (var strea = new StreamWriter(log, true, Encoding.UTF8))
            {
                strea.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}【{msg}】，【{path}】");
            }
        }
    }
    public static class AsyncContext
    {
        public static void Run(Func<Task> func)
        {
            var prevCtx = SynchronizationContext.Current;

            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();

                SynchronizationContext.SetSynchronizationContext(syncCtx);

                var t = func();

                t.ContinueWith(delegate
                {
                    syncCtx.Complete();
                }, TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();

                t.GetAwaiter().GetResult();
            }
            finally
            {

                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }
    }
    public sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void RunOnCurrentThread()
        {
            while (queue.TryTake(out var workItem, Timeout.Infinite))
            {
                workItem.Key(workItem.Value);
            }
        }

        public void Complete()
        {
            queue.CompleteAdding();
        }
    }
}
