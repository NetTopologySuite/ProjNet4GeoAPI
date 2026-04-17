import argparse
import math
import re
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


class WktIdentifier(str):
    pass


class WktNode:
    __slots__ = ('keyword', 'items')

    def __init__(self, keyword: str, items):
        self.keyword = (keyword or '').upper()
        self.items = items


class WktParser:
    def __init__(self, text: str):
        self._text = text or ''
        self._index = 0
        self._length = len(self._text)

    def parse(self):
        self._skip_whitespace()
        if self._index >= self._length:
            raise ValueError('WKT text is empty.')

        node = self._parse_node()
        self._skip_whitespace()
        return node

    def _skip_whitespace(self):
        while self._index < self._length and self._text[self._index].isspace():
            self._index += 1

    def _peek(self):
        if self._index >= self._length:
            return ''
        return self._text[self._index]

    def _consume(self, expected: str):
        actual = self._peek()
        if actual != expected:
            raise ValueError(f'Unexpected token "{actual}" while expecting "{expected}" at offset {self._index}.')
        self._index += 1

    def _parse_identifier(self):
        start = self._index
        while self._index < self._length:
            ch = self._text[self._index]
            if ch.isalnum() or ch == '_':
                self._index += 1
                continue
            break

        if start == self._index:
            raise ValueError(f'Expected identifier at offset {self._index}.')
        return self._text[start:self._index]

    def _parse_string(self):
        self._consume('"')
        buffer = []
        while self._index < self._length:
            ch = self._text[self._index]
            if ch == '"':
                if self._index + 1 < self._length and self._text[self._index + 1] == '"':
                    buffer.append('"')
                    self._index += 2
                    continue

                self._index += 1
                return ''.join(buffer)

            buffer.append(ch)
            self._index += 1

        raise ValueError('Unterminated string literal in WKT text.')

    def _parse_number(self):
        start = self._index
        if self._peek() in '+-':
            self._index += 1

        digits_seen = False
        while self._index < self._length and self._text[self._index].isdigit():
            digits_seen = True
            self._index += 1

        if self._index < self._length and self._text[self._index] == '.':
            self._index += 1
            while self._index < self._length and self._text[self._index].isdigit():
                digits_seen = True
                self._index += 1

        if self._index < self._length and self._text[self._index] in 'eE':
            exponent_pos = self._index
            self._index += 1
            if self._index < self._length and self._text[self._index] in '+-':
                self._index += 1

            exponent_digits = False
            while self._index < self._length and self._text[self._index].isdigit():
                exponent_digits = True
                self._index += 1

            if not exponent_digits:
                self._index = exponent_pos

        token = self._text[start:self._index]
        if not digits_seen:
            raise ValueError(f'Invalid number token "{token}" at offset {start}.')

        if '.' in token or 'e' in token.lower():
            return float(token)
        return int(token)

    def _parse_value(self):
        self._skip_whitespace()
        ch = self._peek()
        if not ch:
            raise ValueError('Unexpected end of WKT text.')

        if ch == '"':
            return self._parse_string()

        if ch in '+-.' or ch.isdigit():
            return self._parse_number()

        if ch.isalpha() or ch == '_':
            identifier = self._parse_identifier()
            self._skip_whitespace()
            if self._peek() == '[':
                return self._parse_node(identifier)
            return WktIdentifier(identifier)

        raise ValueError(f'Unsupported token "{ch}" at offset {self._index}.')

    def _parse_node(self, keyword: str = None):
        node_keyword = keyword if keyword is not None else self._parse_identifier()
        self._skip_whitespace()
        self._consume('[')

        items = []
        while True:
            self._skip_whitespace()
            ch = self._peek()
            if ch == ']':
                self._consume(']')
                break

            items.append(self._parse_value())
            self._skip_whitespace()
            ch = self._peek()
            if ch == ',':
                self._consume(',')
                continue
            if ch == ']':
                self._consume(']')
                break

            raise ValueError(f'Unexpected token "{ch}" at offset {self._index} while parsing "{node_keyword}".')

        return WktNode(node_keyword, items)


def parse_wkt_node(text: str):
    return WktParser(text).parse()


def split_sql_values(values_part: str):
    items = []
    current = []
    in_string = False
    i = 0
    while i < len(values_part):
        ch = values_part[i]
        if ch == "'":
            current.append(ch)
            if in_string and i + 1 < len(values_part) and values_part[i + 1] == "'":
                current.append("'")
                i += 1
            else:
                in_string = not in_string
        elif ch == ',' and not in_string:
            items.append(''.join(current).strip())
            current = []
        else:
            current.append(ch)
        i += 1

    items.append(''.join(current).strip())
    return items


def parse_named_sql_insert(line: str, table_name: str):
    prefix = f'INSERT INTO epsg_{table_name} ('
    if not line.startswith(prefix) or not line.endswith(');'):
        return None

    columns_end = line.find(') VALUES (', len(prefix))
    if columns_end < 0:
        return None

    columns = [column.strip() for column in line[len(prefix):columns_end].split(',')]
    values = split_sql_values(line[columns_end + len(') VALUES ('):-2])
    if len(columns) != len(values):
        raise ValueError(f'Unexpected SQL insert shape for epsg_{table_name}: {len(columns)} columns, {len(values)} values.')

    record = {}
    for column, raw_value in zip(columns, values):
        if raw_value == 'Null':
            record[column] = None
        elif raw_value.startswith("'") and raw_value.endswith("'"):
            record[column] = raw_value[1:-1].replace("''", "'")
        else:
            record[column] = raw_value

    return record


def iter_postgresql_data_script_lines(pg_zip_path: Path):
    with zipfile.ZipFile(pg_zip_path, 'r') as zf:
        script_name = None
        for name in zf.namelist():
            if Path(name).name == 'PostgreSQL_Data_Script.sql':
                script_name = name
                break

        if script_name is None:
            raise FileNotFoundError(f'PostgreSQL_Data_Script.sql not found in {pg_zip_path}.')

        with zf.open(script_name, 'r') as handle:
            for raw_line in handle:
                yield raw_line.decode('utf-8').strip()


