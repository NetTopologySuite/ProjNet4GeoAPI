# Migrating to ProjNET v3

ProjNET v3 makes the coordinate-system object model **immutable**. This is the main intentional breaking change in the v3 line and the foundation for safer sharing, simpler reasoning, and follow-up runtime simplifications.

## Who needs to change code

You need source changes if your code previously **mutated** objects returned by:

- `CoordinateSystemServices`
- `CoordinateSystemFactory`
- WKT/WKT2 parsing
- static convenience accessors such as `GeographicCoordinateSystem.WGS84`, `HorizontalDatum.WGS84`, or `ProjectedCoordinateSystem.WebMercator`

If your code only reads properties, serializes WKT/XML, or creates transformations from existing coordinate systems, you usually do **not** need changes.

## Breaking changes at a glance

| Area | v2-style usage | v3 behavior | Migration path |
| --- | --- | --- | --- |
| `Info` metadata | `Name`, `Authority`, `AuthorityCode`, `Alias`, `Abbreviation`, `Remarks` were mutable | read-only after construction | `WithName(...)`, `WithAuthority(...)`, or rebuild via constructor/factory |
| Datum metadata | `Ensemble` was mutable | read-only after construction | `WithEnsemble(...)` |
| Horizontal datum conversion | `Wgs84Parameters` was mutable | read-only after construction | `WithWgs84Parameters(...)` |
| Temporal datum | `TimeOrigin` was mutable | read-only after construction | create a new `TemporalDatum` |
| CRS composition | datum/unit/prime-meridian/projection references were mutable | read-only after construction | build the desired object up front or clone through `With...` APIs where available |
| Projection/parameter metadata | several public value holders exposed setters | read-only after construction | replace in-place edits with new instances |

The exact public signature changes are tracked in `src/ProjNet/PublicAPI.Shipped.txt`.

## Why v3 made this break

- Coordinate-system graphs can now be treated as **stable values** instead of partially mutable bags of state.
- Shared catalog/static instances are safer to reuse because callers can no longer mutate them after retrieval.
- Immutability gives a clear basis for the v3 thread-safety story.
- Follow-up internal work can remove more defensive cloning and mutation-oriented plumbing without changing the public programming model again.

## Common migrations

### 1. Authority + authority code

**Before**

```csharp
var projected = (ProjectedCoordinateSystem)factory.CreateFromWkt(wkt);
projected.Authority = "EPSG";
projected.AuthorityCode = 28992;
```

**After**

```csharp
var projected = (ProjectedCoordinateSystem)factory.CreateFromWkt(wkt);
projected = (ProjectedCoordinateSystem)projected.WithAuthority("EPSG", 28992);
```

### 2. Renaming an existing object

**Before**

```csharp
var unit = LinearUnit.Metre;
unit.Name = "Meter";
```

**After**

```csharp
var unit = (LinearUnit)LinearUnit.Metre.WithName("Meter");
```

### 3. Replacing Bursa-Wolf parameters

**Before**

```csharp
var datum = HorizontalDatum.ED50;
datum.Wgs84Parameters = new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0);
```

**After**

```csharp
var datum = HorizontalDatum.ED50.WithWgs84Parameters(
    new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0));
```

### 4. Updating retained ensemble metadata

**Before**

```csharp
var datum = HorizontalDatum.WGS84;
datum.Ensemble = ensemble;
```

**After**

```csharp
var datum = (HorizontalDatum)HorizontalDatum.WGS84.WithEnsemble(ensemble);
```

### 5. Creating a renamed + re-identified copy

**Before**

```csharp
var geographic = GeographicCoordinateSystem.WGS84;
geographic.Name = "Custom WGS 84";
geographic.Authority = "TEST";
geographic.AuthorityCode = 1001;
```

**After**

```csharp
var geographic = (GeographicCoordinateSystem)GeographicCoordinateSystem.WGS84
    .WithName("Custom WGS 84")
    .WithAuthority("TEST", 1001);
```

### 6. Changing a temporal datum time origin

There is intentionally **no** `WithTimeOrigin(...)` helper in v3. Create a new temporal datum with the desired origin.

**Before**

```csharp
var datum = new TemporalDatum("1970-01-01T00:00:00Z", "Unix epoch", "EPSG", 1040, "", "", "");
datum.TimeOrigin = "2000-01-01T00:00:00Z";
```

