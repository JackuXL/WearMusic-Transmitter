using System;
using System.Windows;
using System.IO;
using System.Collections;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;

namespace WearMusicTransmitter
{
    public partial class MainWindow : Window
    {
        bool NoDevice = false;
        public static Snackbar Snackbar = new();
        public MainWindow()
        {
            InitializeComponent();
            cmb_devices.ItemsSource = RefreshDevice();
            cmb_devices.SelectedIndex = 0;
            Thread RefreshThread = new Thread(ThreadRefresh);
            RefreshThread.Start();
        }
        public void ThreadRefresh()
        {
            while (true)
            {
                String[] data = RefreshDevice();
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate {
                    cmb_devices.ItemsSource = data;
                    cmb_devices.SelectedIndex = 0;
                });
                Thread.Sleep(2000);
            }
        }
        public String[] RefreshDevice()
        {
            String result = RunCmd("adb devices").Replace("List of devices attached", "");
            String[] s = { "无设备" };
            ArrayList al;

            if (result.Contains("device"))
            {
                s = result.Replace("\r\n", "!").Split('!');
                for (int i = 0; i < s.Length; i++)
                {
                    if (!s[i].Contains("device"))
                    {
                        // 无效项
                        al = new ArrayList(s);
                        al.RemoveAt(i);
                        s = (string[])al.ToArray(typeof(string));
                    }
                    else
                    {
                        // 删除多余文本
                        s[i] = s[i].Replace("\tdevice", "");
                    }
                }
            }

            if (s[0].Equals("无设备"))
            {
                NoDevice = true;
            }
            else
            {
                // 删除多余空白项
                al = new ArrayList(s);
                al.RemoveAt(0);
                s = (string[])al.ToArray(typeof(string));
            }
            return s;
        }
        public string RunCmd(string command)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序
            p.StandardInput.WriteLine("cd adb");
            p.StandardInput.WriteLine(command + "&exit");
            p.StandardInput.AutoFlush = true;

            //获取cmd窗口的输出信息
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();//等待程序执行完成退出进程
            p.Close();

            output = output.Substring(output.LastIndexOf("exit"), output.Length - 1 - output.LastIndexOf("exit"));
            output = output.Replace("exit", "");
            return output;
        }
        private void btn_choose_Click(object sender, RoutedEventArgs e)
        {
            // 选择文件
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if((bool)rb_mp3.IsChecked) dialog.Filters.Add(new CommonFileDialogFilter("音频文件", "*.mp3,*.wav,*.flac,*.aac"));
            else if((bool)rb_lrc.IsChecked) dialog.Filters.Add(new CommonFileDialogFilter("歌词文件", "*.lrc"));
            else if((bool)rb_jpg.IsChecked) dialog.Filters.Add(new CommonFileDialogFilter("封面文件", "*.jpg,*.png"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tb_path.Text = dialog.FileName;
            }
        }

        private void btn_push_Click(object sender, RoutedEventArgs e)
        {
            int type = 3;
            if ((bool)rb_mp3.IsChecked) type = 0;
            else if ((bool)rb_lrc.IsChecked) type = 1;
            else if ((bool)rb_jpg.IsChecked) type = 2;
            String[] TypeStr = { ".mp3", ".lrc", ".jpg" };

            // 推送文件
            if (NoDevice)
            {
                Task.Factory.StartNew(() => Thread.Sleep(0)).ContinueWith(t =>
                {
                    MainSnackbar.MessageQueue?.Enqueue("无设备");
                }, TaskScheduler.FromCurrentSynchronizationContext());

            }
            else if (!File.Exists(tb_path.Text))
            {
                Task.Factory.StartNew(() => Thread.Sleep(0)).ContinueWith(t =>
                {
                    MainSnackbar.MessageQueue?.Enqueue("文件不存在");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (!Path.GetExtension(tb_path.Text).Equals(TypeStr[type]))
            {
                Task.Factory.StartNew(() => Thread.Sleep(0)).ContinueWith(t =>
                {
                    MainSnackbar.MessageQueue?.Enqueue("后缀名不匹配");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                bool flag = true;
                switch (type)
                {
                    case 0:
                        RunCmd("adb -s " + cmb_devices.SelectedValue + " push " + tb_path.Text + " /storage/emulated/0/Android/data/cn.wearbbs.music/files/download/music/");
                        break;
                    case 1:
                        RunCmd("adb -s " + cmb_devices.SelectedValue + " push " + tb_path.Text + " /storage/emulated/0/Android/data/cn.wearbbs.music/files/download/lrc/");
                        break;
                    case 2:
                        RunCmd("adb -s " + cmb_devices.SelectedValue + " push " + tb_path.Text + " /storage/emulated/0/Android/data/cn.wearbbs.music/files/download/cover/");
                        break;
                    default:
                        Task.Factory.StartNew(() => Thread.Sleep(0)).ContinueWith(t =>
                        {
                            //note you can use the message queue from any thread, but just for the demo here we 
                            //need to get the message queue from the snackbar, so need to be on the dispatcher
                            MainSnackbar.MessageQueue?.Enqueue("未知错误");
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        flag = false;
                        break;

                }
                if (flag)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(0)).ContinueWith(t =>
                    {
                        //note you can use the message queue from any thread, but just for the demo here we 
                        //need to get the message queue from the snackbar, so need to be on the dispatcher
                        MainSnackbar.MessageQueue?.Enqueue("命令已提交");
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
    }
}
