# PentaWork.Tools.PowerPlatform

A cross-platform [.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) CLI (`pw-pp`) for Power Platform / Dataverse automation: solution export/import, entity export/import, sharings, relations, web resource/plugin assembly updates, C#/TypeScript proxy generation and plugin call-graph analysis.

Built on `net10.0` and [`Microsoft.PowerPlatform.Dataverse.Client`](https://www.nuget.org/packages/Microsoft.PowerPlatform.Dataverse.Client); runs natively on Linux, macOS and Windows.

## Installation

Install globally:

```
dotnet tool install --global PentaWork.Tools.PowerPlatform
pw-pp --version
```

Or install locally into a repository's [tool manifest](https://learn.microsoft.com/dotnet/core/tools/local-tools-how-to-use):

```
dotnet new tool-manifest   # if one doesn't exist yet
dotnet tool install PentaWork.Tools.PowerPlatform
dotnet tool run pw-pp -- --version
```

Update / uninstall:

```
dotnet tool update --global PentaWork.Tools.PowerPlatform
dotnet tool uninstall --global PentaWork.Tools.PowerPlatform
```

## Connecting

Every command needs a [Dataverse connection string](https://learn.microsoft.com/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect). Provide it either way:

```
pw-pp connection test --connection-string "AuthType=ClientSecret;Url=https://contoso.crm.dynamics.com;ClientId=...;ClientSecret=..."

# or via environment variable, checked when --connection-string is omitted
export PW_PP_CONNECTION_STRING="AuthType=ClientSecret;Url=https://contoso.crm.dynamics.com;ClientId=...;ClientSecret=..."
pw-pp connection test
```

`--connection-string` always wins over `PW_PP_CONNECTION_STRING` when both are set.

## Global options

Available on every command:

| Option | Description |
|---|---|
| `--connection-string <value>` | Dataverse connection string (falls back to `PW_PP_CONNECTION_STRING`). |
| `--timeout-minutes <n>` | Connection timeout in minutes (default `4`). |
| `--verbose` | Write verbose diagnostic logs to stderr. |

## Command reference

| Command | Description |
|---|---|
| `connection test` | Verify the connection string resolves and authenticates. |
| `solution list` | List installed, visible solutions. |
| `solution export <unique-name> --output-dir <dir> [--extract-dir <dir> --clean]` | Export a solution to a zip file (optionally extracted). |
| `solution import <zip>` | Import a solution from a zip file. |
| `entity export <logical-name>` | Export entity instances (with columns, filters, relations, sharings) to JSON. |
| `entity import` | Import entity instances from JSON. |
| `entity remove <logical-name>` | **Destructive.** Delete entity instances matching a filter. |
| `data export --config <file> --output-dir <dir>` | Export configured entity data sets into one JSON file per item. |
| `data import --config <file> --input-dir <dir> --fallback-owner-type <type> --fallback-owner-id <guid>` | Import configured entity data sets. |
| `relation import` | Import N:M relations from JSON. |
| `sharing import` | Import entity sharings (POA) from JSON. |
| `user-object list` | List per-user views, dashboards and charts. |
| `proxy generate [--csharp-output-dir <dir>] [--fake-output-dir <dir>] [--typescript-output-dir <dir>]` | Generate C#/TypeScript proxy and FakeXrmEasy fake classes from live metadata. |
| `plugin-graph export` | Analyze plugin call graphs and render Markdown reports. |
| `assembly update <dll>` | Update a plugin assembly's content/version. |
| `webresource update <file> --name <name>` | Create or update a web resource. |

Run `pw-pp <command> --help` or `pw-pp <command> <subcommand> --help` for the full option list of any command.

## Destructive commands

`entity remove`, `solution import --delete-existing`, `proxy generate --clear` and `plugin-graph export --clear` execute immediately, without an interactive confirmation prompt - the same as the automation logic they're built on. Double-check the target environment before running them, especially in scripts.

`--clear` additionally refuses to delete the filesystem root or the current working directory, as a guard against a bad or empty `--output-dir`.

## Input/output conventions

- Commands that produce JSON (`entity export`, `solution list`, `user-object list`, ...) write it to **stdout** by default, pretty-printed, PascalCase properties, SDK enums as their raw numeric value. Pass `--output <file>` (or `-o`) to write to a file instead; `-` explicitly means stdout.
- Commands that consume JSON (`entity import`, `relation import`, `sharing import`) read it from **stdin** by default. Pass `--input <file>` (or `-i`) to read from a file instead; `-` explicitly means stdin.
- All logs, warnings and progress go to **stderr**, so stdout always stays parseable.

### Pipeline example

```
pw-pp entity export userquery --connection-string "$SOURCE" > userquery.json

cat userquery.json | pw-pp entity import \
  --connection-string "$TARGET" \
  --fallback-owner-type systemuser \
  --fallback-owner-id 00000000-0000-0000-0000-000000000001 \
  --map-by-name
```

### Data workflow configuration

`data export` and `data import` use a shared configuration file (JSON) that describes which entities to
process and how. All arrays are optional and may be empty. `FileName` must not contain a file extension
or a path — it is used as the basename for the exported/imported JSON file.

```json
{
  "Items": [
    {
      "FileName": "01 - Countries",
      "LogicalName": "new_country",
      "Filters": ["statecode:Equal:0"],
      "IgnoreColumns": ["ownerid"],
      "Relations": ["new_country_new_region"],
      "Sharings": false,
      "MapByName": true,
      "MapByNameLookups": ["transactioncurrencyid"],
      "CreateOnly": [],
      "ImpersonateOwner": false,
      "PurgeTarget": false
    }
  ]
}
```

| Field | Description |
|---|---|
| `FileName` | Basename for the export/import JSON file (no extension, no path). |
| `LogicalName` | The target entity logical name. |
| `Filters` | [Filter triples](#filter-triples) applied on export. |
| `IgnoreColumns` | Columns to skip on export. |
| `Relations` | Relation schema names to follow on export and re-import from the source JSON. |
| `Sharings` | `true` to export/import shared access (POA) records. |
| `MapByName` | `true` activates `MapByName` during import; also enables `TakeFirst` fallback. |
| `MapByNameLookups` | Specific lookup columns that should be resolved by name during import. |
| `CreateOnly` | Columns that are only set on create, never on update. |
| `ImpersonateOwner` | `true` to preserve the original owner on import. |
| `PurgeTarget` | `true` to delete all existing instances of this entity before import. |

### Examples

**Data export workflow:**

```
pw-pp data export --config config.json --output-dir ./data --connection-string "$SOURCE"
```

**Data import workflow:**

```
pw-pp data import --config config.json --input-dir ./data --fallback-owner-type systemuser --fallback-owner-id 00000000-0000-0000-0000-000000000001 --connection-string "$TARGET"
```

**Solution export with automatic extraction:**

```
pw-pp solution export MySolution --output-dir ./solutions --extract-dir ./solutions/extracted --clean --connection-string "$SOURCE"
```

**Proxy generation with explicit output targets:**

```
pw-pp proxy generate --proxy-namespace My.Proxies --fake-namespace My.Fakes --output-dir ./staging --csharp-output-dir ./src/Proxies --fake-output-dir ./tests/Fakes --typescript-output-dir ./ts/Proxies --clear --connection-string "$SOURCE"
```

Each proxy output option is independent: an explicitly supplied target is used for that artifact type,
while omitted C#, fake, or TypeScript targets fall back to `CS/`, `Fake/`, or `TS/` below `--output-dir`.
Each effective target is independently validated by `--clear` against the root/workspace guard — the
guard never allows deleting the filesystem root or the current working directory.

`solution export --extract-dir <dir>` preserves unrelated existing files in `<dir>` and overwrites
files included in the ZIP. Add `--clean` to delete `<dir>` first; `--clean` requires `--extract-dir`
and uses the same root/workspace safety guard as the other destructive output options.

### Filter triples

`entity export`/`entity remove --filter` and `relation import --relation-condition` take colon-separated triples:

```
--filter statecode:Equal:0
--filter name:In:Contoso|Fabrikam
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `2` | Validation error (bad arguments/options) |
| `3` | Connection error |
| `4` | Dataverse or file-system error |
| `5` | Partial batch failure (some items in a batch operation failed) |

## Building from source

```
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack src/PentaWork.Tools.PowerPlatform -c Release -o ./nupkg
```

Continuous integration builds and tests on Ubuntu, Windows and macOS (see `.github/workflows/ci.yml`).

## License

[MIT](LICENSE)
