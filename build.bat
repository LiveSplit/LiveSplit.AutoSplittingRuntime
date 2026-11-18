cd src\asr-capi
cargo +nightly-x86_64-pc-windows-msvc build -Zbuild-std=std,panic_abort --config profile.release.trim-paths=\"all\" --release --target x86_64-win7-windows-msvc
copy target\x86_64-win7-windows-msvc\release\asr_capi.dll ..\LiveSplit.AutoSplittingRuntime\x64\.
cargo +nightly-x86_64-pc-windows-msvc build -Zbuild-std=std,panic_abort --config profile.release.trim-paths=\"all\" --release --target i686-win7-windows-msvc
copy target\i686-win7-windows-msvc\release\asr_capi.dll ..\LiveSplit.AutoSplittingRuntime\x86\.