def extract_postgresql_operation_support_data(pg_zip_path: Path):
    extent_bounds = {}
    usage_extents_by_operation = {}
    concat_steps_by_operation = {}

    for line in iter_postgresql_data_script_lines(pg_zip_path):
        if line.startswith('INSERT INTO epsg_extent '):
            record = parse_named_sql_insert(line, 'extent')
            if record is None:
                continue

            south_value = record.get('bbox_south_bound_lat')
            north_value = record.get('bbox_north_bound_lat')
            west_value = record.get('bbox_west_bound_lon')
            east_value = record.get('bbox_east_bound_lon')
            if south_value is None or north_value is None or west_value is None or east_value is None:
                continue

            try:
                extent_code = int(record['extent_code'])
                south = float(south_value)
                north = float(north_value)
                west = float(west_value)
                east = float(east_value)
            except (KeyError, TypeError, ValueError):
                continue

            extent_bounds[extent_code] = (south, north, west, east)
            continue

        if line.startswith('INSERT INTO epsg_usage '):
            record = parse_named_sql_insert(line, 'usage')
            if record is None or record.get('object_table_name') != 'epsg_coordoperation':
                continue

            try:
                operation_code = int(record['object_code'])
                extent_code = int(record['extent_code'])
            except (KeyError, TypeError, ValueError):
                continue

            usage_extents_by_operation.setdefault(operation_code, set()).add(extent_code)
            continue

        if line.startswith('INSERT INTO epsg_coordoperationpath '):
            record = parse_named_sql_insert(line, 'coordoperationpath')
            if record is None:
                continue

            try:
                concat_code = int(record['concat_operation_code'])
                step_code = int(record['single_operation_code'])
                step_index = int(record['op_path_step'])
            except (KeyError, TypeError, ValueError):
                continue

            concat_steps_by_operation.setdefault(concat_code, []).append((step_index, step_code))

    concat_paths = {}
    for concat_code, steps in concat_steps_by_operation.items():
        steps.sort(key=lambda item: item[0])
        concat_paths[concat_code] = [step_code for _, step_code in steps]

    return extent_bounds, usage_extents_by_operation, concat_paths


def _looks_like_wkt(text: str):
    if text is None:
        return False

    stripped = text.lstrip()
    if stripped == '':
        return False

    for ch in stripped:
        if ch.isalpha() or ch == '_':
            continue
        return ch == '['

    return False


def _iter_wkt_nodes(node: WktNode):
    if node is None:
        return

    yield node
    for item in node.items:
        if isinstance(item, WktNode):
            yield from _iter_wkt_nodes(item)


def _child_nodes(node: WktNode, *keywords):
    if node is None:
        return []

    normalized = {keyword.upper() for keyword in keywords} if keywords else None
    children = []
    for item in node.items:
        if not isinstance(item, WktNode):
            continue
        if normalized is None or item.keyword in normalized:
            children.append(item)

    return children


def _first_child(node: WktNode, *keywords):
    children = _child_nodes(node, *keywords)
    if children:
        return children[0]
    return None


def _first_quoted_string(node: WktNode):
    if node is None:
        return None

    for item in node.items:
        if isinstance(item, str) and not isinstance(item, WktIdentifier):
            return item

    return None


def _first_identifier(node: WktNode):
    if node is None:
        return None

    for item in node.items:
        if isinstance(item, WktIdentifier):
            return str(item)

    return None


def _first_numeric(node: WktNode):
    if node is None:
        return None

    for item in node.items:
        if isinstance(item, (int, float)):
            return item

    return None


def _numeric_items(node: WktNode):
    if node is None:
        return []

    return [float(item) for item in node.items if isinstance(item, (int, float))]


def _as_int(value):
    if value is None:
        return None
    if isinstance(value, bool):
        return None
    if isinstance(value, int):
        return value
    if isinstance(value, float):
        if math.isnan(value) or math.isinf(value):
            return None
        return int(value)

    return None


def _epsg_id(node: WktNode):
    if node is None:
        return None

    for id_node in _child_nodes(node, 'ID'):
        if len(id_node.items) < 2:
            continue

        authority = id_node.items[0]
        if isinstance(authority, str) and not isinstance(authority, WktIdentifier) and authority.upper() == 'EPSG':
            return _as_int(id_node.items[1])

    return None


def _collect_unit(unit_node: WktNode, units):
    if unit_node is None:
        return

    unit_code = _epsg_id(unit_node)
    if unit_code is None:
        return

    if unit_node.keyword == 'LENGTHUNIT':
        unit_type = UNIT_LINEAR
    elif unit_node.keyword == 'ANGLEUNIT':
        unit_type = UNIT_ANGULAR
    else:
        return

    unit_name = _first_quoted_string(unit_node) or ''
    values = _numeric_items(unit_node)
    unit_factor = float(values[0]) if values else 1.0

    existing = units.get(unit_code)
    if existing is None:
        units[unit_code] = {
            'code': unit_code,
            'name': unit_name,
            'unit_type': unit_type,
            'factor': unit_factor,
        }
        return

    if existing['name'] == '' and unit_name != '':
        existing['name'] = unit_name
    if existing['factor'] == 1.0 and unit_factor != 1.0:
        existing['factor'] = unit_factor


def _collect_ellipsoid(ellipsoid_node: WktNode, units, ellipsoids):
    if ellipsoid_node is None:
        return

    ellipsoid_code = _epsg_id(ellipsoid_node)
    if ellipsoid_code is None:
        return

    unit_node = _first_child(ellipsoid_node, 'LENGTHUNIT')
    if unit_node is not None:
        _collect_unit(unit_node, units)
    unit_code = _epsg_id(unit_node) if unit_node is not None else -1
    if unit_code is None:
        unit_code = -1

    name = _first_quoted_string(ellipsoid_node) or ''
    values = _numeric_items(ellipsoid_node)
    semi_major = float(values[0]) if values else 0.0
    inverse_flattening = float(values[1]) if len(values) > 1 else 0.0
    use_ivf = inverse_flattening > 0.0 and not math.isinf(inverse_flattening)
    if use_ivf:
        semi_minor = semi_major * (1.0 - (1.0 / inverse_flattening))
    else:
        semi_minor = semi_major

    existing = ellipsoids.get(ellipsoid_code)
    if existing is None:
        ellipsoids[ellipsoid_code] = {
            'code': ellipsoid_code,
            'name': name,
            'semi_major': semi_major,
            'semi_minor': semi_minor,
            'inv_flattening': inverse_flattening,
            'use_ivf': use_ivf,
            'unit_code': unit_code,
        }
        return

    if existing['name'] == '' and name != '':
        existing['name'] = name
    if existing['unit_code'] < 0 and unit_code >= 0:
        existing['unit_code'] = unit_code


