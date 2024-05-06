using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Vorbis;
using NAudio.Wave;
using Un4seen.Bass;

namespace Musicplayer
{
    public partial class Form1 : Form
    {
        //文件存储路径
        string[] files;
        //本地音乐列表
        List<string> LocalMusicLists = new List<string> { };
        //用作音频输出
        private WaveOutEvent outputDevice = null;
        //用作读取OGG格式实例
        private VorbisWaveReader vorbisReader = null;
        public Form1()
        {
            InitializeComponent();
        }

        //处理ogg情况
        private void OggDeal(string oggFilePath)
        {
            //释放之前的资源
            DisposeWave(); 
            try
            {
                //初始化vorbis格式读取器和输出设备
                vorbisReader = new VorbisWaveReader(oggFilePath);
                outputDevice = new WaveOutEvent();
                //处理播放停止事件
                outputDevice.PlaybackStopped += OnPlaybackStopped; 
                outputDevice.Init(vorbisReader);
                outputDevice.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"播放音频文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //发生错误时，释放资源
                DisposeWave(); // 
            }
        }

        // 播放停止事件处理器
        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            // 运行在UI线程上
            this.Invoke((MethodInvoker)(() => {
                // 实际处理逻辑，例如更新UI或释放资源
                DisposeWave();

                if (args.Exception != null)
                {
                    MessageBox.Show($"播放时发生错误: {args.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }));
        }

        // 释放WaveOut和VorbisReader资源
        private void DisposeWave()
        {
            if (outputDevice != null)
            {
                if (outputDevice.PlaybackState != PlaybackState.Stopped)
                {
                    outputDevice.Stop();
                }
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (vorbisReader != null)
            {
                vorbisReader.Dispose();
                vorbisReader = null;
            }
        }
        //窗体关闭时释放资源
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            DisposeWave();
        }
        //自定义播放函数
        private void musicplay(string filename)
        {
            try
            {   //尝试播放新音乐之前确保先释放所有旧资源
                DisposeWave();
                //获取文件扩展名
                string extension = Path.GetExtension(filename);
                //判断文件后缀名
                if (extension == ".ogg")
                {
                    //处理ogg文件
                    OggDeal(filename);
                }
                else
                {
                    //直接播放
                    axWindowsMediaPlayer1.URL = filename;
                    axWindowsMediaPlayer1.Ctlcontrols.play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法播放音乐文件 {filename}: {ex.Message}", "播放错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        //选择按钮
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //过滤器
                openFileDialog1.Filter = "选择音频|*.mp3;*.flac;*.wav;*.ogg";
                //打开多选属性
                openFileDialog1.Multiselect = true; 
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //清除上一次的结果
                    listBox1.Items.Clear();
                    //取多个文件
                    files = openFileDialog1.FileNames;
                    foreach (string x in files)
                    {
                        //添加至listbox
                        listBox1.Items.Add(x); 
                        //添加到播放列表
                        LocalMusicLists.Add(x);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载音频文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        //列表框处理函数
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LocalMusicLists.Count>0)
            { 
                axWindowsMediaPlayer1.URL=LocalMusicLists[listBox1.SelectedIndex];//从选定的歌曲开始放
                musicplay(axWindowsMediaPlayer1.URL);//选中控制栏上的按钮
                label1.Text=Path.GetFileNameWithoutExtension(LocalMusicLists[listBox1.SelectedIndex]);//显示当前播放歌曲的歌名
            }
        }

        //停止播放按钮
        private void button2_Click(object sender, EventArgs e)
        {
            //用Windows Media Player控件播放的情况
            if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                axWindowsMediaPlayer1.Ctlcontrols.pause();
            }
            //用NAudio库播放的OGG文件的情况
            else if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();
            }
        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {

        }

        //下一首按钮
        private void button3_Click(object sender, EventArgs e)
        {
            //目的一：播放下一曲
            //目的二：如果到达末尾，则调到第一首播放
            try
            {
                // 检查列表是否有音乐
                if (LocalMusicLists.Count > 0)
                {
                    //如果没有选中的项，默认从第一首开始，否则选中下一首
                    int currentIndex = listBox1.SelectedIndex;
                    currentIndex = currentIndex == -1 ? 0 : currentIndex + 1; 
                    //到达最后一首歌曲时，循环到第一首
                    if (currentIndex >= LocalMusicLists.Count)
                    {
                        currentIndex = 0;
                    }
                    string nextSong = LocalMusicLists[currentIndex];
                    musicplay(nextSong);
                    label1.Text = Path.GetFileNameWithoutExtension(nextSong);
                    listBox1.SelectedIndex = currentIndex;
                }
                else
                {
                    MessageBox.Show("播放列表中没有歌曲。", "播放信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换歌曲时出错: {ex.Message}", "播放错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        //音量调节功能
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //设置音量调节方式
            axWindowsMediaPlayer1.settings.volume=trackBar1.Value;
        }
        
        //上一首按钮
        private void button1_Click_1(object sender, EventArgs e)
        {
            //目的一：播放上一曲
            //目的二：如果在第一首，则跳到最后一首播放
            try
            {
                //检查列表是否有音乐
                if (LocalMusicLists.Count > 0)
                {
                    //获取当前选中的项索引
                    int currentIndex = listBox1.SelectedIndex;
                    //如果没有选中的项，默认从最后一首开始，否则选中上一首
                    currentIndex = currentIndex == -1 ? LocalMusicLists.Count - 1 : currentIndex - 1;
                    //到达第一首歌曲时，跳到最后一首
                    if (currentIndex < 0)
                    {
                        currentIndex = LocalMusicLists.Count - 1;
                    }
                    string previousSong = LocalMusicLists[currentIndex];
                    musicplay(previousSong);
                    label1.Text = Path.GetFileNameWithoutExtension(previousSong);
                    listBox1.SelectedIndex = currentIndex;
                }
                else
                {
                    MessageBox.Show("播放列表中没有歌曲。", "播放信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换歌曲时出错: {ex.Message}", "播放错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
