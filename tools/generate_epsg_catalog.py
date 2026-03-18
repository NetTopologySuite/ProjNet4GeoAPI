import argparse
import math
import sqlite3
import zipfile
from pathlib import Path

ORIENTATION_MAP = {
    'north': 1,
    'south': 2,
    'east': 3,
    'west': 4,
    'up': 5,
    'down': 6,
    'other': 0,
    'geocentricx': 0,
    'geocentricy': 3,
    'geocentricz': 1,
}


def map_orientation(raw_orientation: str):
    orientation_key = (raw_orientation or '').strip().lower().replace(' ', '').replace('-', '')
    orientation = ORIENTATION_MAP.get(orientation_key)
    if orientation is not None:
        return orientation

    if orientation_key.startswith('north'):
        return ORIENTATION_MAP['north']
    if orientation_key.startswith('south'):
        return ORIENTATION_MAP['south']
    if orientation_key.startswith('east'):
        return ORIENTATION_MAP['east']
    if orientation_key.startswith('west'):
        return ORIENTATION_MAP['west']
    if orientation_key.startswith('up'):
        return ORIENTATION_MAP['up']
    if orientation_key.startswith('down'):
        return ORIENTATION_MAP['down']

    return None

KIND_GEOGRAPHIC = 0
KIND_GEOCENTRIC = 1
KIND_PROJECTED = 2
KIND_VERTICAL = 3
KIND_COMPOUND = 4

UNIT_LINEAR = 0
UNIT_ANGULAR = 1


def esc(value: str) -> str:
    return value.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '\\r').replace('\n', '\\n').replace('\t', '\\t')


