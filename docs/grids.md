# Grid resource configuration

Some ProjNET transformations require external grid files instead of only numeric
parameters. This guide explains which formats are supported, how the resolver
searches for grid files, and how to configure local and network-backed setups.

## Supported grid formats

| Format | Typical extension | Primary use |
| --- | --- | --- |
| NTv2 | `.gsb` | Horizontal grid shifts |
| GTX | `.gtx` | Vertical grid shifts |
| GeoTIFF | `.tif` | Horizontal, vertical, and xyz grid-backed shifts |

## Resolution order

When ProjNET needs a grid file, it resolves it in this order:

1. In-memory cache of previously resolved grid names.
2. Direct rooted path if the requested grid name is already an absolute file path.
3. Configured local directories, matched by file name.
4. Optional network fetch into the configured cache directory when
   `GridResourceResolutionMode.LocalThenNetwork` is active.

Successful resolutions are cached for later reuse. Network-fetched files are
written into the cache directory together with a manifest so stale or partial
downloads can be detected and discarded.

## Environment variables

`CoordinateTransformationFactory.ConfigureGridResolution()` uses these variables
when you call it with no arguments, and the default process-wide resolver also
reads them on startup:

| Variable | Meaning |
| --- | --- |
| `PROJNET_GRID_PATHS` | List of local search directories. Empty means no configured local directories. |
| `PROJNET_GRID_CACHE` | Cache directory for downloaded grid files. Required for network-backed resolution to succeed. |
| `PROJNET_GRID_MODE` | Resolution mode. `LocalThenNetwork` and `network` enable network fallback; any other value behaves as `LocalOnly`. |
| `PROJNET_GRID_REQUIRED` | Fail-fast mode for required grids. `1`, `true`, and `yes` force a `DataUnavailable:` exception when a required grid cannot be resolved. |
| `PROJNET_GRID_BASE_URL` | Absolute base URL used by `HttpGridResourceFetchClient` when network mode is enabled. |

### Local-only PowerShell setup

```powershell
$env:PROJNET_GRID_PATHS = 'C:\projnet\grids;D:\shared\projnet-grids'
$env:PROJNET_GRID_MODE = 'LocalOnly'
$env:PROJNET_GRID_CACHE = $null
$env:PROJNET_GRID_BASE_URL = $null
```

### Local-then-network PowerShell setup

```powershell
$env:PROJNET_GRID_PATHS = 'C:\projnet\grids'
$env:PROJNET_GRID_CACHE = 'C:\projnet\grid-cache'
$env:PROJNET_GRID_MODE = 'network'
$env:PROJNET_GRID_BASE_URL = 'https://cdn.proj.org/'
```

## Programmatic configuration

Use the public static configuration API when you want to control the resolver in
application startup code instead of depending on process environment state.

```csharp
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Resources;

CoordinateTransformationFactory.ConfigureGridResolution(
    new HttpGridResourceFetchClient("https://cdn.proj.org/"),
    new[] { @"C:\projnet\grids" },
    @"C:\projnet\grid-cache",
    GridResourceResolutionMode.LocalThenNetwork);
```

To rebuild the resolver from the current environment variables, call:

```csharp
using ProjNet.CoordinateSystems.Transformations;

CoordinateTransformationFactory.ConfigureGridResolution();
```

## Local and network deployment patterns

### Prefer local files for deterministic production deployments

Ship the grid files alongside your application or mount them into a known
directory. Then set `PROJNET_GRID_PATHS` (or pass `localDirectories`) and keep
`GridResourceResolutionMode.LocalOnly`. This is the most predictable setup for
CI, containers, and offline services.

### Use cache + network for developer convenience or managed refresh

When you want a PROJ-style fetch-on-demand experience, configure:

- one or more local directories for pre-seeded files,
- a writable cache directory,
- `LocalThenNetwork` / `network` mode, and
- an absolute base URL such as `https://cdn.proj.org/`.

ProjNET downloads only by file name into the cache, validates the cached manifest,
and reuses the cached copy on subsequent resolutions.

## Required-grid behavior

By default, ProjNET can still create a transformation when a non-grid fallback
operation exists for the same CRS pair. If the only available path depends on a
missing grid, the transformation cannot be created.

Set `PROJNET_GRID_REQUIRED=true` when your application must not silently accept a
lower-fidelity fallback. In that mode, missing required grids raise an
`InvalidOperationException` whose message starts with `DataUnavailable:`.

## Practical notes

- A rooted grid path such as `C:\projnet\grids\BETA2007.gsb` is used directly.
- Relative grid names are resolved by file name against the configured local
  directories or cache directory.
- Network mode without `PROJNET_GRID_CACHE` (or `cacheDirectory`) does not have a
  writable destination, so downloads will not succeed.
- The built-in default fetch client is a no-op; network fetching only happens when
  an HTTP fetch client is configured explicitly or created from the environment.