def _collect_prime_meridian(prime_meridian_node: WktNode, units, prime_meridians):
    if prime_meridian_node is None:
        return

    prime_meridian_code = _epsg_id(prime_meridian_node)
    if prime_meridian_code is None:
        return

    unit_node = _first_child(prime_meridian_node, 'ANGLEUNIT')
    if unit_node is not None:
        _collect_unit(unit_node, units)
    unit_code = _epsg_id(unit_node) if unit_node is not None else -1
    if unit_code is None:
        unit_code = -1

    name = _first_quoted_string(prime_meridian_node) or ''
    values = _numeric_items(prime_meridian_node)
    longitude = float(values[0]) if values else 0.0

    existing = prime_meridians.get(prime_meridian_code)
    if existing is None:
        prime_meridians[prime_meridian_code] = {
            'code': prime_meridian_code,
            'name': name,
            'longitude': longitude,
            'unit_code': unit_code,
        }
        return

    if existing['name'] == '' and name != '':
        existing['name'] = name
    if existing['unit_code'] < 0 and unit_code >= 0:
        existing['unit_code'] = unit_code


def _collect_geodetic_datum(datum_node: WktNode, units, ellipsoids, prime_meridians, geodetic_datums):
    if datum_node is None:
        return

    datum_code = _epsg_id(datum_node)
    if datum_code is None:
        return

    ellipsoid_node = _first_child(datum_node, 'ELLIPSOID')
    if ellipsoid_node is not None:
        _collect_ellipsoid(ellipsoid_node, units, ellipsoids)
    ellipsoid_code = _epsg_id(ellipsoid_node) if ellipsoid_node is not None else -1
    if ellipsoid_code is None:
        ellipsoid_code = -1

    prime_meridian_node = _first_child(datum_node, 'PRIMEM', 'PRIMEMERIDIAN')
    if prime_meridian_node is not None:
        _collect_prime_meridian(prime_meridian_node, units, prime_meridians)
    prime_meridian_code = _epsg_id(prime_meridian_node) if prime_meridian_node is not None else -1
    if prime_meridian_code is None:
        prime_meridian_code = -1

    name = _first_quoted_string(datum_node) or ''
    existing = geodetic_datums.get(datum_code)
    if existing is None:
        geodetic_datums[datum_code] = {
            'code': datum_code,
            'name': name,
            'ellipsoid_code': ellipsoid_code,
            'prime_meridian_code': prime_meridian_code,
        }
        return

    if existing['name'] == '' and name != '':
        existing['name'] = name
    if existing['ellipsoid_code'] < 0 and ellipsoid_code >= 0:
        existing['ellipsoid_code'] = ellipsoid_code
    if existing['prime_meridian_code'] < 0 and prime_meridian_code >= 0:
        existing['prime_meridian_code'] = prime_meridian_code


def _collect_vertical_datum(vertical_datum_node: WktNode, vertical_datums):
    if vertical_datum_node is None:
        return

    datum_code = _epsg_id(vertical_datum_node)
    if datum_code is None:
        return

    name = _first_quoted_string(vertical_datum_node) or ''
    existing = vertical_datums.get(datum_code)
    if existing is None:
        vertical_datums[datum_code] = {'code': datum_code, 'name': name}
        return

    if existing['name'] == '' and name != '':
        existing['name'] = name


def _update_datum_prime_meridian_bindings(root_node: WktNode, units, prime_meridians, geodetic_datums):
    for node in _iter_wkt_nodes(root_node):
        if node.keyword not in {'GEOGCRS', 'GEODCRS', 'BASEGEOGCRS', 'BASEGEODCRS'}:
            continue

        datum_node = _first_child(node, 'DATUM', 'ENSEMBLE')
        datum_code = _epsg_id(datum_node)
        if datum_code is None:
            continue

        prime_meridian_node = _first_child(node, 'PRIMEM', 'PRIMEMERIDIAN')
        if prime_meridian_node is None:
            continue

        _collect_prime_meridian(prime_meridian_node, units, prime_meridians)
        prime_meridian_code = _epsg_id(prime_meridian_node)
        if prime_meridian_code is None:
            continue

        datum = geodetic_datums.get(datum_code)
        if datum is None:
            continue

        if datum['prime_meridian_code'] < 0:
            datum['prime_meridian_code'] = prime_meridian_code


