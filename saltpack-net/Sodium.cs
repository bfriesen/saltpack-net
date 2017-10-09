using RockLib.Interop;
using System;
using System.Runtime.InteropServices;

namespace SaltpackDotNet
{
    internal static class Sodium
    {
        private static readonly EmbeddedNativeLibrary _sodiumNative;

        static Sodium()
        {
            _sodiumNative = new EmbeddedNativeLibrary(
                "libsodium",
                new DllInfo(TargetRuntime.Win32, "Saltpack.win_x86.native.libsodium.dll", "Saltpack.win_x86.native.msvcr120.dll"),
                new DllInfo(TargetRuntime.Win64, "Saltpack.win_x64.native.libsodium.dll", "Saltpack.win_x64.native.msvcr120.dll"),
                new DllInfo(TargetRuntime.Mac, "Saltpack.osx_x64.native.libsodium.dylib"),
                new DllInfo(TargetRuntime.Linux, "Saltpack.linux_x64.native.libsodium.so"));

            if (sodium_init() == -1)
                throw new InvalidOperationException("The libsodium library failed to initialize: sodium_init() returned -1.");
        }

        private static int sodium_init() => _sodiumNative.GetDelegate<sodium_init_>(nameof(sodium_init)).Invoke();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int sodium_init_();
    }
}