def load_proj_data(db_path: Path):
    conn = sqlite3.connect(str(db_path))
    conn.row_factory = sqlite3.Row
    cur = conn.cursor()

    cur.execute("SELECT code, name, type, conv_factor FROM unit_of_measure WHERE auth_name='EPSG' ORDER BY CAST(code AS INTEGER)")
    units = {}
    for row in cur.fetchall():
        typ = (row['type'] or '').strip().lower()
        if typ.startswith('linear') or typ == 'length':
            unit_type = UNIT_LINEAR
        elif typ.startswith('angular') or typ == 'angle':
            unit_type = UNIT_ANGULAR
        else:
            continue
        units[int(row['code'])] = {
            'code': int(row['code']),
            'name': row['name'] or '',
            'unit_type': unit_type,
            'factor': float(row['conv_factor']) if row['conv_factor'] is not None else 1.0,
        }

    cur.execute("SELECT code, type, dimension FROM coordinate_system WHERE auth_name='EPSG'")
    coordinate_systems = {int(r['code']): {'type': r['type'] or '', 'dimension': int(r['dimension'])} for r in cur.fetchall()}

    cur.execute("SELECT coordinate_system_code, coordinate_system_order, name, orientation, uom_code FROM axis WHERE auth_name='EPSG' ORDER BY CAST(coordinate_system_code AS INTEGER), coordinate_system_order")
    axes_by_cs = {}
    for row in cur.fetchall():
        cs_code = int(row['coordinate_system_code'])
        orientation = map_orientation(row['orientation'])
        axis = {
            'order': int(row['coordinate_system_order']),
            'name': row['name'] or '',
            'orientation': orientation,
            'unit_code': int(row['uom_code']) if row['uom_code'] is not None else -1,
        }
        axes_by_cs.setdefault(cs_code, []).append(axis)

    cur.execute("SELECT code, name, semi_major_axis, semi_minor_axis, inv_flattening, uom_code FROM ellipsoid WHERE auth_name='EPSG'")
    ellipsoids = {}
    for row in cur.fetchall():
        inv_flattening = row['inv_flattening']
        semi_minor = row['semi_minor_axis']
        if inv_flattening is None:
            inv_flattening = 0.0
        if semi_minor is None:
            semi_minor = 0.0
        use_ivf = bool(inv_flattening and not math.isinf(inv_flattening))
        ellipsoids[int(row['code'])] = {
            'code': int(row['code']),
            'name': row['name'] or '',
            'semi_major': float(row['semi_major_axis']),
            'semi_minor': float(semi_minor),
            'inv_flattening': float(inv_flattening),
            'use_ivf': use_ivf,
            'unit_code': int(row['uom_code']) if row['uom_code'] is not None else -1,
        }

    cur.execute("SELECT code, name, longitude, uom_code FROM prime_meridian WHERE auth_name='EPSG'")
    prime_meridians = {int(r['code']): {'code': int(r['code']), 'name': r['name'] or '', 'longitude': float(r['longitude']), 'unit_code': int(r['uom_code']) if r['uom_code'] is not None else -1} for r in cur.fetchall()}

    cur.execute("SELECT code, name, ellipsoid_code, prime_meridian_code FROM geodetic_datum WHERE auth_name='EPSG'")
    geodetic_datums = {int(r['code']): {'code': int(r['code']), 'name': r['name'] or '', 'ellipsoid_code': int(r['ellipsoid_code']), 'prime_meridian_code': int(r['prime_meridian_code'])} for r in cur.fetchall()}

    cur.execute("SELECT code, name FROM vertical_datum WHERE auth_name='EPSG'")
    vertical_datums = {int(r['code']): {'code': int(r['code']), 'name': r['name'] or ''} for r in cur.fetchall()}

    cur.execute("SELECT code, name FROM conversion_method WHERE auth_name='EPSG'")
    conversion_methods = {int(r['code']): r['name'] or '' for r in cur.fetchall()}

    cur.execute("SELECT code, name FROM conversion_param WHERE auth_name='EPSG'")
    conversion_params = {int(r['code']): r['name'] or '' for r in cur.fetchall()}

    cur.execute("SELECT * FROM conversion_table WHERE auth_name='EPSG'")
    conversions = {}
    conversion_parameters = []
    for row in cur.fetchall():
        code = int(row['code'])
        method_name = conversion_methods.get(int(row['method_code']), '')
        start = len(conversion_parameters)
        count = 0
        for i in range(1, 8):
            param_code = row[f'param{i}_code']
            if param_code is None:
                continue
            value = row[f'param{i}_value']
            if value is None:
                continue
            name = conversion_params.get(int(param_code), '')
            conversion_parameters.append({'conversion_code': code, 'name': name, 'value': float(value)})
            count += 1
        conversions[code] = {'code': code, 'method_name': method_name, 'start': start, 'count': count}

    cur.execute("SELECT code, name, type, coordinate_system_code, datum_code FROM geodetic_crs WHERE auth_name='EPSG'")
    geodetic_crs = {int(r['code']): {'srid': int(r['code']), 'name': r['name'] or '', 'type': (r['type'] or '').lower(), 'coordinate_system_code': int(r['coordinate_system_code']), 'datum_code': int(r['datum_code'])} for r in cur.fetchall()}

    cur.execute("SELECT code, name, coordinate_system_code, geodetic_crs_code, conversion_code FROM projected_crs WHERE auth_name='EPSG'")
    projected_crs = {int(r['code']): {'srid': int(r['code']), 'name': r['name'] or '', 'coordinate_system_code': int(r['coordinate_system_code']), 'base_srid': int(r['geodetic_crs_code']), 'conversion_code': int(r['conversion_code'])} for r in cur.fetchall()}

    cur.execute("SELECT code, name, coordinate_system_code, datum_code FROM vertical_crs WHERE auth_name='EPSG'")
    vertical_crs = {int(r['code']): {'srid': int(r['code']), 'name': r['name'] or '', 'coordinate_system_code': int(r['coordinate_system_code']), 'datum_code': int(r['datum_code'])} for r in cur.fetchall()}

    cur.execute("SELECT code, name, horiz_crs_code, vertical_crs_code FROM compound_crs WHERE auth_name='EPSG'")
    compound_crs = {int(r['code']): {'srid': int(r['code']), 'name': r['name'] or '', 'horizontal_srid': int(r['horiz_crs_code']), 'vertical_srid': int(r['vertical_crs_code'])} for r in cur.fetchall()}

    conn.close()

    return {
        'units': units,
        'coordinate_systems': coordinate_systems,
        'axes_by_cs': axes_by_cs,
        'ellipsoids': ellipsoids,
        'prime_meridians': prime_meridians,
        'geodetic_datums': geodetic_datums,
        'vertical_datums': vertical_datums,
        'conversions': conversions,
        'conversion_parameters': conversion_parameters,
        'geodetic_crs': geodetic_crs,
        'projected_crs': projected_crs,
        'vertical_crs': vertical_crs,
        'compound_crs': compound_crs,
    }


