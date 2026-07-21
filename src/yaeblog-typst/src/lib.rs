mod ffi_utils;
mod world;

use std::ffi::{c_char, c_void};
use crate::ffi_utils::ExternResult;

pub struct TypstCompiler {

}

pub extern "C" fn init_compiler(work_dir: *const c_char) -> ExternResult<*mut c_void> {
    todo!()
}

pub extern "C" fn typst_compile(handler: *mut c_void, str: *const c_char) -> ExternResult<*mut c_char> {
    todo!()
}
