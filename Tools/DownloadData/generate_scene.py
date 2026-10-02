import os
import uuid

def generate_scene():
    template_path = os.path.join(os.path.dirname(__file__), "MainEarthScene_template.unity")
    scene_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Scenes", "MainEarthScene.unity"))

    os.makedirs(os.path.dirname(scene_path), exist_ok=True)

    with open(template_path, "r", encoding="utf-8") as f:
        scene_yaml = f.read()

    with open(scene_path, "w", encoding="utf-8") as f:
        f.write(scene_yaml)

    scene_guid = uuid.uuid4().hex
    scene_meta = f"""fileFormatVersion: 2
guid: {scene_guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(scene_path + ".meta", "w", encoding="utf-8") as f:
        f.write(scene_meta)

    print(f"Created {scene_path} with GUID {scene_guid}")

if __name__ == "__main__":
    generate_scene()