def extract_operation_data(zip_path: Path):
    import re

    crs_pattern = re.compile(r'^EPSG-CRS-(\d+)\.wkt$')
    transform_pattern = re.compile(r'^EPSG-Transformation-(\d+)\.wkt$')
    concat_pattern = re.compile(r'^EPSG-ConcatenatedOperation-(\d+)\.wkt$')
    pmo_pattern = re.compile(r'^EPSG-PMO-(\d+)\.wkt$')
    id_pattern = re.compile(r'ID\["EPSG",(\d+)\]')
    method_pattern = re.compile(r'METHOD\["([^"]+)"')
    accuracy_pattern = re.compile(r'OPERATIONACCURACY\[(-?\d+(?:\.\d+)?)\]')
    parameter_file_pattern = re.compile(r'PARAMETERFILE\["[^"]*","([^"]+)"')
    parameter_pattern = re.compile(r'PARAMETER\["([^"]+)",\s*([-+]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)')

    def normalize_operation_method_name(value: str):
        if not value:
            return ''
        return ''.join(ch.lower() for ch in value if ch.isalnum())

    def normalize_operation_parameter_name(value: str):
        if not value:
            return ''

        buffer = []
        for character in value:
            if character.isalnum():
                buffer.append(character.lower())
            elif buffer and buffer[-1] != '_':
                buffer.append('_')

        if buffer and buffer[-1] == '_':
            buffer.pop()

        return ''.join(buffer)

    def bracket_content(text: str, token: str):
        idx = text.find(token)
        if idx < 0:
            return None
        start = idx + len(token)
        depth = 1
        i = start
        while i < len(text):
            ch = text[i]
            if ch == '[':
                depth += 1
            elif ch == ']':
                depth -= 1
                if depth == 0:
                    return text[start:i]
            i += 1
        return None

    parsed_operations = []
    with zipfile.ZipFile(zip_path, 'r') as zf:
        for info in sorted(zf.infolist(), key=lambda i: i.filename):
            name = Path(info.filename).name
            if not name.lower().endswith('.wkt'):
                continue
            text = zf.read(info).decode('utf-8')
            if crs_pattern.match(name):
                continue
            operation_type = -1
            operation_code = -1
            m = transform_pattern.match(name)
            if m:
                operation_type = 0
                operation_code = int(m.group(1))
            else:
                m = concat_pattern.match(name)
                if m:
                    operation_type = 1
                    operation_code = int(m.group(1))
                else:
                    m = pmo_pattern.match(name)
                    if m:
                        operation_type = 2
                        operation_code = int(m.group(1))
            if operation_type < 0:
                continue

            src_block = bracket_content(text, 'SOURCECRS[')
            tgt_block = bracket_content(text, 'TARGETCRS[')
            if not src_block or not tgt_block:
                continue
            src_ids = id_pattern.findall(src_block)
            tgt_ids = id_pattern.findall(tgt_block)
            if not src_ids or not tgt_ids:
                continue

            method_match = method_pattern.search(text)
            method_name = method_match.group(1) if method_match else ''
            acc_match = accuracy_pattern.search(text)
            accuracy = float(acc_match.group(1)) if acc_match else float('nan')
            pf_match = parameter_file_pattern.search(text)
            parameter_file_name = pf_match.group(1) if pf_match else ''
            parameters = [(m.group(1), float(m.group(2))) for m in parameter_pattern.finditer(text)]

            parsed_operations.append({
                'operation_type': operation_type,
                'operation_code': operation_code,
                'source_srid': int(src_ids[-1]),
                'target_srid': int(tgt_ids[-1]),
                'accuracy': accuracy,
                'method_name': method_name,
                'parameter_file_name': parameter_file_name,
                'parameters': parameters,
            })

    parsed_operations.sort(key=lambda r: (r['operation_type'], r['operation_code']))

    operations = []
    operation_parameters = []
    explicit_operations = []
    for record in parsed_operations:
        parameter_start_index = len(operation_parameters)
        parameter_count = 0
        normalized_parameters = {}
        for parameter_name, parameter_value in record['parameters']:
            operation_parameters.append((record['operation_code'], parameter_name, parameter_value))
            parameter_count += 1
            normalized_parameters[normalize_operation_parameter_name(parameter_name)] = parameter_value

        operations.append((
            record['operation_type'],
            record['operation_code'],
            record['source_srid'],
            record['target_srid'],
            record['accuracy'],
            record['method_name'],
            record['parameter_file_name'],
            parameter_start_index,
            parameter_count,
        ))

        normalized_method = normalize_operation_method_name(record['method_name'])
        supports_explicit_helmert = (
            'geocentrictranslations' in normalized_method
            or 'positionvectortransformation' in normalized_method
            or 'coordinateframerotation' in normalized_method
        )
        if not supports_explicit_helmert:
            continue

        if ('x_axis_translation' not in normalized_parameters
                or 'y_axis_translation' not in normalized_parameters
                or 'z_axis_translation' not in normalized_parameters):
            continue

        dx = normalized_parameters['x_axis_translation']
        dy = normalized_parameters['y_axis_translation']
        dz = normalized_parameters['z_axis_translation']
        ex = 0.0
        ey = 0.0
        ez = 0.0
        ppm = 0.0

        has_rotation_scale = (
            'positionvectortransformation' in normalized_method
            or 'coordinateframerotation' in normalized_method
        )
        if has_rotation_scale:
            if ('x_axis_rotation' not in normalized_parameters
                    or 'y_axis_rotation' not in normalized_parameters
                    or 'z_axis_rotation' not in normalized_parameters
                    or 'scale_difference' not in normalized_parameters):
                continue

            ex = normalized_parameters['x_axis_rotation']
            ey = normalized_parameters['y_axis_rotation']
            ez = normalized_parameters['z_axis_rotation']
            ppm = normalized_parameters['scale_difference']

            if 'coordinateframerotation' in normalized_method:
                ex = -ex
                ey = -ey
                ez = -ez

        explicit_operations.append((record['operation_code'], dx, dy, dz, ex, ey, ez, ppm))

    explicit_operations.sort(key=lambda v: v[0])
    return operations, operation_parameters, explicit_operations