**After**

```csharp
var original = new TemporalDatum("1970-01-01T00:00:00Z", "Unix epoch", "EPSG", 1040, "", "", "");
var updated = new TemporalDatum(
    "2000-01-01T00:00:00Z",
    original.Name,
    original.Authority,
    original.AuthorityCode,
    original.Alias,
    original.Remarks,
    original.Abbreviation);
```

### 7. Replacing subclasses of newly sealed types

The following public types are now sealed in v3:

- `AffineTransform`
- `CoordinateTransformation`
- `GeographicTransform`
- `ProjectionParameterSet`

If you previously inherited from them, switch to composition instead:

| Sealed type | Typical reason for subclassing | Migration path |
| --- | --- | --- |
| `AffineTransform` | custom affine runtime behavior | derive from `MathTransform` for a custom transform, or wrap an `AffineTransform` instance and delegate to it |
| `CoordinateTransformation` | attach custom metadata or behavior to a resolved transformation | create your own wrapper around `ICoordinateTransformation` / `ICoordinateTransformationCore` instead of inheriting |
| `GeographicTransform` | specialize datum-shift runtime behavior | implement a custom `MathTransform` and plug it into your own transformation pipeline |
| `ProjectionParameterSet` | attach helper methods or validation to the parameter dictionary | keep a separate helper/wrapper type and construct or copy a `ProjectionParameterSet` where the ProjNET APIs require one |

The practical v3 rule is: **treat these runtime/container types as finished building blocks, not inheritance extension points**.

### 8. Replacing removed `MapProjection` protected helpers

Several legacy `protected static` helpers that older custom projections sometimes called directly are no longer part of the v3 surface.

| Removed helper | v3 replacement |
| --- | --- |
| `phi2z(...)` | use `Phi2z(...)` |
| `sign(...)` | use `Sign(...)` |
| `msfnz(...)` | use `Msfnz(...)` |
| `e0fn(...)` / `e1fn(...)` / `e2fn(...)` / `e3fn(...)` / `e4fn(...)` | use the precomputed meridional-series fields already maintained by `MapProjection` (`en0` … `en4`) together with `Mlfn(...)`, or copy the coefficient math locally if you were computing them outside a projection instance |
| `CUBE(x)` | replace with the direct expression `x * x * x` or a local helper in your own derived type |

If you own custom projections, the safest migration is usually to rename direct PascalCase replacements first (`Phi2z`, `Sign`, `Msfnz`), then do a small manual rewrite for the removed coefficient/cube helpers.

### 9. Updating manual `CoordinateSystemServices` enumeration

`CoordinateSystemServices.GetEnumerator()` now returns `IEnumerator<CoordinateSystemEntry>` instead of
`IEnumerator<KeyValuePair<int, CoordinateSystem>>`.

This only affects code that explicitly stores or types the enumerator/current item. Plain `foreach`
usage continues to work, but the item type is now `CoordinateSystemEntry` with named `Srid` and
`CoordinateSystem` properties.

**Before**

```csharp
IEnumerator<KeyValuePair<int, CoordinateSystem>> enumerator = services.GetEnumerator();
while (enumerator.MoveNext())
{
    KeyValuePair<int, CoordinateSystem> current = enumerator.Current;
    Console.WriteLine($"{current.Key}: {current.Value.Name}");
}
```

**After**

```csharp
IEnumerator<CoordinateSystemEntry> enumerator = services.GetEnumerator();
while (enumerator.MoveNext())
{
    CoordinateSystemEntry current = enumerator.Current;
    Console.WriteLine($"{current.Srid}: {current.CoordinateSystem.Name}");
}
```

### 10. Updating constructor, parsing, and serialization assumptions

Several smaller API and output changes can require targeted source updates:

