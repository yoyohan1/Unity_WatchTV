﻿using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.IO;

namespace Net.Media
{
    //定义替代变量
    using libvlc_media_t = IntPtr;
    using libvlc_media_player_t = IntPtr;
    using libvlc_instance_t = IntPtr;

    public class MediaPlayer
    {
        #region 全局变量
        //数组转换为指针
        internal struct PointerToArrayOfPointerHelper
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public IntPtr[] pointers;
        }

        internal enum libvlc_track_type_t
        {
            libvlc_track_unknown,
            libvlc_track_audio,
            libvlc_track_video,
            libvlc_track_text
        }

        internal struct libvlc_media_track_t
        {
            public uint i_codec;
            public uint i_original_fourcc;
            public int i_id;
            public libvlc_track_type_t i_type;
            public int i_profile;
            public int i_level;
        }

        /// <summary>
        /// vlc库启动参数配置
        /// </summary>
        private static string pluginPath = @UnityEngine.Application.streamingAssetsPath + "/plugins/";

        private static string plugin_arg = "--plugin-path=" + pluginPath;
        //用于播放节目时，转录节目
        private static string program_arg = "--sout=#duplicate{dst=display,dst=std{access=file,mux=flv,dst=" + @UnityEngine.Application.streamingAssetsPath + "/record.flv}}";
        //    + "--vout-filter=transform,--transform-type=hflip";
        //https://www.cnblogs.com/waimai/p/3342739.html  , program_arg
        //private static string program_arg = "--network-caching=1000"; 
        //private static string[] arguments = { "-I", "dummy", "--no-ignore-config", "--no-video-title", plugin_arg }; , "--avcodec-hw=any"
        private static string[] arguments = {
            "-I",
            "dummy",
            "--no-ignore-config",
            "--no-video-title",
            "--verbose=4",
            "--ffmpeg-hw",
            "--avcodec-hw=any",
            //plugin_arg
        };

        #region 结构体
        public struct libvlc_media_stats_t
        {
            /* Input */
            public int i_read_bytes;
            public float f_input_bitrate;

            /* Demux */
            public int i_demux_read_bytes;
            public float f_demux_bitrate;
            public int i_demux_corrupted;
            public int i_demux_discontinuity;

            /* Decoders */
            public int i_decoded_video;
            public int i_decoded_audio;

            /* Video Output */
            public int i_displayed_pictures;
            public int i_lost_pictures;

            /* Audio output */
            public int i_played_abuffers;
            public int i_lost_abuffers;

            /* Stream output */
            public int i_sent_packets;
            public int i_sent_bytes;
            public float f_send_bitrate;
        }
        #endregion

        #endregion

