"""Resource consistency checks, independent of the game installation."""
import json
import re
import unittest
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]

class ResourceTests(unittest.TestCase):
    def test_translations_reference_existing_dynamic_vars(self):
        classes = {}
        for path in (ROOT / 'MadScientistCode/Cards').glob('*.cs'):
            text = path.read_text(encoding='utf-8-sig')
            for match in re.finditer(r'public class (\w+) : MadScientistCard(.*?)(?=\npublic class |\Z)', text, re.S):
                name, body = match.groups()
                key = re.sub(r'(?<!^)(?=[A-Z])', '_', name).upper()
                variables = {'IfUpgraded'}
                variables.update(re.findall(r'new (Damage|Block|Cards|Energy)Var\(', body))
                variables.update(re.findall(r'new PowerVar<(\w+)>', body))
                variables.update(re.findall(r'new DynamicVar\("(\w+)"', body))
                classes[key] = variables
        classes['FUSED_CARD'] = classes['FUSED_X_CARD'] = {'Effects'}
        for lang in ('eng', 'zhs'):
            data = json.loads((ROOT / f'MadScientist/localization/{lang}/cards.json').read_text())
            for key, text in data.items():
                if not key.endswith('.description') or not key.startswith('MADSCIENTIST-'):
                    continue
                name = key.removeprefix('MADSCIENTIST-').removesuffix('.description')
                for var in re.findall(r'\{(\w+)', text):
                    self.assertIn(var, classes.get(name, set()), f'{lang}: {key} -> {var}')

    def test_matching_translation_keys(self):
        paths = [ROOT / f'MadScientist/localization/{lang}/cards.json' for lang in ('eng', 'zhs')]
        self.assertEqual(set(json.loads(paths[0].read_text())), set(json.loads(paths[1].read_text())))

    def test_animation_paths_resolve_to_current_hierarchy(self):
        source = (ROOT / 'MadScientistCode/Visuals/MadScientistVisuals.cs').read_text()
        paths = re.findall(r'AddTrack\(anim, "([^"]+)"\)', source)
        self.assertTrue(paths)
        for path in paths:
            node, prop = path.split(':')
            self.assertIn(node, ('Visuals', 'Visuals/Artwork/Sprite'))
            if prop == 'scale':
                self.assertEqual(node, 'Visuals/Artwork/Sprite')
        self.assertIn('Name = "Artwork", Scale = Vector2.One * VisualScale', source)
        self.assertIn('bounds.Size *= VisualScale', source)

    def test_all_deploy_outputs_use_one_directory(self):
        tree = ET.parse(ROOT / 'MadScientist.csproj')
        for copy in tree.findall('.//Copy'):
            self.assertEqual(copy.attrib['DestinationFolder'].rstrip('/'), '$(ModOutputDir)')
        publish = tree.find(".//Target[@Name='GodotPublish']/Exec")
        self.assertIn('$(ModOutputDir)/$(MSBuildProjectName).pck', publish.attrib['Command'])

if __name__ == '__main__':
    unittest.main()
