#[cfg(target_pointer_width = "64")]
use {
    livesplit_auto_splitting::{
        time, Config, SettingValue, SettingsStore, Timer, TimerState, UserSettingKind,
    },
    std::{cell::RefCell, ffi::CStr, fmt, fs},
};

#[cfg(target_pointer_width = "64")]
thread_local! {
    static OUTPUT_VEC: RefCell<Vec<u8>>  = RefCell::new(Vec::new());
}

#[cfg(target_pointer_width = "64")]
fn output_vec<F>(f: F) -> *const u8
where
    F: FnOnce(&mut Vec<u8>),
{
    OUTPUT_VEC.with(|output| {
        let mut output = output.borrow_mut();
        output.clear();
        f(&mut output);
        output.push(0);
        output.as_ptr()
    })
}

#[cfg(target_pointer_width = "64")]
fn output_str(s: &str) -> *const u8 {
    output_vec(|o| {
        o.extend_from_slice(s.as_bytes());
    })
}

#[cfg(target_pointer_width = "64")]
unsafe fn str(s: *const u8) -> &'static str {
    if s.is_null() {
        ""
    } else {
        let bytes = CStr::from_ptr(s.cast()).to_bytes();
        std::str::from_utf8_unchecked(bytes)
    }
}

#[cfg(target_pointer_width = "64")]
pub struct Runtime {
    runtime: livesplit_auto_splitting::Runtime<CTimer>,
}

#[cfg(not(target_pointer_width = "64"))]
pub type Runtime = ();

#[cfg(not(target_pointer_width = "64"))]
pub type SettingsStore = ();

#[no_mangle]
pub extern "C" fn SettingsStore_new() -> Box<SettingsStore> {
    #[cfg(target_pointer_width = "64")]
    {
        Box::new(SettingsStore::new())
    }
    #[cfg(not(target_pointer_width = "64"))]
    Box::new(())
}

#[no_mangle]
pub extern "C" fn SettingsStore_drop(_: Box<SettingsStore>) {}

/// # Safety
/// TODO:
#[no_mangle]
pub unsafe extern "C" fn SettingsStore_set_bool(
    _this: &mut SettingsStore,
    _key_ptr: *const u8,
    _value: bool,
) {
    #[cfg(target_pointer_width = "64")]
    {
        _this.set(str(_key_ptr as _).into(), SettingValue::Bool(_value));
    }
}

/// # Safety
/// TODO:
#[no_mangle]
pub unsafe extern "C" fn Runtime_new(
    _path_ptr: *const u8,
    _settings_store: Box<SettingsStore>,
    _state: unsafe extern "C" fn() -> i32,
    _start: unsafe extern "C" fn(),
    _split: unsafe extern "C" fn(),
    _skip_split: unsafe extern "C" fn(),
    _undo_split: unsafe extern "C" fn(),
    _reset: unsafe extern "C" fn(),
    _set_game_time: unsafe extern "C" fn(i64),
    _pause_game_time: unsafe extern "C" fn(),
    _resume_game_time: unsafe extern "C" fn(),
    _log: unsafe extern "C" fn(*const u8, usize),
) -> Option<Box<Runtime>> {
    #[cfg(target_pointer_width = "64")]
    {
        let path = str(_path_ptr);
        let file = fs::read(path).ok()?;

        let mut config = Config::default();
        config.settings_store = Some(*_settings_store);

        let runtime = livesplit_auto_splitting::Runtime::new(
            &file,
            CTimer {
                state: _state,
                start: _start,
                split: _split,
                skip_split: _skip_split,
                undo_split: _undo_split,
                reset: _reset,
                set_game_time: _set_game_time,
                pause_game_time: _pause_game_time,
                resume_game_time: _resume_game_time,
                log: _log,
            },
            config,
        )
        .ok()?;

        Some(Box::new(Runtime { runtime }))
    }
    #[cfg(not(target_pointer_width = "64"))]
    Some(Box::new(()))
}

