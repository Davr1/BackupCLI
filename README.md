# BackupCLI
Windows client for backing up local directories at predefined intervals with a simple command line interface.

## Installation
There is no setup required.

### Prebuilt binaries
| [Portable](http://gitlab.com/sssvt-all/studenti/2021bsk1/sssvt-rizzieri-david-g/backupcli/-/jobs/artifacts/master/raw/BackupCLI_Portable.exe?job=build) | [Standalone](http://gitlab.com/sssvt-all/studenti/2021bsk1/sssvt-rizzieri-david-g/backupcli/-/jobs/artifacts/master/raw/BackupCLI_Standalone.exe?job=build) |
|-|-|
| Requires .NET 8.0, ~5.5 MB | Comes bundled with .NET, ~80 MB |

## Usage
- `-c, --config` Path to a json config file
- `-l, --log` Path to a log file (default: `latest.log`)
- `-q, --quiet` Suppress console logs

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