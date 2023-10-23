using LiveSplitCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TimeSpan = System.TimeSpan;

namespace LiveSplit.AutoSplittingRuntime
{
    public class RuntimeRef
    {
        internal IntPtr ptr;
        internal RuntimeRef(IntPtr ptr)
        {
            this.ptr = ptr;
        }
    }

    public class RuntimeRefMut : RuntimeRef
    {
        internal RuntimeRefMut(IntPtr ptr) : base(ptr) { }
    }

    public class Runtime : RuntimeRefMut, IDisposable
    {
        private void Drop()
        {
            if (ptr != IntPtr.Zero)
            {
                ASRNative.Runtime_drop(this.ptr);
                ptr = IntPtr.Zero;
            }
        }
        ~Runtime()
        {
            Drop();
        }
        public void Dispose()
        {
            Drop();
            GC.SuppressFinalize(this);
        }
        public Runtime(
            string path,
            SettingsStore settingsStore,
            StateDelegate state,
            Action start,
            Action split,
            Action skipSplit,
            Action undoSplit,
            Action reset,
            SetGameTimeDelegate setGameTime,
            Action pauseGameTime,
            Action resumeGameTime,
            LogDelegate log
        ) : base(IntPtr.Zero)
        {
            IntPtr settingsStorePtr = settingsStore.ptr;
            if (settingsStorePtr == IntPtr.Zero)
            {
                throw new ArgumentException("The Settings Store is disposed.");
            }
            settingsStore.ptr = IntPtr.Zero;
            this.ptr = ASRNative.Runtime_new(
                path,
                settingsStorePtr,
                state,
                start,
                split,
                skipSplit,
                undoSplit,
                reset,
                setGameTime,
                pauseGameTime,
                resumeGameTime,
                log
            );
            if (this.ptr == IntPtr.Zero)
            {
                throw new ArgumentException("Couldn't load the module provided.");
            }
        }
        internal Runtime(IntPtr ptr) : base(ptr) { }

        public bool Step()
        {
            if (ptr == IntPtr.Zero)
            {
                return false;
            }
            return ASRNative.Runtime_step(this.ptr);
        }

        public TimeSpan TickRate()
        {
            if (ptr == IntPtr.Zero)
            {
                return TimeSpan.Zero;
            }
            return new TimeSpan((long)ASRNative.Runtime_tick_rate(this.ptr));
        }

        public ulong UserSettingsLength()
        {
            if (ptr == IntPtr.Zero)
            {
                return 0;
            }
            return (ulong)ASRNative.Runtime_user_settings_len(this.ptr);
        }

        public string UserSettingGetKey(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return ASRNative.Runtime_user_settings_get_key(this.ptr, (UIntPtr)index);
        }

        public string UserSettingGetDescription(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return ASRNative.Runtime_user_settings_get_description(this.ptr, (UIntPtr)index);
        }

        public string UserSettingGetTooltip(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            return ASRNative.Runtime_user_settings_get_tooltip(this.ptr, (UIntPtr)index);
        }

        public uint UserSettingGetHeadingLevel(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return 0;
            }
            return ASRNative.Runtime_user_settings_get_heading_level(this.ptr, (UIntPtr)index);
        }

        public string UserSettingGetType(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return "";
            }
            var ty = ASRNative.Runtime_user_settings_get_type(this.ptr, (UIntPtr)index);
            switch ((ulong)ty)
            {
                case 1: return "bool";
                case 2: return "title";
                default: return "";
            }
        }

        public bool UserSettingGetBool(ulong index)
        {
            if (ptr == IntPtr.Zero)
            {
                return false;
            }
            return ASRNative.Runtime_user_settings_get_bool(this.ptr, (UIntPtr)index) != 0;
        }
    }

    public class SettingsStoreRef
    {
        internal IntPtr ptr;
        internal SettingsStoreRef(IntPtr ptr)
        {
            this.ptr = ptr;
        }
    }

    public class SettingsStoreRefMut : SettingsStoreRef
    {
        internal SettingsStoreRefMut(IntPtr ptr) : base(ptr) { }
    }

    public class SettingsStore : SettingsStoreRefMut, IDisposable
    {
        private void Drop()
        {
            if (ptr != IntPtr.Zero)
            {
                ASRNative.SettingsStore_drop(this.ptr);
                ptr = IntPtr.Zero;
            }
        }
        ~SettingsStore()
        {
            Drop();
        }
        public void Dispose()
        {
            Drop();
            GC.SuppressFinalize(this);
        }
        public SettingsStore() : base(IntPtr.Zero)
        {
            this.ptr = ASRNative.SettingsStore_new();
            if (this.ptr == IntPtr.Zero)
            {
                throw new ArgumentException("Couldn't create the Settings Store.");
            }
        }
        internal SettingsStore(IntPtr ptr) : base(ptr) { }
        public void Set(string key, bool value)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }
            ASRNative.SettingsStore_set_bool(this.ptr, key, value ? (byte)1 : (byte)0);
        }
    }

    public delegate int StateDelegate();
    public delegate void SetGameTimeDelegate(long gameTime);
    public delegate void LogDelegate(IntPtr messagePtr, UIntPtr messageLen);

    public static class ASRNative
    {
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Runtime_new(
            ASRString path,
            IntPtr settings_store,
            StateDelegate state,
            Action start,
            Action split,
            Action skipSplit,
            Action undoSplit,
            Action reset,
            SetGameTimeDelegate set_game_time,
            Action pause_game_time,
            Action resume_game_time,
            LogDelegate log
        );
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Runtime_drop(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Runtime_step(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong Runtime_tick_rate(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr Runtime_user_settings_len(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASRString Runtime_user_settings_get_key(IntPtr self, UIntPtr index);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASRString Runtime_user_settings_get_description(IntPtr self, UIntPtr index);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern ASRString Runtime_user_settings_get_tooltip(IntPtr self, UIntPtr index);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Runtime_user_settings_get_heading_level(IntPtr self, UIntPtr index);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr Runtime_user_settings_get_type(IntPtr self, UIntPtr index);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte Runtime_user_settings_get_bool(IntPtr self, UIntPtr index);

        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SettingsStore_new();
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SettingsStore_drop(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SettingsStore_set_bool(IntPtr self, ASRString key, byte value);

        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr get_buf_len();
    }

    public class ASRString : SafeHandle
    {
        private bool needToFree;

        public ASRString() : base(IntPtr.Zero, false) { }

        public override bool IsInvalid
        {
            get { return false; }
        }

        public static implicit operator ASRString(string managedString)
        {
            ASRString asrString = new ASRString();

            int len = Encoding.UTF8.GetByteCount(managedString);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            asrString.SetHandle(nativeUtf8);
            asrString.needToFree = true;
            return asrString;
        }

        public static string FromPtrLen(IntPtr ptr, UIntPtr len)
        {
            if (ptr == IntPtr.Zero || (ulong)len > (ulong)int.MaxValue)
            {
                return null;
            }

            unsafe
            {
                return Encoding.UTF8.GetString((byte*)ptr, (int)len);
            }
        }

        public static implicit operator string(ASRString asrString)
        {
            return FromPtrLen(asrString.handle, ASRNative.get_buf_len());
        }

        protected override bool ReleaseHandle()
        {
            if (needToFree)
            {
                Marshal.FreeHGlobal(handle);
            }
            return true;
        }
    }

}
