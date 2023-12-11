use crate::settings_map::SettingsMap;

#[cfg(target_pointer_width = "64")]
use crate::{output_str, setting_value::SettingValue};
#[cfg(target_pointer_width = "64")]
use livesplit_auto_splitting::settings::{Widget, WidgetKind};
#[cfg(target_pointer_width = "64")]
use std::sync::Arc;

#[cfg(target_pointer_width = "64")]
pub struct UserSettings {
    pub inner: Arc<Vec<Widget>>,
}

#[cfg(not(target_pointer_width = "64"))]
pub type UserSettings = ();

#[no_mangle]
pub extern "C" fn UserSettings_drop(_: Box<UserSettings>) {}

#[no_mangle]
pub extern "C" fn UserSettings_len(_this: &UserSettings) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        _this.inner.len()
    }
    #[cfg(not(target_pointer_width = "64"))]
    0
}

#[no_mangle]
pub extern "C" fn UserSettings_get_key(_this: &UserSettings, _index: usize) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(&_this.inner[_index].key)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_description(_this: &UserSettings, _index: usize) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(&_this.inner[_index].description)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_tooltip(_this: &UserSettings, _index: usize) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        output_str(_this.inner[_index].tooltip.as_deref().unwrap_or_default())
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_type(_this: &UserSettings, _index: usize) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        match _this.inner[_index].kind {
            WidgetKind::Bool { .. } => 1,
            WidgetKind::Title { .. } => 2,
            WidgetKind::Choice { .. } => 3,
            WidgetKind::FileSelection { .. } => 4,
        }
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_bool(
    _this: &UserSettings,
    _index: usize,
    _settings_map: &SettingsMap,
) -> bool {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Bool { default_value } = setting.kind else {
            return false;
        };
        match _settings_map.get(&setting.key) {
            Some(SettingValue::Bool(stored)) => *stored,
            _ => default_value,
        }
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_choice_current_index(
    _this: &UserSettings,
    _index: usize,
    _settings_map: &SettingsMap,
) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Choice {
            default_option_key,
            options,
        } = &setting.kind
        else {
            return 0;
        };
        let key = match _settings_map.get(&setting.key) {
            Some(SettingValue::String(stored)) => stored,
            _ => default_option_key,
        };
        options
            .iter()
            .position(|option| option.key == *key)
            .or_else(|| {
                options
                    .iter()
                    .position(|option| option.key == *default_option_key)
            })
            .unwrap_or_default()
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_choice_options_len(
    _this: &UserSettings,
    _index: usize,
) -> usize {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Choice { options, .. } = &setting.kind else {
            return 0;
        };
        options.len()
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_choice_option_key(
    _this: &UserSettings,
    _index: usize,
    _option_index: usize,
) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Choice { options, .. } = &setting.kind else {
            return output_str("");
        };
        output_str(&options[_option_index].key)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_choice_option_description(
    _this: &UserSettings,
    _index: usize,
    _option_index: usize,
) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Choice { options, .. } = &setting.kind else {
            return output_str("");
        };
        output_str(&options[_option_index].description)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_heading_level(_this: &UserSettings, _index: usize) -> u32 {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::Title { heading_level } = setting.kind else {
            return 0;
        };
        heading_level
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}

#[no_mangle]
pub extern "C" fn UserSettings_get_fileselection_filter(
    _this: &UserSettings,
    _index: usize,
) -> *const u8 {
    #[cfg(target_pointer_width = "64")]
    {
        let setting = &_this.inner[_index];
        let WidgetKind::FileSelection { filter } = &setting.kind else {
            return output_str("");
        };
        output_str(&filter)
    }
    #[cfg(not(target_pointer_width = "64"))]
    panic!("Index out of bounds")
}