def build_catalog(data):
    units = data['units']
    coordinate_systems = data['coordinate_systems']
    axes_by_cs = data['axes_by_cs']
    ellipsoids = data['ellipsoids']
    prime_meridians = data['prime_meridians']
    geodetic_datums = data['geodetic_datums']
    vertical_datums = data['vertical_datums']
    conversions = data['conversions']

    def cs_supported(cs_code, expected_dimension=None):
        cs = coordinate_systems.get(cs_code)
        axes = axes_by_cs.get(cs_code)
        if cs is None or axes is None:
            return False
        if expected_dimension is not None and cs['dimension'] < expected_dimension:
            return False
        for axis in axes:
            if axis['orientation'] is None:
                return False
            if axis['unit_code'] not in units:
                return False
        return True

    supported_srids = set()

    geographic_records = []
    geocentric_records = []
    projected_records = []
    vertical_records = []
    compound_records = []

    geodetic = data['geodetic_crs']
    for srid in sorted(geodetic):
        record = geodetic[srid]
        datum = geodetic_datums.get(record['datum_code'])
        if datum is None:
            continue
        if datum['ellipsoid_code'] not in ellipsoids or datum['prime_meridian_code'] not in prime_meridians:
            continue
        if record['type'] == 'geographic 2d':
            if not cs_supported(record['coordinate_system_code'], 2):
                continue
            axes = axes_by_cs[record['coordinate_system_code']]
            if len(axes) < 2 or units[axes[0]['unit_code']]['unit_type'] != UNIT_ANGULAR or units[axes[1]['unit_code']]['unit_type'] != UNIT_ANGULAR:
                continue
            supported_srids.add(srid)
            geographic_records.append((srid, record['name'], record['datum_code'], record['coordinate_system_code']))
        elif record['type'] == 'geocentric':
            if not cs_supported(record['coordinate_system_code'], 3):
                continue
            axes = axes_by_cs[record['coordinate_system_code']]
            if len(axes) < 3 or any(units[a['unit_code']]['unit_type'] != UNIT_LINEAR for a in axes[:3]):
                continue
            supported_srids.add(srid)
            geocentric_records.append((srid, record['name'], record['datum_code'], record['coordinate_system_code']))

    for srid in sorted(data['projected_crs']):
        record = data['projected_crs'][srid]
        if record['base_srid'] not in supported_srids:
            continue
        if not cs_supported(record['coordinate_system_code'], 2):
            continue
        axes = axes_by_cs[record['coordinate_system_code']]
        if len(axes) < 2 or units[axes[0]['unit_code']]['unit_type'] != UNIT_LINEAR or units[axes[1]['unit_code']]['unit_type'] != UNIT_LINEAR:
            continue
        conversion = conversions.get(record['conversion_code'])
        if conversion is None:
            continue
        supported_srids.add(srid)
        projected_records.append((srid, record['name'], record['base_srid'], record['coordinate_system_code'], record['conversion_code']))

    for srid in sorted(data['vertical_crs']):
        record = data['vertical_crs'][srid]
        if record['datum_code'] not in vertical_datums:
            continue
        if not cs_supported(record['coordinate_system_code'], 1):
            continue
        axis = axes_by_cs[record['coordinate_system_code']][0]
        if units[axis['unit_code']]['unit_type'] != UNIT_LINEAR:
            continue
        supported_srids.add(srid)
        vertical_records.append((srid, record['name'], record['datum_code'], record['coordinate_system_code']))

    for srid in sorted(data['compound_crs']):
        record = data['compound_crs'][srid]
        if record['horizontal_srid'] not in supported_srids or record['vertical_srid'] not in supported_srids:
            continue
        supported_srids.add(srid)
        compound_records.append((srid, record['name'], record['horizontal_srid'], record['vertical_srid']))

    unit_records = []
    used_unit_codes = set()
    for axes in axes_by_cs.values():
        for a in axes:
            used_unit_codes.add(a['unit_code'])
    for e in ellipsoids.values():
        used_unit_codes.add(e['unit_code'])
    for p in prime_meridians.values():
        used_unit_codes.add(p['unit_code'])

    for code in sorted(used_unit_codes):
        unit = units.get(code)
        if unit is None:
            continue
        unit_records.append((code, unit['unit_type'], unit['factor'], unit['name']))

    axis_records = []
    for cs_code in sorted(axes_by_cs):
        for axis in sorted(axes_by_cs[cs_code], key=lambda a: a['order']):
            if axis['orientation'] is None:
                continue
            axis_records.append((cs_code, axis['order'], axis['name'], axis['orientation'], axis['unit_code']))

    ellipsoid_records = []
    used_ellipsoid = {data['geodetic_datums'][r[2]]['ellipsoid_code'] for r in geographic_records + geocentric_records if r[2] in data['geodetic_datums']}
    for code in sorted(used_ellipsoid):
        e = ellipsoids[code]
        ellipsoid_records.append((code, e['name'], e['semi_major'], e['semi_minor'], e['inv_flattening'], 1 if e['use_ivf'] else 0, e['unit_code']))

    prime_meridian_records = []
    used_pm = {data['geodetic_datums'][r[2]]['prime_meridian_code'] for r in geographic_records + geocentric_records if r[2] in data['geodetic_datums']}
    for code in sorted(used_pm):
        p = prime_meridians[code]
        prime_meridian_records.append((code, p['name'], p['longitude'], p['unit_code']))

    geodetic_datum_records = []
    used_datum = {r[2] for r in geographic_records + geocentric_records}
    for code in sorted(used_datum):
        d = geodetic_datums[code]
        geodetic_datum_records.append((code, d['name'], d['ellipsoid_code'], d['prime_meridian_code']))

    vertical_datum_records = []
    used_vdatum = {r[2] for r in vertical_records}
    for code in sorted(used_vdatum):
        d = vertical_datums[code]
        vertical_datum_records.append((code, d['name']))

    conversion_records = []
    conversion_param_records = []
    used_conversions = {r[4] for r in projected_records}
    for code in sorted(used_conversions):
        c = conversions[code]
        local_start = len(conversion_param_records)
        local_count = 0
        for i in range(c['count']):
            parameter = data['conversion_parameters'][c['start'] + i]
            conversion_param_records.append((code, parameter['name'], parameter['value']))
            local_count += 1

        conversion_records.append((code, c['method_name'], local_start, local_count))


    ref_records = []
    geo_index = {r[0]: i for i, r in enumerate(geographic_records)}
    geoc_index = {r[0]: i for i, r in enumerate(geocentric_records)}
    proj_index = {r[0]: i for i, r in enumerate(projected_records)}
    vert_index = {r[0]: i for i, r in enumerate(vertical_records)}
    comp_index = {r[0]: i for i, r in enumerate(compound_records)}
    for srid in sorted(supported_srids):
        if srid in geo_index:
            ref_records.append((srid, KIND_GEOGRAPHIC, geo_index[srid]))
        elif srid in geoc_index:
            ref_records.append((srid, KIND_GEOCENTRIC, geoc_index[srid]))
        elif srid in proj_index:
            ref_records.append((srid, KIND_PROJECTED, proj_index[srid]))
        elif srid in vert_index:
            ref_records.append((srid, KIND_VERTICAL, vert_index[srid]))
        elif srid in comp_index:
            ref_records.append((srid, KIND_COMPOUND, comp_index[srid]))

    return {
        'unit_records': unit_records,
        'axis_records': axis_records,
        'ellipsoid_records': ellipsoid_records,
        'prime_meridian_records': prime_meridian_records,
        'geodetic_datum_records': geodetic_datum_records,
        'vertical_datum_records': vertical_datum_records,
        'conversion_records': conversion_records,
        'conversion_param_records': conversion_param_records,
        'geographic_records': geographic_records,
        'geocentric_records': geocentric_records,
        'projected_records': projected_records,
        'vertical_records': vertical_records,
        'compound_records': compound_records,
        'ref_records': ref_records,
    }