def _extract_coordinate_system(root_node: WktNode, units, coordinate_systems, axes_by_cs):
    cs_node = _first_child(root_node, 'CS')
    if cs_node is None:
        return

    cs_code = _epsg_id(cs_node)
    if cs_code is None:
        return

    cs_type = (_first_identifier(cs_node) or '').lower()
    dimension = _as_int(_first_numeric(cs_node))
    if dimension is None:
        dimension = 0

    existing_cs = coordinate_systems.get(cs_code)
    if existing_cs is None:
        coordinate_systems[cs_code] = {'type': cs_type, 'dimension': dimension}
    else:
        if existing_cs['type'] == '' and cs_type != '':
            existing_cs['type'] = cs_type
        if existing_cs['dimension'] <= 0 and dimension > 0:
            existing_cs['dimension'] = dimension

    default_unit_node = None
    if cs_type in {'ellipsoidal', 'spherical'}:
        default_unit_node = _first_child(root_node, 'ANGLEUNIT')

    if default_unit_node is None:
        default_unit_node = _first_child(root_node, 'LENGTHUNIT', 'ANGLEUNIT')

    if default_unit_node is not None:
        _collect_unit(default_unit_node, units)
    default_unit_code = _epsg_id(default_unit_node) if default_unit_node is not None else -1
    if default_unit_code is None:
        default_unit_code = -1

    axis_map = axes_by_cs.setdefault(cs_code, {})
    axis_nodes = _child_nodes(root_node, 'AXIS')
    for axis_index, axis_node in enumerate(axis_nodes, start=1):
        axis_name = _first_quoted_string(axis_node) or ''
        orientation_token = _first_identifier(axis_node) or ''
        orientation = map_orientation(orientation_token)

        order_node = _first_child(axis_node, 'ORDER')
        axis_order = _as_int(_first_numeric(order_node)) if order_node is not None else axis_index
        if axis_order is None:
            axis_order = axis_index

        axis_unit_node = _first_child(axis_node, 'LENGTHUNIT', 'ANGLEUNIT')
        if axis_unit_node is not None:
            _collect_unit(axis_unit_node, units)
        unit_code = _epsg_id(axis_unit_node) if axis_unit_node is not None else default_unit_code
        if unit_code is None:
            unit_code = default_unit_code
        if unit_code is None:
            unit_code = -1

        axis_map[int(axis_order)] = {
            'order': int(axis_order),
            'name': axis_name,
            'orientation': orientation,
            'unit_code': int(unit_code),
        }


def _collect_projected_conversion(root_node: WktNode, conversion_by_code):
    if root_node.keyword != 'PROJCRS':
        return

    conversion_node = _first_child(root_node, 'CONVERSION')
    if conversion_node is None:
        return

    conversion_code = _epsg_id(conversion_node)
    if conversion_code is None:
        return

    method_node = _first_child(conversion_node, 'METHOD')
    method_name = _first_quoted_string(method_node) or ''
    parameters = []
    for parameter_node in _child_nodes(conversion_node, 'PARAMETER'):
        parameter_name = _first_quoted_string(parameter_node)
        parameter_value = _first_numeric(parameter_node)
        if parameter_name is None or parameter_value is None:
            continue

        parameters.append((parameter_name, float(parameter_value)))

    existing = conversion_by_code.get(conversion_code)
    if existing is None:
        conversion_by_code[conversion_code] = {
            'code': conversion_code,
            'method_name': method_name,
            'parameters': parameters,
        }
        return

    if existing['method_name'] == '' and method_name != '':
        existing['method_name'] = method_name
    if len(existing['parameters']) < len(parameters):
        existing['parameters'] = parameters


def _extract_crs_record(root_node: WktNode, geodetic_crs, projected_crs, vertical_crs, compound_crs):
    srid = _epsg_id(root_node)
    if srid is None:
        return

    name = _first_quoted_string(root_node) or ''
    cs_node = _first_child(root_node, 'CS')
    cs_code = _epsg_id(cs_node) if cs_node is not None else None

    if root_node.keyword in {'GEOGCRS', 'GEODCRS'}:
        datum_node = _first_child(root_node, 'DATUM', 'ENSEMBLE')
        datum_code = _epsg_id(datum_node) if datum_node is not None else None
        if cs_code is None or datum_code is None:
            return

        crs_type = 'geographic 2d'
        if root_node.keyword == 'GEODCRS':
            cs_type = (_first_identifier(cs_node) or '').lower()
            dimension = _as_int(_first_numeric(cs_node))
            if cs_type == 'cartesian':
                crs_type = 'geocentric'
            elif cs_type == 'ellipsoidal' and dimension == 2:
                crs_type = 'geographic 2d'
            elif cs_type == 'ellipsoidal' and dimension == 3:
                crs_type = 'geographic 3d'
            else:
                crs_type = cs_type

        geodetic_crs[srid] = {
            'srid': srid,
            'name': name,
            'type': crs_type,
            'coordinate_system_code': cs_code,
            'datum_code': datum_code,
        }
        return

    if root_node.keyword == 'PROJCRS':
        base_node = _first_child(root_node, 'BASEGEOGCRS', 'BASEGEODCRS')
        conversion_node = _first_child(root_node, 'CONVERSION')
        base_srid = _epsg_id(base_node) if base_node is not None else None
        conversion_code = _epsg_id(conversion_node) if conversion_node is not None else None
        if cs_code is None or base_srid is None or conversion_code is None:
            return

        projected_crs[srid] = {
            'srid': srid,
            'name': name,
            'coordinate_system_code': cs_code,
            'base_srid': base_srid,
            'conversion_code': conversion_code,
        }
        return

    if root_node.keyword == 'VERTCRS':
        vdatum_node = _first_child(root_node, 'VDATUM')
        datum_code = _epsg_id(vdatum_node) if vdatum_node is not None else None
        if cs_code is None or datum_code is None:
            return

        vertical_crs[srid] = {
            'srid': srid,
            'name': name,
            'coordinate_system_code': cs_code,
            'datum_code': datum_code,
        }
        return

    if root_node.keyword == 'COMPOUNDCRS':
        horizontal_srid = None
        vertical_srid = None
        for item in root_node.items:
            if not isinstance(item, WktNode):
                continue
            if not item.keyword.endswith('CRS'):
                continue

            component_srid = _epsg_id(item)
            if component_srid is None:
                continue

            if item.keyword == 'VERTCRS':
                vertical_srid = component_srid
            elif horizontal_srid is None:
                horizontal_srid = component_srid

        if horizontal_srid is None or vertical_srid is None:
            return

        compound_crs[srid] = {
            'srid': srid,
            'name': name,
            'horizontal_srid': horizontal_srid,
            'vertical_srid': vertical_srid,
        }


