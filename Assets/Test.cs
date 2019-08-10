using Net.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    //视频宽
    public int width = 1024;
    //视频高
    public int height = 768;
    public Texture2D texture;
    public Material mat;
    IntPtr libvlc_instance_t;
    IntPtr libvlc_media_player_t;

    private VideoLockCB _videoLockCB;
    private VideoUnlockCB _videoUnlockCB;
    private VideoDisplayCB _videoDisplayCB;

    private int _pixelBytes = 4;
    private int _pitch;
    private IntPtr _buff = IntPtr.Zero;
    bool ready = false;

    string snapShotpath;

    public VideoTV curVideoTV = VideoTV.CCTV6电影频道;
    private Dictionary<VideoTV, string> dicSrcPath = new Dictionary<VideoTV, string>();

    void InitVideoTV()
    {
        dicSrcPath.Add(VideoTV.河南卫视, "rtmp://58.200.131.2:1935/livetv/hntv");
        dicSrcPath.Add(VideoTV.湖南卫视, "rtmp://58.200.131.2:1935/livetv/hunantv");
        dicSrcPath.Add(VideoTV.CCTV6电影频道, "rtmp://58.200.131.2:1935/livetv/cctv6");
        dicSrcPath.Add(VideoTV.CCTV9记录, "rtmp://58.200.131.2:1935/livetv/cctv9");
        dicSrcPath.Add(VideoTV.CCTV2财经, "rtmp://58.200.131.2:1935/livetv/cctv2");
        dicSrcPath.Add(VideoTV.CCTV13新闻, "rtmp://58.200.131.2:1935/livetv/cctv13");
    }
    

    void Start()
    {
        InitVideoTV();

        Application.targetFrameRate = 30;
        Loom.Initialize();
        snapShotpath = "file:///" + Application.streamingAssetsPath;

        _videoLockCB += VideoLockCallBack;

        _videoUnlockCB += VideoUnlockCallBack;

        _videoDisplayCB += VideoDiplayCallBack;

        libvlc_instance_t = MediaPlayer.Create_Media_Instance();

        libvlc_media_player_t = MediaPlayer.Create_MediaPlayer(libvlc_instance_t);


        string videoPath = dicSrcPath[curVideoTV]; 

        //本地视频地址
        //string videoPath = "file:///" + Application.streamingAssetsPath + "/test.mp4";
        bool state = MediaPlayer.SetLocation(libvlc_instance_t, libvlc_media_player_t, videoPath);

        Debug.Log("state:" + state);
        width = MediaPlayer.GetMediaWidth(libvlc_media_player_t);
        Debug.Log("width: " + width);
        height = MediaPlayer.GetMediaHeight(libvlc_media_player_t);
        Debug.Log("height: " + height);
        //网络地址不晓得怎么拿到视频宽高
        if (width == 0 && height == 0)
        {
            width = 1024;
            height = 576;
        }
        _pitch = width * _pixelBytes;
        _buff = Marshal.AllocHGlobal(height * _pitch);
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        mat.mainTexture = texture;

        MediaPlayer.SetCallbacks(libvlc_media_player_t, _videoLockCB, _videoUnlockCB, _videoDisplayCB, IntPtr.Zero);
        MediaPlayer.SetFormart(libvlc_media_player_t, "ARGB", width, height, _pitch);

        ready = MediaPlayer.MediaPlayer_Play(libvlc_media_player_t);
        Debug.Log("ready:" + ready);
        string[] arguments = { "--avcodec-hw=any",
            //"--spect-show-original",
            // "--avcodec-threads=124"
        };
        //MediaPlayer.AddOption(libvlc_media_player_t, arguments);
        Debug.Log(MediaPlayer.MediaPlayer_IsPlaying(libvlc_media_player_t));
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 100), "Take"))
        {
            Debug.Log("snapShotpath:" + snapShotpath);
            Debug.Log("@snapShotpath:" + @snapShotpath);
            //vlc截图未解决 用Unity保存帧图，画面是上下反转左右反转的
            //Debug.Log(MediaPlayer.TakeSnapShot(libvlc_media_player_t, snapShotpath, "testa.jpg", width, height));
            byte[] bs = texture.EncodeToJPG();
            File.WriteAllBytes(Application.streamingAssetsPath + "/test.jpg", bs);
        }
    }

    private IntPtr VideoLockCallBack(IntPtr opaque, IntPtr planes)
    {
        Lock();
        Marshal.WriteIntPtr(planes, 0, _buff);
        Loom.QueueOnMainThread(() =>
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            texture.LoadRawTextureData(_buff, _buff.ToInt32());
            texture.Apply();

            //stopwatch.Stop();
            //TimeSpan timespan = stopwatch.Elapsed;
            //Debug.Log(timespan.TotalMilliseconds);
        });
        return IntPtr.Zero;
    }

    private void VideoDiplayCallBack(IntPtr opaque, IntPtr picture)
    {

    }

    private void VideoUnlockCallBack(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        Unlock();
    }

    bool obj = false;
    private void Lock()
    {
        obj = true;
    }
    private void Unlock()
    {
        obj = false;
    }
    private bool Islock()
    {
        return obj;
    }

    private void OnDestroy()
    {

    }

    private void OnApplicationFocus(bool focus)
    {

    }

    private void OnApplicationQuit()
    {
        try
        {
            if (MediaPlayer.MediaPlayer_IsPlaying(libvlc_media_player_t))
            {
                MediaPlayer.MediaPlayer_Stop(libvlc_media_player_t);
            }

            MediaPlayer.Release_MediaPlayer(libvlc_media_player_t);

            MediaPlayer.Release_Media_Instance(libvlc_instance_t);

            GC.Collect();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
}


public enum VideoTV
{
    CCTV6电影频道,
    河南卫视,
    湖南卫视,
    CCTV9记录,
    CCTV2财经,
    CCTV13新闻
}