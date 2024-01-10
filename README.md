# BackupCLI
Windows client for backing up local directories at predefined intervals with a simple command line interface.

## Installation
To build the project, run `dotnet publish -p:PublishProfile=Build` (requires .NET 8.0)

## Usage
- `-c` / `--config` - Path to a json config file
- `-l` / `--log` - Path to a log file (default: `latest.log`)
- `-q` / `--quiet` - Suppress console logs

### Configuration
Multiple backup jobs can be specified in the config file.
Each backup job must follow this schema:
```json
{
    "sources": [ ... ],
    "targets": [ ... ],
    "method": "full" | "incremental" | "differential",
    "timing": "<cron>",
    "retention": {
        "count": int,
        "size": int
    }
}
```
### Timing
5, 6, and 7 place cron expressions are supported

### Backup methods
| Method | Package contents | Backup speed | Indexing |
|-|-|-|-|
| Full | same as source | slow | fast |
| Differential | full + changes since the first backup | faster during subsequent backups | fast |
| Incremental | full + changes since the last backup | faster than diff over a longer time frame | slower with each backup |

## Output
All source folders are copied to all target folders as self-contained packages.\
Every time a backup job runs, a new backup folder is created inside the latest package located in the target folder.
If a package outgrows `size`, a new package is created. Only the last `count` of packages are kept - older are deleted.

### metadata.json
Automatically generated - contains information about the packages in the current folder
```json
{
  "packages": [
    "8DBFE370D22352E",
    "8DBFE393776FB2A"
  ],
  "currentPackage": "8DBFE393776FB2A"
}
```

### package.json
Automatically generated - contains information about the current package
```json
{
  "paths": {
    "C:\\autodesk\\": "6658FFAC49DE6ACD72BA5F6C5A9C8702"
  },
  "backups": [
    "FULL",
    "INCR-8DBFE4204B08633"
  ],
  "lastWriteTime": "2023-12-16T14:19:30.3042593+01:00"
}
```