        #region 公开函数
        /// <summary>
        /// 创建VLC播放资源索引
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static libvlc_instance_t Create_Media_Instance()
        {
            libvlc_instance_t libvlc_instance = IntPtr.Zero;
            IntPtr argvPtr = IntPtr.Zero;

            try
            {
                if (arguments.Length == 0 ||
                    arguments == null)
                {
                    return IntPtr.Zero;
                }

                //将string数组转换为指针
                argvPtr = StrToIntPtr(arguments);
                if (argvPtr == null || argvPtr == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                //设置启动参数
                libvlc_instance = SafeNativeMethods.libvlc_new(arguments.Length, argvPtr);
                if (libvlc_instance == null || libvlc_instance == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                return libvlc_instance;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 释放VLC播放资源索引
        /// </summary>
        /// <param name="libvlc_instance">VLC 全局变量</param>
        public static void Release_Media_Instance(libvlc_instance_t libvlc_instance)
        {
            try
            {
                if (libvlc_instance != IntPtr.Zero ||
                    libvlc_instance != null)
                {
                    SafeNativeMethods.libvlc_release(libvlc_instance);
                }

                libvlc_instance = IntPtr.Zero;
            }
            catch (Exception)
            {
                libvlc_instance = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 创建VLC播放器
        /// </summary>
        /// <param name="libvlc_instance">VLC 全局变量</param>
        /// <param name="handle">VLC MediaPlayer需要绑定显示的窗体句柄</param>
        /// <returns></returns>
        public static libvlc_media_player_t Create_MediaPlayer(libvlc_instance_t libvlc_instance)
        {
            libvlc_media_player_t libvlc_media_player = IntPtr.Zero;

            try
            {
                if (libvlc_instance == IntPtr.Zero ||
                    libvlc_instance == null)
                {
                    return IntPtr.Zero;
                }

                //创建播放器
                libvlc_media_player = SafeNativeMethods.libvlc_media_player_new(libvlc_instance);
                if (libvlc_media_player == null || libvlc_media_player == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                //设置播放窗口            
                //SafeNativeMethods.libvlc_media_player_set_hwnd(libvlc_media_player, (int)handle);

                return libvlc_media_player;
            }
            catch
            {
                SafeNativeMethods.libvlc_media_player_release(libvlc_media_player);

                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 释放媒体播放器
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        public static void Release_MediaPlayer(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player != IntPtr.Zero ||
                    libvlc_media_player != null)
                {
                    if (SafeNativeMethods.libvlc_media_player_is_playing(libvlc_media_player))
                    {
                        SafeNativeMethods.libvlc_media_player_stop(libvlc_media_player);
                    }

                    SafeNativeMethods.libvlc_media_player_release(libvlc_media_player);
                }

                libvlc_media_player = IntPtr.Zero;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                libvlc_media_player = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 设置文件路径
        /// </summary>
        /// <param name="libvlc_instance"></param>
        /// <param name="libvlc_media_player"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool SetLocation(libvlc_instance_t libvlc_instance, libvlc_media_player_t libvlc_media_player, string url)
        {
            IntPtr pMrl = IntPtr.Zero;
            libvlc_media_t libvlc_media = IntPtr.Zero;

            try
            {
                if (url == null ||
                    libvlc_instance == IntPtr.Zero ||
                    libvlc_instance == null ||
                    libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }
                UnityEngine.Debug.Log("url:" + url);
                pMrl = StrToIntPtr(url);
                if (pMrl == null || pMrl == IntPtr.Zero)
                {
                    return false;
                }

                //播放网络文件
                libvlc_media = SafeNativeMethods.libvlc_media_new_location(libvlc_instance, pMrl);
              
                string[] arguments = 
                { 
                    "--avcodec-hw=any",
                    "--spect-show-original",
                "--avcodec-threads=124"};
                AddOption(libvlc_media, arguments);

                if (libvlc_media == null || libvlc_media == IntPtr.Zero)
                {
                    return false;
                }

                SafeNativeMethods.libvlc_media_parse(libvlc_media);
                long duration = SafeNativeMethods.libvlc_media_get_duration(libvlc_media);
                UnityEngine.Debug.Log("duration: " + duration / 1000);

                //将Media绑定到播放器上
                SafeNativeMethods.libvlc_media_player_set_media(libvlc_media_player, libvlc_media);

                //释放libvlc_media资源
                SafeNativeMethods.libvlc_media_release(libvlc_media);

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                //释放libvlc_media资源
                if (libvlc_media != IntPtr.Zero)
                {
                    SafeNativeMethods.libvlc_media_release(libvlc_media);
                }
                libvlc_media = IntPtr.Zero;

                return false;
            }
        }

        public static void AddOption(libvlc_media_player_t libvlc_media_player, string[] arguments)
        {
            for(int i=0;i<arguments.Length;i++)
            {
                IntPtr pMrl = Marshal.StringToHGlobalAnsi(arguments[i]);
                SafeNativeMethods.libvlc_media_add_option(libvlc_media_player, pMrl);
                Marshal.FreeHGlobal(pMrl);
            }
            
        }

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="libvlc_instance">VLC 全局变量</param>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <param name="url">网络视频URL，支持http、rtp、udp等格式的URL播放</param>
        /// <returns></returns>
        public static bool MediaPlayer_Play(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero || libvlc_media_player == null)
                {
                    return false;
                }

                if (0 != SafeNativeMethods.libvlc_media_player_play(libvlc_media_player))
                {
                    return false;
                }

                //休眠指定时间
                Thread.Sleep(500);

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        public static void SetFormart(libvlc_media_player_t libvlc_media_player, string chroma, int width, int height, int pitch)
        {
            SafeNativeMethods.libvlc_video_set_format(libvlc_media_player, StrToIntPtr(chroma), width, height, pitch);
        }

        public static int GetMediaWidth(libvlc_media_player_t libvlc_media_player)
        {
            int width = SafeNativeMethods.libvlc_video_get_width(libvlc_media_player);
            return width;
        }

        public static int GetMediaHeight(libvlc_media_player_t libvlc_media_player)
        {
            int height = SafeNativeMethods.libvlc_video_get_height(libvlc_media_player);
            return height;
        }

        /// <summary>
        /// 暂停或恢复视频
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <returns></returns>
        public static bool MediaPlayer_Pause(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_can_pause(libvlc_media_player))
                {
                    SafeNativeMethods.libvlc_media_player_pause(libvlc_media_player);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <returns></returns>
        public static bool MediaPlayer_Stop(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                SafeNativeMethods.libvlc_media_player_stop(libvlc_media_player);

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 快进
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <returns></returns>
        public static bool MediaPlayer_Forward(libvlc_media_player_t libvlc_media_player)
        {
            double time = 0;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_is_seekable(libvlc_media_player))
                {
                    time = SafeNativeMethods.libvlc_media_player_get_time(libvlc_media_player) / 1000.0;
                    if (time == -1)
                    {
                        return false;
                    }

                    SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)((time + 30) * 1000));

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 快退
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <returns></returns>
        public static bool MediaPlayer_Back(libvlc_media_player_t libvlc_media_player)
        {
            double time = 0;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                if (SafeNativeMethods.libvlc_media_player_is_seekable(libvlc_media_player))
                {
                    time = SafeNativeMethods.libvlc_media_player_get_time(libvlc_media_player) / 1000.0;
                    if (time == -1)
                    {
                        return false;
                    }

                    if (time - 30 < 0)
                    {
                        SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)(1 * 1000));
                    }
                    else
                    {
                        SafeNativeMethods.libvlc_media_player_set_time(libvlc_media_player, (Int64)((time - 30) * 1000));
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// VLC MediaPlayer是否在播放
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <returns></returns>
        public static bool MediaPlayer_IsPlaying(libvlc_media_player_t libvlc_media_player)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                return SafeNativeMethods.libvlc_media_player_is_playing(libvlc_media_player);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 录制快照
        /// </summary>
        /// <param name="libvlc_media_player">VLC MediaPlayer变量</param>
        /// <param name="path">快照要存放的路径</param>
        /// <param name="name">快照保存的文件名称</param>
        /// <returns></returns>
        public static bool TakeSnapShot(libvlc_media_player_t libvlc_media_player, string path, string name, int width, int height)
        {
            try
            {
                string snap_shot_path = null;

                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    UnityEngine.Debug.LogError("HERE1");
                    return false;
                }

                snap_shot_path = path + "\\" + name;
                snap_shot_path = snap_shot_path.Replace('/', '\\');
                //snap_shot_path = @"D:\\1.jpg";
                UnityEngine.Debug.LogError("snap_shot_path:" + snap_shot_path);

                int code = SafeNativeMethods.libvlc_video_take_snapshot(libvlc_media_player, 0, snap_shot_path.ToCharArray(), 0, 0);
                UnityEngine.Debug.LogError("code:" + code);
                if (0 == code)
                {
                    UnityEngine.Debug.LogError("HERE2");
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError("HERE3");
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取信息
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <returns></returns>
        public static bool GetMedia(libvlc_media_player_t libvlc_media_player)
        {
            libvlc_media_t media = IntPtr.Zero;

            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                media = SafeNativeMethods.libvlc_media_player_get_media(libvlc_media_player);
                if (media == IntPtr.Zero || media == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取已经显示的图片数
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <returns></returns>
        public static int GetDisplayedPictures(libvlc_media_player_t libvlc_media_player)
        {
            libvlc_media_t media = IntPtr.Zero;
            libvlc_media_stats_t media_stats = new libvlc_media_stats_t();
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return 0;
                }

                media = SafeNativeMethods.libvlc_media_player_get_media(libvlc_media_player);
                if (media == IntPtr.Zero || media == null)
                {
                    return 0;
                }

                if (1 == SafeNativeMethods.libvlc_media_get_stats(media, ref media_stats))
                {
                    return media_stats.i_displayed_pictures;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 设置全屏
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <param name="isFullScreen"></param>
        public static bool SetFullScreen(libvlc_media_player_t libvlc_media_player, int isFullScreen)
        {
            try
            {
                if (libvlc_media_player == IntPtr.Zero ||
                    libvlc_media_player == null)
                {
                    return false;
                }

                SafeNativeMethods.libvlc_set_fullscreen(libvlc_media_player, isFullScreen);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 设置回调
        /// </summary>
        /// <param name="libvlc_media_player"></param>
        /// <param name="lockcb"></param>
        /// <param name="unlockcb"></param>
        /// <param name="displaycb"></param>
        /// <param name="opaque"></param>
        public static void SetCallbacks(libvlc_media_player_t libvlc_media_player, VideoLockCB lockcb, VideoUnlockCB unlockcb, VideoDisplayCB displaycb, IntPtr opaque)
        {
            try
            {
                SafeNativeMethods.libvlc_video_set_callbacks(libvlc_media_player, lockcb, unlockcb, displaycb, opaque);
                UnityEngine.Debug.Log("SetCallbacks");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        #endregion

        #region 私有函数
        //将string []转换为IntPtr
        public static IntPtr StrToIntPtr(string[] args)
        {
            try
            {
                IntPtr ip_args = IntPtr.Zero;

                PointerToArrayOfPointerHelper argv = new PointerToArrayOfPointerHelper();
                argv.pointers = new IntPtr[args.Length];

                for (int i = 0; i < args.Length; i++)
                {
                    argv.pointers[i] = Marshal.StringToHGlobalAnsi(args[i]);
                }

                int size = Marshal.SizeOf(typeof(PointerToArrayOfPointerHelper));
                ip_args = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(argv, ip_args, false);

                return ip_args;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        //将string转换为IntPtr
        private static IntPtr StrToIntPtr(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return IntPtr.Zero;
                }

                IntPtr pMrl = IntPtr.Zero;
                byte[] bytes = Encoding.UTF8.GetBytes(url);

                pMrl = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, pMrl, bytes.Length);
                Marshal.WriteByte(pMrl, bytes.Length, 0);

                return pMrl;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }
        #endregion

        #region 导入库函数
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            // 创建一个libvlc实例，它是引用计数的
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_instance_t libvlc_new(int argc, IntPtr argv);

            // 释放libvlc实例
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_release(libvlc_instance_t libvlc_instance);

            //获取libvlc的版本
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern String libvlc_get_version();

            //从视频来源(例如http、rtsp)构建一个libvlc_meida
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_new_location(libvlc_instance_t libvlc_instance, IntPtr path);

            //从本地文件路径构建一个libvlc_media
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_new_path(libvlc_instance_t libvlc_instance, IntPtr path);

            //释放libvlc_media
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_release(libvlc_media_t libvlc_media_inst);

            // 创建一个空的播放器
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_player_t libvlc_media_player_new(libvlc_instance_t libvlc_instance);

            //从libvlc_media构建播放器
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_player_t libvlc_media_player_new_from_media(libvlc_media_t libvlc_media);

            //释放播放器资源
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_release(libvlc_media_player_t libvlc_mediaplayer);

            // 将视频(libvlc_media)绑定到播放器上
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_media(libvlc_media_player_t libvlc_media_player, libvlc_media_t libvlc_media);

            // 参数设置
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_add_option(libvlc_media_player_t libvlc_media_player, IntPtr options);

            //获取播放信息
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_tracks_get(libvlc_media_player_t libvlc_media_player, libvlc_media_t libvlc_media);

            // 设置编码
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_video_set_format(libvlc_media_player_t libvlc_media_player, IntPtr chroma, Int32 width, Int32 height, Int32 pitch);

            // 视频每一帧的数据信息
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_video_set_callbacks(libvlc_media_player_t libvlc_mediaplayer, VideoLockCB lockCB, VideoUnlockCB unlockCB, VideoDisplayCB displayCB, IntPtr opaque);

            // 设置图像输出的窗口
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_hwnd(libvlc_media_player_t libvlc_mediaplayer, Int32 drawable);

            //播放器播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_player_play(libvlc_media_player_t libvlc_mediaplayer);

            //播放器暂停
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_pause(libvlc_media_player_t libvlc_mediaplayer);

            //播放器停止
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_stop(libvlc_media_player_t libvlc_mediaplayer);

            // 解析视频资源的媒体信息(如时长等)
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_parse(libvlc_media_t libvlc_media);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int32 libvlc_video_get_width(libvlc_media_player_t libvlc_mediaplayer);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int32 libvlc_video_get_height(libvlc_media_player_t libvlc_mediaplayer);

            // 返回视频的时长(必须先调用libvlc_media_parse之后，该函数才会生效)
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int64 libvlc_media_get_duration(libvlc_media_t libvlc_media);

            // 当前播放时间
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern Int64 libvlc_media_player_get_time(libvlc_media_player_t libvlc_mediaplayer);

            // 设置播放时间
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_media_player_set_time(libvlc_media_player_t libvlc_mediaplayer, Int64 time);

            // 获取音量
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_audio_get_volume(libvlc_media_player_t libvlc_media_player);

            //设置音量
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_audio_set_volume(libvlc_media_player_t libvlc_media_player, int volume);

            // 设置全屏
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_set_fullscreen(libvlc_media_player_t libvlc_media_player, int isFullScreen);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_get_fullscreen(libvlc_media_player_t libvlc_media_player);

            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void libvlc_toggle_fullscreen(libvlc_media_player_t libvlc_media_player);

            //判断播放时是否在播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_is_playing(libvlc_media_player_t libvlc_media_player);

            //判断播放时是否能够Seek
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_is_seekable(libvlc_media_player_t libvlc_media_player);

            //判断播放时是否能够Pause
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern bool libvlc_media_player_can_pause(libvlc_media_player_t libvlc_media_player);

            //判断播放器是否可以播放
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_player_will_play(libvlc_media_player_t libvlc_media_player);

            //进行快照
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_video_take_snapshot(libvlc_media_player_t libvlc_media_player, int num, char[] filepath, int i_width, int i_height);

            //获取Media信息
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern libvlc_media_t libvlc_media_player_get_media(libvlc_media_player_t libvlc_media_player);

            //获取媒体信息
            [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int libvlc_media_get_stats(libvlc_media_t libvlc_media, ref libvlc_media_stats_t lib_vlc_media_stats);
        }
        #endregion
    }
}