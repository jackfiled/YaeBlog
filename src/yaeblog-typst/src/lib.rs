mod ffi_utils;
mod world;

use crate::ffi_utils::{ExternResult, extract_str, from_handler, into_handler, return_str};
use crate::world::SystemWorld;
use anyhow::anyhow;
use std::ffi::{c_char, c_void};
use std::path::PathBuf;
use std::str::FromStr;
use typst::diag::{EcoVec, SourceDiagnostic, Warned};
use typst_html::{HtmlDocument, HtmlOptions};

struct TypstCompiler {
    world: SystemWorld,
}

impl TypstCompiler {
    fn new(work_dir: &str) -> anyhow::Result<Self> {
        let path = PathBuf::from_str(work_dir)?;

        Ok(Self {
            world: SystemWorld::new(path),
        })
    }

    fn compile(&mut self, input: &str) -> anyhow::Result<String> {
        self.world.set_current_file(input);

        let Warned { output, warnings } = typst::compile::<HtmlDocument>(&mut self.world);
        let document = match output {
            Ok(d) => Ok(d),
            Err(e) => Err(anyhow!(typst_to_err(e))),
        }?;

        let content = export_html(&document)?;
        Ok(content)
    }
}

fn typst_to_err(e: EcoVec<SourceDiagnostic>) -> String {
    let mut message = String::new();
    message.push_str("Failed to compile: \n");

    for err in e {
        message.push_str(format!("  - {} \n", err.message.as_str()).as_str());
    }

    message
}

fn export_html(document: &HtmlDocument) -> anyhow::Result<String> {
    let options = HtmlOptions { pretty: false };

    match typst_html::html(document, &options) {
        Ok(o) => Ok(o),
        Err(e) => Err(anyhow!(typst_to_err(e))),
    }
}

fn create_compiler(work_dir: *const c_char) -> anyhow::Result<Box<TypstCompiler>> {
    let work_dir = extract_str(work_dir)?;
    let compiler = TypstCompiler::new(work_dir)?;

    Ok(Box::new(compiler))
}

pub extern "C" fn init_compiler(work_dir: *const c_char) -> ExternResult<*mut c_void> {
    let compiler = create_compiler(work_dir);
    let handler = compiler.map(into_handler);

    ExternResult::from_result(handler)
}

fn typst_compile_inner(
    compiler: &mut TypstCompiler,
    str: *const c_char,
) -> anyhow::Result<*mut c_char> {
    let input = extract_str(str)?;
    compiler.compile(input).map(return_str)
}

pub extern "C" fn typst_compile(
    handler: *mut c_void,
    str: *const c_char,
) -> ExternResult<*mut c_char> {
    let compiler = from_handler::<TypstCompiler>(handler);

    ExternResult::from_result(typst_compile_inner(compiler, str))
}

pub extern "C" fn free_compiler(handler: *mut c_void) {
    if handler.is_null() {
        return;
    }

    unsafe {
        // Drop it!
        let _ = Box::from_raw(handler as *mut TypstCompiler);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn smoke_test() {
        let mut compiler = TypstCompiler::new(".").unwrap();
        let result = compiler.compile("= 123").unwrap();

        assert!(result.contains("<h2>123</h2>"))
    }

    #[test]
    fn math_test() {
        let mut compiler = TypstCompiler::new(".").unwrap();
        let result = compiler.compile(
            "$1 + 1 = 2$").unwrap();

        assert!(result.contains("math"));
    }
}
