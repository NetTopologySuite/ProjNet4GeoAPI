# NOTICE

This project is distributed under `LGPL-2.1-or-later`.

## Attribution chain

- **ProjNet4GeoAPI original implementation**
  - Copyright 2005-2009 Morten Nielsen and contributors.
  - Distributed under LGPL-2.1-or-later.

- **GeoTools.NET / Urban Science derived portions**
  - Includes code historically attributed to Urban Science Applications, Inc.
  - Distributed under LGPL-compatible terms in the ProjNET lineage.

- **PROJ-derived implementation work**
  - Portions of the current projection/transformation implementation are derived from PROJ.
  - Upstream PROJ material is provided under the PROJ-specific MIT license text in
    `LICENSES/PROJ-MIT.txt`.

- **Vendored grid data from the OSGeo PROJ data CDN**
  - `NKG`, `eur_nkg_nkgrf03vel_realigned.tif`, and `eur_nkg_nkgrf17vel.tif` originate from the Nordic Geodetic Commission / NordicTransformations `eur_nkg` data family distributed via `https://cdn.proj.org/`.
  - `no_kv_NKGETRF14_EPSG7922_2000.tif` originates from the Kartverket `no_kv` data family distributed via `https://cdn.proj.org/`.
  - These CDN-distributed data files are provided under the CC BY 4.0 license.
  - The vendored `no_kv_NKGETRF14_EPSG7922_2000.tif` repository copy is a lossless strip-based rewrite of the official CDN GeoTIFF because the current ProjNET GeoTIFF reader requires strip-based rather than tile-based internal layout; sample values and GeoTIFF/GDAL metadata were preserved.

- **Current maintenance**
  - Copyright 2026 Martin Karing / TKI mbH, Chemnitz, Germany.

## Included license texts

- `LICENSES/LGPL-2.1-or-later.txt`
- `LICENSES/PROJ-MIT.txt`
