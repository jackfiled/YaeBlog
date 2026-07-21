use std::path::PathBuf;
use std::sync::LazyLock;
use typst::diag::{FileError, FileResult, PackageError};
use typst::foundations::{Bytes, Datetime, Duration};
use typst::syntax::{FileId, RootedPath, Source, VirtualPath, VirtualRoot};
use typst::text::{Font, FontBook};
use typst::utils::LazyHash;
use typst::{Feature, Features, Library, LibraryExt, World};
use typst_kit::datetime::Time;
use typst_kit::files::{FileLoader, FileStore, FsRoot};
use typst_kit::fonts::{FontStore, embedded};

pub struct SystemWorld {
    work_dir: PathBuf,
    library: LazyHash<Library>,
    files: FileStore<SystemFiles>,
    fonts: LazyLock<FontStore, Box<dyn Fn() -> FontStore + Send + Sync>>,
    now: Time,
}

const DEFAULT_FEATURES: [Feature; 1] = [Feature::Html];

impl SystemWorld {
    pub fn new(work_dir: PathBuf) -> Self {
        let features = DEFAULT_FEATURES.iter().copied().map(Into::into).collect();
        let library = Library::builder().with_features(features).build();

        Self {
            work_dir: work_dir.clone(),
            library: LazyHash::new(library),
            files: FileStore::new(SystemFiles::new(work_dir)),
            fonts: LazyLock::new(Box::new(|| {
                let mut fonts = FontStore::new();

                fonts.extend(embedded());

                fonts
            })),
            now: Time::system(),
        }
    }

    pub fn set_current_file(&mut self, content: &str) {
        let current_file = &mut self.files.loader_mut().current_file;
        let bytes = content.as_bytes();

        current_file.clear();
        current_file.resize(bytes.len(), 0);
        current_file.copy_from_slice(content.as_bytes())
    }
}

impl World for SystemWorld {
    fn library(&self) -> &LazyHash<Library> {
        &self.library
    }

    fn book(&self) -> &LazyHash<FontBook> {
        self.fonts.book()
    }

    fn main(&self) -> FileId {
        *MAIN_ID
    }

    fn source(&self, id: FileId) -> FileResult<Source> {
        self.files.source(id)
    }

    fn file(&self, id: FileId) -> FileResult<Bytes> {
        self.files.file(id)
    }

    fn font(&self, index: usize) -> Option<Font> {
        self.fonts.font(index)
    }

    fn today(&self, offset: Option<Duration>) -> Option<Datetime> {
        self.now.today(offset)
    }
}

/// Provides project files from a configured directory and package files from
/// standard locations.
struct SystemFiles {
    pub main: FileId,
    project: FsRoot,
    current_file: Vec<u8>,
}

static MAIN_ID: LazyLock<FileId> = LazyLock::new(|| {
    FileId::unique(RootedPath::new(
        VirtualRoot::Project,
        VirtualPath::new("<main>").unwrap(),
    ))
});

impl SystemFiles {
    pub fn new(work_dir: PathBuf) -> Self {
        Self {
            main: *MAIN_ID,
            project: FsRoot::new(work_dir),
            current_file: Vec::new(),
        }
    }

    pub fn resolve(&self, id: FileId) -> FileResult<PathBuf> {
        Ok(self.root(id)?.resolve(id.vpath())?)
    }

    fn root(&self, id: FileId) -> FileResult<FsRoot> {
        Ok(match id.root() {
            VirtualRoot::Project => Ok(self.project.clone()),
            VirtualRoot::Package(_) => Err(FileError::Package(PackageError::Other(None))),
        }?)
    }
}

impl FileLoader for SystemFiles {
    fn load(&self, id: FileId) -> FileResult<Bytes> {
        if id == *MAIN_ID {
            Ok(Bytes::new(self.current_file.clone()))
        } else {
            self.root(id)?.load(id.vpath())
        }
    }
}
