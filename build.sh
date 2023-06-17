cd asr-capi
cargo +stable-x86_64-pc-windows-msvc build --release
cp target/release/asr_capi.dll ../x64/.
cargo +stable-i686-pc-windows-msvc build --release
cp target/release/asr_capi.dll ../x86/.