| Area | v2-style assumption | v3 behavior | Migration path |
| --- | --- | --- | --- |
| `CoordinateSystemServices` seeded definitions | constructors accepted `IEnumerable<KeyValuePair<int, string>>` | constructors now accept `IEnumerable<CoordinateSystemDefinition>` | wrap each SRID/WKT pair in `new CoordinateSystemDefinition(srid, wkt)` |
| `CoordinateSystemFactory.CreateFromWkt(...)` | return value was treated as always non-null | return type is `CoordinateSystem?` | null-check or use `?? throw` when your input must be a coordinate system |
| `[Serializable]` on model/runtime types | legacy binary serialization attributes were available | `[Serializable]` was removed from the public surface | switch persistence/integration code to WKT/WKT2/XML or your own DTOs |
| `VerticalDatum.WKT` | emitted `DATUM[...]` in vertical coordinate system output | emits `VERT_DATUM[...]` | update string comparisons, snapshots, and custom parsers to the vertical-specific keyword |

**Before**

```csharp
var definitions = new[]
{
    new KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT),
};

var services = new CoordinateSystemServices(definitions);
CoordinateSystem parsed = factory.CreateFromWkt(wkt);
```

**After**

```csharp
var definitions = new[]
{
    new CoordinateSystemDefinition(4326, GeographicCoordinateSystem.WGS84.WKT),
};

var services = new CoordinateSystemServices(definitions);
CoordinateSystem parsed = factory.CreateFromWkt(wkt)
    ?? throw new InvalidOperationException("Expected a coordinate system WKT.");
```

If you previously depended on `[Serializable]`, treat that as a required migration off legacy binary
serialization rather than a drop-in attribute rename.

## Important note about return types

`WithAuthority(...)` and `WithName(...)` are declared on `Info`, and `WithEnsemble(...)` is declared on `Datum`. They preserve the **concrete runtime type**, but their declared return types are the base types:

- `Info.WithAuthority(...)` -> `Info`
- `Info.WithName(...)` -> `Info`
- `Datum.WithEnsemble(...)` -> `Datum`

That means callers commonly cast back to the expected subtype:

```csharp
var projected = (ProjectedCoordinateSystem)parsed.WithAuthority("EPSG", 28992);
var renamed = (PrimeMeridian)PrimeMeridian.Greenwich.WithName("Custom Greenwich");
var datum = (HorizontalDatum)HorizontalDatum.WGS84.WithEnsemble(ensemble);
```

If you prefer an assertion-style guard in tests, `Assert.IsType<T>(...)` is a good fit.

## Practical search-and-replace checklist

These searches find the vast majority of v2 mutation sites:

```powershell
rg '\.Authority\s*=' -g '*.cs'
rg '\.AuthorityCode\s*=' -g '*.cs'
rg '\.Name\s*=' -g '*.cs'
rg '\.Wgs84Parameters\s*=' -g '*.cs'
rg '\.Ensemble\s*=' -g '*.cs'
rg '\.TimeOrigin\s*=' -g '*.cs'
```

Recommended replacements:

| Search hit | Typical replacement |
| --- | --- |
| `.Authority = ...` + `.AuthorityCode = ...` | replace the pair with `value = (T)value.WithAuthority(authority, code);` |
| `.Name = ...` | replace with `value = (T)value.WithName(name);` |
| `.Wgs84Parameters = ...` | replace with `value = value.WithWgs84Parameters(...);` |
| `.Ensemble = ...` | replace with `value = (T)value.WithEnsemble(...);` |
| `.TimeOrigin = ...` | replace with a new `TemporalDatum(...)` instance |

Because the old setter patterns often span multiple lines and variable names differ from file to file, a **guided manual pass** is safer than trying to force a single bulk regex replacement across the whole codebase.

## What does not change

- `CoordinateSystemServices` remains the main entry point for EPSG lookup and transformation creation.
- WKT/WKT2 parsing and serialization stay available.
- `EqualParams(...)` semantics remain metadata-insensitive unless the changed value is part of the actual coordinate-system definition.
- The `With...` helpers preserve the concrete runtime type of the cloned object.

## Recommended migration strategy

1. First replace obvious setter pairs (`Authority` + `AuthorityCode`, `Name`, `Wgs84Parameters`, `Ensemble`, `TimeOrigin`).
2. Then compile and fix any remaining setter-based call sites one by one.
3. Prefer replacing post-construction mutation with constructor/factory composition when the target object is built locally anyway.
4. Use the `With...` helpers when adapting parsed/catalog objects that should keep the rest of their definition unchanged.

## v3 takeaway

The v3 model treats coordinate-system objects as **values**: build them once, clone intentionally when metadata must differ, and then share them safely.
