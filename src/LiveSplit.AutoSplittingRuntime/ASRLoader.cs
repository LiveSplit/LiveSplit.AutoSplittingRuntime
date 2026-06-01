using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LiveSplit.AutoSplittingRuntime;

public class ASRLoader
{
    [DllImport("kernel32")]
    private static extern unsafe void* LoadLibrary(string dllname);

    [DllImport("kernel32")]
    private static extern unsafe void FreeLibrary(void* handle);

    private sealed unsafe class LibraryUnloader
    {
        internal LibraryUnloader(void* handle)
        {
            this.handle = handle;
        }

        ~LibraryUnloader()
        {
            if (handle != null)
            {
                FreeLibrary(handle);
            }
        }

        private readonly void* handle;

    }

    private static LibraryUnloader unloader;

    public static void LoadASR()
    {
        if (unloader != null)
        {
            return;
        }

        string path = Unsafe.SizeOf<IntPtr>() == 8
            ? @"Components\x64\asr_capi.dll"
            : @"Components\x86\asr_capi.dll";
        unsafe
        {
            void* handle = LoadLibrary(path);

            if (handle == null)
            {
                throw new DllNotFoundException("Unable to load the native ASR library: " + path);
            }

            unloader = new LibraryUnloader(handle);
        }
    }
}
