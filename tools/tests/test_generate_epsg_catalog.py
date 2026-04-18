import sys
import unittest
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

import generate_epsg_catalog as generator


class GenerateEpsgCatalogTests(unittest.TestCase):
    def test_bracket_content_ignores_brackets_inside_wkt_strings(self):
        text = 'SOURCECRS[GEOGCRS["A ] ""quoted"" name",ID["EPSG",4326]],REMARK["still inside"]]TARGETCRS[GEOGCRS["Target",ID["EPSG",4979]]]'

        block = generator.bracket_content(text, 'SOURCECRS[')

        self.assertEqual('GEOGCRS["A ] ""quoted"" name",ID["EPSG",4326]],REMARK["still inside"]', block)

    def test_emit_wrapped_int_array_splits_values_across_multiple_lines(self):
        lines = []

        generator.emit_wrapped_int_array(
            lines,
            '        private static readonly int[] Values = new int[]',
            [1, 2, 3, 4, 5, 6, 7],
            values_per_line=3)

        self.assertEqual(
            [
                '        private static readonly int[] Values = new int[]',
                '        {',
                '            1, 2, 3,',
                '            4, 5, 6,',
                '            7,',
                '        };',
                '',
            ],
            lines)


if __name__ == '__main__':
    unittest.main()
