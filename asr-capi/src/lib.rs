#[cfg(target_pointer_width = "64")]
use {
    livesplit_auto_splitting::{time, Runtime, Timer, TimerState},
    log::{Level, LevelFilter},
    std::{ffi::CStr, path::Path},
};

type LogCallback = unsafe extern "C" fn(*const u8, usize, u8);

#[no_mangle]
pub unsafe extern "C" fn Runtime_set_logger(_log_fn: LogCallback) {
    #[cfg(target_pointer_width = "64")]
    {
        struct Logger;

        impl log::Log for Logger {
            fn enabled(&self, metadata: &log::Metadata) -> bool {
                metadata.level() <= Level::Info
            }

            fn log(&self, record: &log::Record) {
                if self.enabled(record.metadata()) {
                    let msg = format!("[Auto Splitting Runtime] {}", record.args());
                    unsafe {
                        let f: LogCallback = core::mem::transmute(self as *const Logger);
                        f(msg.as_ptr(), msg.len(), record.level() as u8);
                    }
                }
            }

            fn flush(&self) {}
        }

        _ = log::set_logger(&*(_log_fn as *const Logger));
        log::set_max_level(LevelFilter::Info);
    }
}

#[cfg(target_pointer_width = "64")]
pub type CRuntime = Runtime<CTimer>;

#[cfg(not(target_pointer_width = "64"))]
pub type CRuntime = ();

#[no_mangle]
pub unsafe extern "C" fn Runtime_new(
    _path_ptr: *const u8,
    _state: unsafe extern "C" fn() -> i32,
    _start: unsafe extern "C" fn(),
    _split: unsafe extern "C" fn(),
    _reset: unsafe extern "C" fn(),
    _set_game_time: unsafe extern "C" fn(i64),
    _pause_game_time: unsafe extern "C" fn(),
    _resume_game_time: unsafe extern "C" fn(),
) -> Option<Box<CRuntime>> {
    #[cfg(target_pointer_width = "64")]
    {
        let path = CStr::from_ptr(_path_ptr as _).to_str().ok()?;
        Runtime::new(
            Path::new(path),
            CTimer {
                state: _state,
                start: _start,
                split: _split,
                reset: _reset,
                set_game_time: _set_game_time,
                pause_game_time: _pause_game_time,
                resume_game_time: _resume_game_time,
            },
        )
        .ok()
        .map(Box::new)
    }
    #[cfg(not(target_pointer_width = "64"))]
    Some(Box::new(()))
}

#[no_mangle]
pub extern "C" fn Runtime_drop(_: Box<CRuntime>) {}

#[no_mangle]
pub extern "C" fn Runtime_step(_this: &mut CRuntime) -> bool {
    #[cfg(target_pointer_width = "64")]
    {
        _this.step().is_ok()
    }
    #[cfg(not(target_pointer_width = "64"))]
    true
}

#[cfg(target_pointer_width = "64")]
pub struct CTimer {
    state: unsafe extern "C" fn() -> i32,
    start: unsafe extern "C" fn(),
    split: unsafe extern "C" fn(),
    reset: unsafe extern "C" fn(),
    set_game_time: unsafe extern "C" fn(i64),
    pause_game_time: unsafe extern "C" fn(),
    resume_game_time: unsafe extern "C" fn(),
}

#[cfg(target_pointer_width = "64")]
impl Timer for CTimer {
    fn state(&self) -> TimerState {
        match unsafe { (self.state)() } {
            1 => TimerState::Running,
            2 => TimerState::Paused,
            3 => TimerState::Ended,
            _ => TimerState::NotRunning,
        }
    }

    fn start(&mut self) {
        unsafe { (self.start)() }
    }

    fn split(&mut self) {
        unsafe { (self.split)() }
    }

    fn reset(&mut self) {
        unsafe { (self.reset)() }
    }

    fn set_game_time(&mut self, time: time::Duration) {
        const TICKS_PER_SEC: i64 = 10_000_000;
        const NANOS_PER_SEC: i64 = 1_000_000_000;
        const NANOS_PER_TICK: i64 = NANOS_PER_SEC / TICKS_PER_SEC;

        let (secs, nanos) = (time.whole_seconds(), time.subsec_nanoseconds());
        let ticks = secs * TICKS_PER_SEC + nanos as i64 / NANOS_PER_TICK;
        unsafe { (self.set_game_time)(ticks) }
    }

    fn pause_game_time(&mut self) {
        unsafe { (self.pause_game_time)() }
    }

    fn resume_game_time(&mut self) {
        unsafe { (self.resume_game_time)() }
    }

    fn set_variable(&mut self, _: &str, _: &str) {}
}