def emit(output_path: Path, zip_name: str, catalog, operations, operation_parameters, explicit_operations):
    lines = []
    lines.append('// <auto-generated>')
    lines.append('// Generated by tools\\Generate-EpsgManagedData.ps1')
    lines.append(f'// Source: {zip_name}')
    lines.append('// </auto-generated>')
    lines.append('#pragma warning disable SA0001, SA1512, SA1518, SA1600, SA1614, SA1616, SA1633, SA1636')
    lines.append('using System;')
    lines.append('using System.Collections.Generic;')
    lines.append('')
    lines.append('namespace ProjNet.Data.Generated')
    lines.append('{')
    lines.append('    internal enum EpsgOperationType : byte { Transformation = 0, ConcatenatedOperation = 1, PointMotionOperation = 2 }')
    lines.append('    internal enum EpsgCoordinateSystemKind : byte { Geographic2D = 0, Geocentric = 1, Projected = 2, Vertical = 3, Compound = 4 }')
    lines.append('')

    struct_defs = {
        'EpsgCoordinateReferenceRecord': 'int srid, EpsgCoordinateSystemKind kind, int recordIndex',
        'EpsgGeographicCrsRecord': 'int srid, string name, int datumCode, int coordinateSystemCode',
        'EpsgGeocentricCrsRecord': 'int srid, string name, int datumCode, int coordinateSystemCode',
        'EpsgProjectedCrsRecord': 'int srid, string name, int baseSrid, int coordinateSystemCode, int conversionCode',
        'EpsgVerticalCrsRecord': 'int srid, string name, int datumCode, int coordinateSystemCode',
        'EpsgCompoundCrsRecord': 'int srid, string name, int horizontalSrid, int verticalSrid',
        'EpsgUnitRecord': 'int code, byte unitType, double factor, string name',
        'EpsgAxisRecord': 'int coordinateSystemCode, byte axisOrder, string name, sbyte orientation, int unitCode',
        'EpsgEllipsoidRecord': 'int code, string name, double semiMajor, double semiMinor, double inverseFlattening, bool isInverseFlatteningDefinitive, int unitCode',
        'EpsgPrimeMeridianRecord': 'int code, string name, double longitude, int unitCode',
        'EpsgGeodeticDatumRecord': 'int code, string name, int ellipsoidCode, int primeMeridianCode',
        'EpsgVerticalDatumRecord': 'int code, string name',
        'EpsgConversionRecord': 'int code, string methodName, int parameterStartIndex, int parameterCount',
        'EpsgConversionParameterRecord': 'int conversionCode, string name, double value',
        'EpsgOperationRecord': 'EpsgOperationType operationType, int operationCode, int sourceSrid, int targetSrid, double accuracy, string methodName, string parameterFileName, int parameterStartIndex, int parameterCount',
        'EpsgOperationParameterRecord': 'int operationCode, string name, double value',
        'EpsgExplicitOperationRecord': 'int operationCode, double dx, double dy, double dz, double ex, double ey, double ez, double ppm',
    }

    for name, args in struct_defs.items():
        lines.append(f'    internal readonly struct {name}')
        lines.append('    {')
        ctor_args = ', '.join([a.strip() for a in args.split(',')])
        lines.append(f'        internal {name}({ctor_args})')
        lines.append('        {')
        for part in args.split(','):
            var_name = part.strip().split(' ')[-1]
            prop = var_name[0].upper() + var_name[1:]
            lines.append(f'            {prop} = {var_name};')
        lines.append('        }')
        lines.append('')
        for part in args.split(','):
            t, var_name = part.strip().rsplit(' ', 1)
            prop = var_name[0].upper() + var_name[1:]
            lines.append(f'        internal {t} {prop} {{ get; }}')
        lines.append('    }')
        lines.append('')

    lines.append('    internal static class EpsgGeneratedCatalog')
    lines.append('    {')
    lines.append(f'        internal const string SourceArchive = "{esc(zip_name)}";')

    def build_string_pool(values):
        pool = sorted(set(values))
        index_by_value = {value: idx for idx, value in enumerate(pool)}
        return pool, index_by_value

    def emit_string_array(name, values):
        lines.append(f'        private static readonly string[] {name} = new string[]')
        lines.append('        {')
        for value in values:
            lines.append(f'            "{esc(value)}",')
        lines.append('        };')
        lines.append('')

    def emit_array(name, type_name, values, fmt):
        lines.append(f'        internal static readonly {type_name}[] {name} = new {type_name}[]')
        lines.append('        {')
        for v in values:
            lines.append('            new ' + type_name + '(' + fmt(v) + '),')
        lines.append('        };')
        lines.append('')

    lines.append(f'        internal const int CoordinateReferenceCount = {len(catalog["ref_records"])};')
    lines.append('        private static readonly Dictionary<int, EpsgCoordinateReferenceRecord> CoordinateReferences = new Dictionary<int, EpsgCoordinateReferenceRecord>();')
    lines.append('        private static readonly object CoordinateReferencesSync = new object();')
    lines.append(f'        private static readonly int[] CoordinateSridByCacheIndex = new int[] {{ {", ".join(str(ref_record[0]) for ref_record in catalog["ref_records"])} }};')
    lines.append('')

    conversion_method_pool, conversion_method_index = build_string_pool([record[1] for record in catalog['conversion_records']])
    conversion_parameter_pool, conversion_parameter_index = build_string_pool([record[1] for record in catalog['conversion_param_records']])
    operation_method_pool, operation_method_index = build_string_pool([record[5] for record in operations])
    operation_parameter_file_pool, operation_parameter_file_index = build_string_pool([record[6] for record in operations])
    operation_parameter_pool, operation_parameter_index = build_string_pool([record[1] for record in operation_parameters])

    emit_string_array('ConversionMethodNames', conversion_method_pool)
    emit_string_array('ConversionParameterNames', conversion_parameter_pool)
    emit_string_array('OperationMethodNames', operation_method_pool)
    emit_string_array('OperationParameterFileNames', operation_parameter_file_pool)
    emit_string_array('OperationParameterNames', operation_parameter_pool)

    emit_array('GeographicCrs', 'EpsgGeographicCrsRecord', catalog['geographic_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}")
    emit_array('GeocentricCrs', 'EpsgGeocentricCrsRecord', catalog['geocentric_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}")
    emit_array('ProjectedCrs', 'EpsgProjectedCrsRecord', catalog['projected_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}, {v[4]}")
    emit_array('VerticalCrs', 'EpsgVerticalCrsRecord', catalog['vertical_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}")
    emit_array('CompoundCrs', 'EpsgCompoundCrsRecord', catalog['compound_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}")
    emit_array('Units', 'EpsgUnitRecord', catalog['unit_records'], lambda v: f"{v[0]}, {v[1]}, {repr(v[2])}d, \"{esc(v[3])}\"")
    emit_array('Axes', 'EpsgAxisRecord', catalog['axis_records'], lambda v: f"{v[0]}, (byte){v[1]}, \"{esc(v[2])}\", (sbyte){v[3]}, {v[4]}")
    emit_array('Ellipsoids', 'EpsgEllipsoidRecord', catalog['ellipsoid_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {repr(v[2])}d, {repr(v[3])}d, {repr(v[4])}d, {'true' if v[5] else 'false'}, {v[6]}")
    emit_array('PrimeMeridians', 'EpsgPrimeMeridianRecord', catalog['prime_meridian_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {repr(v[2])}d, {v[3]}")
    emit_array('GeodeticDatums', 'EpsgGeodeticDatumRecord', catalog['geodetic_datum_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\", {v[2]}, {v[3]}")
    emit_array('VerticalDatums', 'EpsgVerticalDatumRecord', catalog['vertical_datum_records'], lambda v: f"{v[0]}, \"{esc(v[1])}\"")
    emit_array('Conversions', 'EpsgConversionRecord', catalog['conversion_records'], lambda v: f"{v[0]}, ConversionMethodNames[{conversion_method_index[v[1]]}], {v[2]}, {v[3]}")
    emit_array('ConversionParameters', 'EpsgConversionParameterRecord', catalog['conversion_param_records'], lambda v: f"{v[0]}, ConversionParameterNames[{conversion_parameter_index[v[1]]}], {repr(v[2])}d")

    def fmt_op(v):
        acc = 'double.NaN' if math.isnan(v[4]) else f'{repr(v[4])}d'
        return (
            f"(EpsgOperationType){v[0]}, {v[1]}, {v[2]}, {v[3]}, {acc}, "
            f"OperationMethodNames[{operation_method_index[v[5]]}], "
            f"OperationParameterFileNames[{operation_parameter_file_index[v[6]]}], {v[7]}, {v[8]}"
        )

    emit_array('Operations', 'EpsgOperationRecord', operations, fmt_op)
    emit_array('OperationParameters', 'EpsgOperationParameterRecord', operation_parameters, lambda v: f"{v[0]}, OperationParameterNames[{operation_parameter_index[v[1]]}], {repr(v[2])}d")
    emit_array('ExplicitOperations', 'EpsgExplicitOperationRecord', explicit_operations, lambda v: f"{v[0]}, {repr(v[1])}d, {repr(v[2])}d, {repr(v[3])}d, {repr(v[4])}d, {repr(v[5])}d, {repr(v[6])}d, {repr(v[7])}d")

    lines.append('        internal static bool TryGetCoordinateReference(int srid, out EpsgCoordinateReferenceRecord reference, out int cacheIndex)')
    lines.append('        {')
    lines.append('            switch (srid)')
    lines.append('            {')
    for idx, ref_record in enumerate(catalog['ref_records']):
        lines.append(f'                case {ref_record[0]}:')
        lines.append(f'                    cacheIndex = {idx};')
        lines.append(f'                    if (!CoordinateReferences.TryGetValue({idx}, out reference))')
        lines.append('                    {')
        lines.append('                        lock (CoordinateReferencesSync)')
        lines.append('                        {')
        lines.append(f'                            if (!CoordinateReferences.TryGetValue({idx}, out reference))')
        lines.append('                            {')
        lines.append(f'                                reference = new EpsgCoordinateReferenceRecord({ref_record[0]}, (EpsgCoordinateSystemKind){ref_record[1]}, {ref_record[2]});')
        lines.append(f'                                CoordinateReferences[{idx}] = reference;')
        lines.append('                            }')
        lines.append('                        }')
        lines.append('                    }')
        lines.append('                    return true;')
    lines.append('                default:')
    lines.append('                    cacheIndex = -1;')
    lines.append('                    reference = default;')
    lines.append('                    return false;')
    lines.append('            }')
    lines.append('        }')
    lines.append('')
    lines.append('        internal static bool TryGetExplicitOperationParameters(int operationCode, out EpsgExplicitOperationRecord parameters)')
    lines.append('        {')
    lines.append('            switch (operationCode)')
    lines.append('            {')
    for idx, explicit_record in enumerate(explicit_operations):
        lines.append(f'                case {explicit_record[0]}:')
        lines.append(f'                    parameters = ExplicitOperations[{idx}];')
        lines.append('                    return true;')
    lines.append('                default:')
    lines.append('                    parameters = default;')
    lines.append('                    return false;')
    lines.append('            }')
    lines.append('        }')
    lines.append('')
    lines.append('        internal static bool TryGetCoordinateSridByCacheIndex(int cacheIndex, out int srid)')
    lines.append('        {')
    lines.append('            if ((uint)cacheIndex < (uint)CoordinateSridByCacheIndex.Length)')
    lines.append('            {')
    lines.append('                srid = CoordinateSridByCacheIndex[cacheIndex];')
    lines.append('                return true;')
    lines.append('            }')
    lines.append('')
    lines.append('            srid = -1;')
    lines.append('            return false;')
    lines.append('        }')

    lines.append('    }')
    lines.append('}')

    output_path.write_text('\n'.join(lines) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--zip', required=True)
    parser.add_argument('--proj-db', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()

    zip_path = Path(args.zip)
    proj_db_path = Path(args.proj_db)
    output_path = Path(args.output)

    output_path.parent.mkdir(parents=True, exist_ok=True)

    proj_data = load_proj_data(proj_db_path)
    catalog = build_catalog(proj_data)
    operations, operation_parameters, explicit_operations = extract_operation_data(zip_path)

    emit(output_path, zip_path.name, catalog, operations, operation_parameters, explicit_operations)

    print(f'Generated: {output_path}')
    print(f"CRS records: {len(catalog['ref_records'])}")
    print(f'Operation records: {len(operations)}')


if __name__ == '__main__':
    main()




