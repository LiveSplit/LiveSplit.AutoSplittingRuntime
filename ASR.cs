using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            StateDelegate state,
            Action start,
            Action split,
            Action reset,
            SetGameTimeDelegate setGameTime,
            Action pauseGameTime,
            Action resumeGameTime
        ) : base(IntPtr.Zero)
        {
            this.ptr = ASRNative.Runtime_new(
                path,
                state,
                start,
                split,
                reset,
                setGameTime,
                pauseGameTime,
                resumeGameTime
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
        public static void SetLogger(LogDelegate log)
        {
            ASRNative.Runtime_set_logger(log);
        }
    }

    public delegate int StateDelegate();
    public delegate void SetGameTimeDelegate(long gameTime);
    public delegate void LogDelegate(IntPtr messagePtr, IntPtr messageLen, byte logLevel);

    public static class ASRNative
    {
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Runtime_new(
            ASRString path,
            StateDelegate state,
            Action start,
            Action split,
            Action reset,
            SetGameTimeDelegate set_game_time,
            Action pause_game_time,
            Action resume_game_time
        );
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Runtime_drop(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Runtime_step(IntPtr self);
        [DllImport("asr_capi", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Runtime_set_logger(LogDelegate log);
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

        public static string FromPtrLen(IntPtr ptr, IntPtr len)
        {
            if (ptr == IntPtr.Zero)
                return null;

            byte[] buffer = new byte[(long)len];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
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