def load_wkt_data(zip_path: Path):
    crs_pattern = re.compile(r'^EPSG-CRS-(\d+)\.wkt$', re.IGNORECASE)

    units = {}
    coordinate_systems = {}
    axes_by_cs = {}
    ellipsoids = {}
    prime_meridians = {}
    geodetic_datums = {}
    vertical_datums = {}
    conversion_by_code = {}
    geodetic_crs = {}
    projected_crs = {}
    vertical_crs = {}
    compound_crs = {}

    with zipfile.ZipFile(zip_path, 'r') as zip_file:
        for info in sorted(zip_file.infolist(), key=lambda i: i.filename):
            name = Path(info.filename).name
            if not crs_pattern.match(name):
                continue

            text = zip_file.read(info).decode('utf-8', errors='replace')
            if not _looks_like_wkt(text):
                continue

            try:
                root_node = parse_wkt_node(text)
            except ValueError as ex:
                raise ValueError(f'Failed to parse CRS WKT "{name}".') from ex

            for node in _iter_wkt_nodes(root_node):
                if node.keyword in {'LENGTHUNIT', 'ANGLEUNIT'}:
                    _collect_unit(node, units)
                elif node.keyword == 'ELLIPSOID':
                    _collect_ellipsoid(node, units, ellipsoids)
                elif node.keyword in {'PRIMEM', 'PRIMEMERIDIAN'}:
                    _collect_prime_meridian(node, units, prime_meridians)
                elif node.keyword in {'DATUM', 'ENSEMBLE'}:
                    _collect_geodetic_datum(node, units, ellipsoids, prime_meridians, geodetic_datums)
                elif node.keyword == 'VDATUM':
                    _collect_vertical_datum(node, vertical_datums)

            _update_datum_prime_meridian_bindings(root_node, units, prime_meridians, geodetic_datums)
            _collect_projected_conversion(root_node, conversion_by_code)
            _extract_coordinate_system(root_node, units, coordinate_systems, axes_by_cs)
            _extract_crs_record(root_node, geodetic_crs, projected_crs, vertical_crs, compound_crs)

    if 9102 not in units:
        units[9102] = {
            'code': 9102,
            'name': 'degree',
            'unit_type': UNIT_ANGULAR,
            'factor': 0.0174532925199433,
        }

    if 8901 not in prime_meridians:
        prime_meridians[8901] = {
            'code': 8901,
            'name': 'Greenwich',
            'longitude': 0.0,
            'unit_code': 9102,
        }

    for datum in geodetic_datums.values():
        if datum['prime_meridian_code'] < 0:
            datum['prime_meridian_code'] = 8901

    normalized_axes_by_cs = {}
    for cs_code in sorted(axes_by_cs):
        axis_map = axes_by_cs[cs_code]
        normalized_axes_by_cs[cs_code] = [axis_map[order] for order in sorted(axis_map)]

    conversion_parameters = []
    conversions = {}
    for conversion_code in sorted(conversion_by_code):
        conversion = conversion_by_code[conversion_code]
        start = len(conversion_parameters)
        for parameter_name, parameter_value in conversion['parameters']:
            conversion_parameters.append(
                {
                    'conversion_code': conversion_code,
                    'name': parameter_name or '',
                    'value': float(parameter_value),
                }
            )

        conversions[conversion_code] = {
            'code': conversion_code,
            'method_name': conversion['method_name'] or '',
            'start': start,
            'count': len(conversion_parameters) - start,
        }

    return {
        'units': units,
        'coordinate_systems': coordinate_systems,
        'axes_by_cs': normalized_axes_by_cs,
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

    extent_bounds = {}
    usage_extents_by_operation = {}

    extent_insert_pattern = re.compile(
        r"INSERT INTO \"extent\" VALUES\('EPSG','([^']+)','[^']*','[^']*',([^,]+),([^,]+),([^,]+),([^,]+),")
    usage_insert_pattern = re.compile(
        r"INSERT INTO \"usage\" VALUES\('[^']*','[^']*','([^']+)','([^']+)','([^']+)','([^']+)','([^']+)','[^']+','[^']+'\)")
    transform_sql_files = (
        'helmert_transformation.sql',
        'grid_transformation.sql',
        'other_transformation.sql',
        'concatenated_operation.sql',
    )

    proj_sql_root = zip_path.resolve().parent.parent / 'PROJ' / 'data' / 'sql'
    extent_sql_path = proj_sql_root / 'extent.sql'
    if extent_sql_path.exists():
        for line in extent_sql_path.read_text(encoding='utf-8').splitlines():
            extent_match = extent_insert_pattern.search(line)
            if not extent_match:
                continue

            try:
                extent_code = int(extent_match.group(1))
            except ValueError:
                continue

            south_value = extent_match.group(2)
            north_value = extent_match.group(3)
            west_value = extent_match.group(4)
            east_value = extent_match.group(5)
            if south_value == 'NULL' or north_value == 'NULL' or west_value == 'NULL' or east_value == 'NULL':
                continue

            try:
                south = float(south_value)
                north = float(north_value)
                west = float(west_value)
                east = float(east_value)
            except ValueError:
                continue

            extent_bounds[extent_code] = (south, north, west, east)

    if proj_sql_root.exists():
        for sql_name in transform_sql_files:
            sql_path = proj_sql_root / sql_name
            if not sql_path.exists():
                continue

            for line in sql_path.read_text(encoding='utf-8').splitlines():
                usage_match = usage_insert_pattern.search(line)
                if not usage_match:
                    continue

                object_table_name = usage_match.group(1)
                operation_auth = usage_match.group(2)
                try:
                    operation_code = int(usage_match.group(3))
                except ValueError:
                    continue
                extent_auth = usage_match.group(4)
                try:
                    extent_code = int(usage_match.group(5))
                except ValueError:
                    continue

                if operation_auth != 'EPSG' or extent_auth != 'EPSG':
                    continue

                if object_table_name not in ('helmert_transformation', 'grid_transformation', 'other_transformation', 'concatenated_operation'):
                    continue

                usage_extents_by_operation.setdefault(operation_code, set()).add(extent_code)

    def merge_operation_bounds(operation_code: int):
        extents = [
            extent_bounds[extent_code]
            for extent_code in usage_extents_by_operation.get(operation_code, set())
            if extent_code in extent_bounds]
        if not extents:
            return float('nan'), float('nan'), float('nan'), float('nan')

        south = min(value[0] for value in extents)
        north = max(value[1] for value in extents)
        west = min(value[2] for value in extents)
        east = max(value[3] for value in extents)
        return south, north, west, east

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

            area_south_latitude, area_north_latitude, area_west_longitude, area_east_longitude = merge_operation_bounds(operation_code)

            parsed_operations.append({
                'operation_type': operation_type,
                'operation_code': operation_code,
                'source_srid': int(src_ids[-1]),
                'target_srid': int(tgt_ids[-1]),
                'accuracy': accuracy,
                'method_name': method_name,
                'parameter_file_name': parameter_file_name,
                'parameters': parameters,
                'area_south_latitude': area_south_latitude,
                'area_north_latitude': area_north_latitude,
                'area_west_longitude': area_west_longitude,
                'area_east_longitude': area_east_longitude,
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
            record['area_south_latitude'],
            record['area_north_latitude'],
            record['area_west_longitude'],
            record['area_east_longitude'],
            parameter_start_index,
            parameter_count,
        ))

        normalized_method = normalize_operation_method_name(record['method_name'])
        supports_explicit_helmert = (
            'geocentrictranslations' in normalized_method
            or 'positionvectortransformation' in normalized_method
            or 'coordinateframerotation' in normalized_method
            or 'molodensky' in normalized_method
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
    used_conversions = {r[4] for r in projected_records}
    for code in sorted(used_conversions):
        c = conversions[code]
        conversion_parameters = []
        for i in range(c['count']):
            parameter = data['conversion_parameters'][c['start'] + i]
            conversion_parameters.append((parameter['name'], parameter['value']))

        conversion_records.append((code, c['method_name'], conversion_parameters))


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
        'geographic_records': geographic_records,
        'geocentric_records': geocentric_records,
        'projected_records': projected_records,
        'vertical_records': vertical_records,
        'compound_records': compound_records,
        'ref_records': ref_records,
    }


def emit(output_path: Path, zip_name: str, catalog, operations, operation_parameters, explicit_operations):
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
        'EpsgConversionRecord': 'int code, string methodName, int parameterCount',
        'EpsgConversionParameterRecord': 'string name, double value',
        'EpsgOperationRecord': 'EpsgOperationType operationType, int operationCode, int sourceSrid, int targetSrid, double accuracy, string methodName, string parameterFileName, double areaSouthLatitude, double areaNorthLatitude, double areaWestLongitude, double areaEastLongitude, int parameterStartIndex, int parameterCount',
        'EpsgOperationParameterRecord': 'int operationCode, string name, double value',
        'EpsgExplicitOperationRecord': 'int operationCode, double dx, double dy, double dz, double ex, double ey, double ez, double ppm',
    }

    output_name = output_path.name
    if output_name.endswith('.g.cs'):
        base_name = output_name[:-5]
    else:
        base_name = output_path.stem

    types_path = output_path.with_name(f'{base_name}.Types.g.cs')
    projected_path = output_path.with_name(f'{base_name}.Projected.g.cs')
    conversions_path = output_path.with_name(f'{base_name}.Conversions.g.cs')
    operations_path = output_path.with_name(f'{base_name}.Operations.g.cs')

    def create_file_lines():
        lines = []
        lines.append('// <auto-generated>')
        lines.append('// Generated by tools\\Generate-EpsgManagedData.ps1')
        lines.append(f'// Source: {zip_name}')
        lines.append('// </auto-generated>')
        lines.append('#pragma warning disable SA0001, SA1512, SA1518, SA1600, SA1614, SA1616, SA1633, SA1636')
        lines.append('using System;')
        lines.append('')
        lines.append('namespace ProjNet.Data.Generated')
        lines.append('{')
        return lines

    def write_generated_file(path: Path, lines):
        lines.append('}')
        path.write_text('\n'.join(lines) + '\n', encoding='utf-8')

    def begin_catalog_partial(lines):
        lines.append('    internal static partial class EpsgGeneratedCatalog')
        lines.append('    {')

    def end_catalog_partial(lines):
        lines.append('    }')

    def emit_array(lines, name, type_name, values, fmt):
        lines.append(f'        internal static readonly {type_name}[] {name} = new {type_name}[]')
        lines.append('        {')
        for value in values:
            lines.append('            new ' + type_name + '(' + fmt(value) + '),')
        lines.append('        };')
        lines.append('')

    def emit_switch_factory(lines, method_name, type_name, values, fmt):
        lines.append(f'        internal static bool {method_name}(int index, out {type_name} record)')
        lines.append('        {')
        lines.append('            switch (index)')
        lines.append('            {')
        for index, value in enumerate(values):
            lines.append(f'                case {index}:')
            lines.append(f'                    record = new {type_name}({fmt(value)});')
            lines.append('                    return true;')
        lines.append('                default:')
        lines.append('                    record = default;')
        lines.append('                    return false;')
        lines.append('            }')
        lines.append('        }')
        lines.append('')

    def emit_projected_switch_factory(lines, values, fmt):
        buckets = {}
        for index, value in enumerate(values):
            bucket = index // 1000
            buckets.setdefault(bucket, []).append((index, value))

        lines.append('        internal static bool TryGetProjectedCrs(int index, out EpsgProjectedCrsRecord record)')
        lines.append('        {')
        lines.append('            switch (index / 1000)')
        lines.append('            {')
        for bucket in sorted(buckets):
            lines.append(f'                case {bucket}:')
            lines.append(f'                    return TryGetProjectedCrsBucket{bucket}(index, out record);')
        lines.append('                default:')
        lines.append('                    record = default;')
        lines.append('                    return false;')
        lines.append('            }')
        lines.append('        }')
        lines.append('')

        for bucket in sorted(buckets):
            lines.append(f'        private static bool TryGetProjectedCrsBucket{bucket}(int index, out EpsgProjectedCrsRecord record)')
            lines.append('        {')
            lines.append('            switch (index)')
            lines.append('            {')
            for index, value in buckets[bucket]:
                lines.append(f'                case {index}:')
                lines.append(f'                    record = new EpsgProjectedCrsRecord({fmt(value)});')
                lines.append('                    return true;')
            lines.append('                default:')
            lines.append('                    record = default;')
            lines.append('                    return false;')
            lines.append('            }')
            lines.append('        }')
            lines.append('')

    def emit_conversion_switch_factories(lines, values):
        buckets = {}
        for value in values:
            bucket = value[0] // 1000
            buckets.setdefault(bucket, []).append(value)

        lines.append('        internal static bool TryGetConversion(int code, out EpsgConversionRecord record)')
        lines.append('        {')
        lines.append('            switch (code / 1000)')
        lines.append('            {')
        for bucket in sorted(buckets):
            lines.append(f'                case {bucket}:')
            lines.append(f'                    return TryGetConversionBucket{bucket}(code, out record);')
        lines.append('                default:')
        lines.append('                    record = default;')
        lines.append('                    return false;')
        lines.append('            }')
        lines.append('        }')
        lines.append('')

        for bucket in sorted(buckets):
            lines.append(f'        private static bool TryGetConversionBucket{bucket}(int code, out EpsgConversionRecord record)')
            lines.append('        {')
            lines.append('            switch (code)')
            lines.append('            {')
            for code, method_name, parameters in buckets[bucket]:
                lines.append(f'                case {code}:')
                lines.append(f'                    record = new EpsgConversionRecord({code}, "{esc(method_name)}", {len(parameters)});')
                lines.append('                    return true;')
            lines.append('                default:')
            lines.append('                    record = default;')
            lines.append('                    return false;')
            lines.append('            }')
            lines.append('        }')
            lines.append('')

        lines.append('        internal static bool TryGetConversionParameter(int conversionCode, int parameterIndex, out EpsgConversionParameterRecord parameter)')
        lines.append('        {')
        lines.append('            switch (conversionCode / 1000)')
        lines.append('            {')
        for bucket in sorted(buckets):
            lines.append(f'                case {bucket}:')
            lines.append(f'                    return TryGetConversionParameterBucket{bucket}(conversionCode, parameterIndex, out parameter);')
        lines.append('                default:')
        lines.append('                    parameter = default;')
        lines.append('                    return false;')
        lines.append('            }')
        lines.append('        }')
        lines.append('')

        for bucket in sorted(buckets):
            lines.append(f'        private static bool TryGetConversionParameterBucket{bucket}(int conversionCode, int parameterIndex, out EpsgConversionParameterRecord parameter)')
            lines.append('        {')
            lines.append('            switch (conversionCode)')
            lines.append('            {')
            for code, _, parameters in buckets[bucket]:
                lines.append(f'                case {code}:')
                lines.append('                    switch (parameterIndex)')
                lines.append('                    {')
                for parameter_index, parameter_record in enumerate(parameters):
                    lines.append(f'                        case {parameter_index}:')
                    lines.append(f'                            parameter = new EpsgConversionParameterRecord("{esc(parameter_record[0])}", {repr(parameter_record[1])}d);')
                    lines.append('                            return true;')
                lines.append('                        default:')
                lines.append('                            break;')
                lines.append('                    }')
                lines.append('                    break;')
            lines.append('                default:')
            lines.append('                    break;')
            lines.append('            }')
            lines.append('')
            lines.append('            parameter = default;')
            lines.append('            return false;')
            lines.append('        }')
            lines.append('')

    fmt_geographic_crs = lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}"
    fmt_geocentric_crs = lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}"
    fmt_projected_crs = lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}, {value[4]}"
    fmt_vertical_crs = lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}"
    fmt_compound_crs = lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}"

    def fmt_operation(value):
        accuracy = 'double.NaN' if math.isnan(value[4]) else f'{repr(value[4])}d'
        area_south_latitude = 'double.NaN' if math.isnan(value[7]) else f'{repr(value[7])}d'
        area_north_latitude = 'double.NaN' if math.isnan(value[8]) else f'{repr(value[8])}d'
        area_west_longitude = 'double.NaN' if math.isnan(value[9]) else f'{repr(value[9])}d'
        area_east_longitude = 'double.NaN' if math.isnan(value[10]) else f'{repr(value[10])}d'
        return (
            f"(EpsgOperationType){value[0]}, {value[1]}, {value[2]}, {value[3]}, {accuracy}, "
            f"\"{esc(value[5])}\", \"{esc(value[6])}\", {area_south_latitude}, {area_north_latitude}, {area_west_longitude}, {area_east_longitude}, {value[11]}, {value[12]}"
        )

    types_lines = create_file_lines()
    types_lines.append('    internal enum EpsgOperationType : byte { Transformation = 0, ConcatenatedOperation = 1, PointMotionOperation = 2 }')
    types_lines.append('    internal enum EpsgCoordinateSystemKind : byte { Geographic2D = 0, Geocentric = 1, Projected = 2, Vertical = 3, Compound = 4 }')
    types_lines.append('')

    for name, args in struct_defs.items():
        types_lines.append(f'    internal readonly struct {name}')
        types_lines.append('    {')
        constructor_args = ', '.join(part.strip() for part in args.split(','))
        types_lines.append(f'        internal {name}({constructor_args})')
        types_lines.append('        {')
        for part in args.split(','):
            variable_name = part.strip().split(' ')[-1]
            property_name = variable_name[0].upper() + variable_name[1:]
            types_lines.append(f'            {property_name} = {variable_name};')
        types_lines.append('        }')
        types_lines.append('')
        for part in args.split(','):
            property_type, variable_name = part.strip().rsplit(' ', 1)
            property_name = variable_name[0].upper() + variable_name[1:]
            types_lines.append(f'        internal {property_type} {property_name} {{ get; }}')
        types_lines.append('    }')
        types_lines.append('')

    write_generated_file(types_path, types_lines)

    core_lines = create_file_lines()
    begin_catalog_partial(core_lines)
    core_lines.append(f'        internal const string SourceArchive = "{esc(zip_name)}";')
    core_lines.append(f'        internal const int CoordinateReferenceCount = {len(catalog["ref_records"])};')
    core_lines.append(f'        private static readonly int[] CoordinateSridByCacheIndex = new int[] {{ {", ".join(str(ref_record[0]) for ref_record in catalog["ref_records"])} }};')
    core_lines.append('')

    emit_switch_factory(core_lines, 'TryGetGeographicCrs', 'EpsgGeographicCrsRecord', catalog['geographic_records'], fmt_geographic_crs)
    emit_switch_factory(core_lines, 'TryGetGeocentricCrs', 'EpsgGeocentricCrsRecord', catalog['geocentric_records'], fmt_geocentric_crs)
    emit_switch_factory(core_lines, 'TryGetVerticalCrs', 'EpsgVerticalCrsRecord', catalog['vertical_records'], fmt_vertical_crs)
    emit_switch_factory(core_lines, 'TryGetCompoundCrs', 'EpsgCompoundCrsRecord', catalog['compound_records'], fmt_compound_crs)

    emit_array(core_lines, 'Units', 'EpsgUnitRecord', catalog['unit_records'], lambda value: f"{value[0]}, {value[1]}, {repr(value[2])}d, \"{esc(value[3])}\"")
    emit_array(core_lines, 'Axes', 'EpsgAxisRecord', catalog['axis_records'], lambda value: f"{value[0]}, (byte){value[1]}, \"{esc(value[2])}\", (sbyte){value[3]}, {value[4]}")
    emit_array(core_lines, 'Ellipsoids', 'EpsgEllipsoidRecord', catalog['ellipsoid_records'], lambda value: f"{value[0]}, \"{esc(value[1])}\", {repr(value[2])}d, {repr(value[3])}d, {repr(value[4])}d, {'true' if value[5] else 'false'}, {value[6]}")
    emit_array(core_lines, 'PrimeMeridians', 'EpsgPrimeMeridianRecord', catalog['prime_meridian_records'], lambda value: f"{value[0]}, \"{esc(value[1])}\", {repr(value[2])}d, {value[3]}")
    emit_array(core_lines, 'GeodeticDatums', 'EpsgGeodeticDatumRecord', catalog['geodetic_datum_records'], lambda value: f"{value[0]}, \"{esc(value[1])}\", {value[2]}, {value[3]}")
    emit_array(core_lines, 'VerticalDatums', 'EpsgVerticalDatumRecord', catalog['vertical_datum_records'], lambda value: f"{value[0]}, \"{esc(value[1])}\"")

    core_lines.append('        internal static bool TryGetCoordinateReference(int srid, out EpsgCoordinateReferenceRecord reference, out int cacheIndex)')
    core_lines.append('        {')
    core_lines.append('            switch (srid)')
    core_lines.append('            {')
    for index, ref_record in enumerate(catalog['ref_records']):
        core_lines.append(f'                case {ref_record[0]}:')
        core_lines.append(f'                    cacheIndex = {index};')
        core_lines.append(f'                    reference = new EpsgCoordinateReferenceRecord({ref_record[0]}, (EpsgCoordinateSystemKind){ref_record[1]}, {ref_record[2]});')
        core_lines.append('                    return true;')
    core_lines.append('                default:')
    core_lines.append('                    cacheIndex = -1;')
    core_lines.append('                    reference = default;')
    core_lines.append('                    return false;')
    core_lines.append('            }')
    core_lines.append('        }')
    core_lines.append('')
    core_lines.append('        internal static bool TryGetCoordinateSridByCacheIndex(int cacheIndex, out int srid)')
    core_lines.append('        {')
    core_lines.append('            if ((uint)cacheIndex < (uint)CoordinateSridByCacheIndex.Length)')
    core_lines.append('            {')
    core_lines.append('                srid = CoordinateSridByCacheIndex[cacheIndex];')
    core_lines.append('                return true;')
    core_lines.append('            }')
    core_lines.append('')
    core_lines.append('            srid = -1;')
    core_lines.append('            return false;')
    core_lines.append('        }')
    end_catalog_partial(core_lines)
    write_generated_file(output_path, core_lines)

    projected_lines = create_file_lines()
    begin_catalog_partial(projected_lines)
    emit_projected_switch_factory(projected_lines, catalog['projected_records'], fmt_projected_crs)
    end_catalog_partial(projected_lines)
    write_generated_file(projected_path, projected_lines)

    conversions_lines = create_file_lines()
    begin_catalog_partial(conversions_lines)
    emit_conversion_switch_factories(conversions_lines, catalog['conversion_records'])
    end_catalog_partial(conversions_lines)
    write_generated_file(conversions_path, conversions_lines)

    operations_lines = create_file_lines()
    begin_catalog_partial(operations_lines)
    emit_array(operations_lines, 'Operations', 'EpsgOperationRecord', operations, fmt_operation)
    emit_array(operations_lines, 'OperationParameters', 'EpsgOperationParameterRecord', operation_parameters, lambda value: f"{value[0]}, \"{esc(value[1])}\", {repr(value[2])}d")
    operations_lines.append('        internal static bool TryGetExplicitOperationParameters(int operationCode, out EpsgExplicitOperationRecord parameters)')
    operations_lines.append('        {')
    operations_lines.append('            switch (operationCode)')
    operations_lines.append('            {')
    for explicit_record in explicit_operations:
        operations_lines.append(f'                case {explicit_record[0]}:')
        operations_lines.append(f'                    parameters = new EpsgExplicitOperationRecord({explicit_record[0]}, {repr(explicit_record[1])}d, {repr(explicit_record[2])}d, {repr(explicit_record[3])}d, {repr(explicit_record[4])}d, {repr(explicit_record[5])}d, {repr(explicit_record[6])}d, {repr(explicit_record[7])}d);')
        operations_lines.append('                    return true;')
    operations_lines.append('                default:')
    operations_lines.append('                    parameters = default;')
    operations_lines.append('                    return false;')
    operations_lines.append('            }')
    operations_lines.append('        }')
    end_catalog_partial(operations_lines)
    write_generated_file(operations_path, operations_lines)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--zip', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()

    zip_path = Path(args.zip)
    output_path = Path(args.output)

    output_path.parent.mkdir(parents=True, exist_ok=True)

    catalog = build_catalog(load_wkt_data(zip_path))
    operations, operation_parameters, explicit_operations = extract_operation_data(zip_path)

    emit(output_path, zip_path.name, catalog, operations, operation_parameters, explicit_operations)

    print(f'Generated: {output_path}')
    print(f"CRS records: {len(catalog['ref_records'])}")
    print(f'Operation records: {len(operations)}')


if __name__ == '__main__':
    main()




