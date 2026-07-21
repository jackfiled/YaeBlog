use std::ffi::{CStr, CString, c_char, c_void};

#[repr(C)]
pub struct ExternResult<T> {
    pub content: T,
    pub error_message: *mut c_char,
    pub succeeded: bool,
}

impl<T> ExternResult<T> where T : Default {
    pub fn from_result(r: anyhow::Result<T>) -> Self {
        match r {
            Ok(c) => {
                ExternResult {
                    content: c,
                    error_message: std::ptr::null_mut(),
                    succeeded: true
                }
            },
            Err(e) => {
                ExternResult {
                    content: T::default(),
                    error_message: return_str(e.to_string()),
                    succeeded: false
                }
            }
        }
    }
}

pub fn extract_str<'a>(str: *const c_char) -> anyhow::Result<&'a str> {
    let c_str = unsafe { CStr::from_ptr(str) };

    let result = c_str.to_str()?;
    Ok(result)
}

pub fn return_str(str: String) -> *mut c_char {
    let returned_c_str = match CString::new(str) {
        Ok(s) => s,
        Err(_) => {
            return std::ptr::null_mut();
        }
    };

    returned_c_str.into_raw()
}

#[unsafe(no_mangle)]
pub extern "C" fn free_rust_string(ptr: *mut c_char) {
    if ptr.is_null() {
        return;
    }

    unsafe {
        // Retake the ownership of string and drop it.
        let _ = CString::from_raw(ptr);
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn process_string(str: *const c_char) -> *mut c_char {
    let rust_str = match extract_str(str) {
        Ok(s) => s,
        Err(e) => {
            return return_str(format!("Encounter error: {e}"))
        }
    };

    let mut returned_str = String::new();
    returned_str.push_str("Process ");
    returned_str.push_str(rust_str);

    return_str(returned_str)
}

pub fn into_handler<T>(handler: Box<T>) -> *mut c_void {
    Box::into_raw(handler) as *mut c_void
}

pub fn from_handler<'a, T>(handler: *mut c_void) -> &'a mut T {
    unsafe {
        &mut *(handler as *mut T)
    }
}
