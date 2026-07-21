use std::path::PathBuf;
use std::sync::LazyLock;
use typst::utils::LazyHash;
use typst::{Library, World};
use typst::diag::FileResult;
use typst::foundations::{Bytes, Datetime, Duration};
use typst::syntax::{FileId, Source};
use typst::text::{Font, FontBook};

pub struct SystemWorld {
    work_dir: PathBuf,
    library: LazyHash<Library>,
}

impl World for SystemWorld {
    fn library(&self) -> &LazyHash<Library> {
        todo!()
    }

    fn book(&self) -> &LazyHash<FontBook> {
        todo!()
    }

    fn main(&self) -> FileId {
        todo!()
    }

    fn source(&self, id: FileId) -> FileResult<Source> {
        todo!()
    }

    fn file(&self, id: FileId) -> FileResult<Bytes> {
        todo!()
    }

    fn font(&self, index: usize) -> Option<Font> {
        todo!()
    }

    fn today(&self, offset: Option<Duration>) -> Option<Datetime> {
        todo!()
    }
}