#[no_mangle]
pub extern "C" fn Runtime_drop(_: Box<Runtime>) {}

#[no_mangle]
pub extern "C" fn Runtime_step(_this: &Runtime) -> bool {
    #[cfg(target_pointer_width = "64")]
    {
        _this.runtime.lock().update().is_ok()
    }
    #[cfg(not(target_pointer_width = "64"))]
    true
}

#[no_mangle]
pub extern "C" fn Runtime_tick_rate(_this: &Runtime) -> u64 {
    const TICKS_PER_SEC: u64 = 10_000_000;
    const NANOS_PER_SEC: u64 = 1_000_000_000;
    const NANOS_PER_TICK: u64 = NANOS_PER_SEC / TICKS_PER_SEC;

    #[cfg(target_pointer_width = "64")]
    let tick_rate = _this.runtime.tick_rate();
    #[cfg(not(target_pointer_width = "64"))]
    let tick_rate = std::time::Duration::new(1, 0) / 120;

    let (secs, nanos) = (tick_rate.as_secs(), tick_rate.subsec_nanos());

    secs * TICKS_PER_SEC + nanos as u64 / NANOS_PER_TICK
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_len(_this: &Runtime) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        _this.runtime.user_settings().len()
    }
    #[cfg(not(target_pointer_width = "64"))]
    0
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_key(_this: &Runtime, _index: usize) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(&_this.runtime.user_settings()[_index].key)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_description(
    _this: &Runtime,
    _index: usize,
) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(&_this.runtime.user_settings()[_index].description)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_tooltip(_this: &Runtime, _index: usize) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(
            _this.runtime.user_settings()[_index]
                .tooltip
                .as_deref()
                .unwrap_or_default(),
        )
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_type(_this: &Runtime, _index: usize) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        match _this.runtime.user_settings()[_index].kind {
            UserSettingKind::Bool { .. } => 1,
            UserSettingKind::Title { .. } => 2,
        }
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_bool(_this: &Runtime, _index: usize) -> bool {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.runtime.user_settings()[_index];
        let UserSettingKind::Bool { default_value } = setting.kind else {
            return false;
        };
        match _this.runtime.settings_store().get(&setting.key) {
            Some(SettingValue::Bool(stored)) => *stored,
            _ => default_value,
        }
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn Runtime_user_settings_get_heading_level(_this: &Runtime, _index: usize) -> u32 {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.runtime.user_settings()[_index];
        let UserSettingKind::Title { heading_level } = setting.kind else {
            return 0;
        };
        heading_level
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[cfg(target_pointer_width = "64")]
pub struct CTimer {
    state: unsafe extern "C" fn() -> i32,
    start: unsafe extern "C" fn(),
    split: unsafe extern "C" fn(),
    skip_split: unsafe extern "C" fn(),
    undo_split: unsafe extern "C" fn(),
    reset: unsafe extern "C" fn(),
    set_game_time: unsafe extern "C" fn(i64),
    pause_game_time: unsafe extern "C" fn(),
    resume_game_time: unsafe extern "C" fn(),
    log: unsafe extern "C" fn(*const u8, usize),
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

    fn skip_split(&mut self) {
        unsafe { (self.skip_split)() }
    }

    fn undo_split(&mut self) {
        unsafe { (self.undo_split)() }
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

    fn log(&mut self, message: fmt::Arguments<'_>) {
        let owned;
        let message = match message.as_str() {
            Some(m) => m,
            None => {
                owned = message.to_string();
                &owned
            }
        };
        unsafe { (self.log)(message.as_ptr(), message.len()) }
    }
}

/// Returns the byte length of the last nul-terminated string returned on the
/// current thread. The length excludes the nul-terminator.
#[no_mangle]
pub extern "C" fn get_buf_len() -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        OUTPUT_VEC.with(|v| v.borrow().len() - 1)
    }
    #[cfg(not(target_pointer_width = "64"))]
    0
}
