import os
import uuid

def generate_guid():
    return uuid.uuid4().hex

def ensure_meta(path, is_folder=False):
    meta_path = path + ".meta"
    if not os.path.exists(meta_path):
        guid = generate_guid()
        if is_folder:
            content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        else:
            content = f"""fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        with open(meta_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Created meta: {path} -> {guid}")

for root, dirs, files in os.walk("Assets"):
    for d in dirs:
        ensure_meta(os.path.join(root, d), is_folder=True)
    for f in files:
        if not f.endswith(".meta"):
            p = os.path.join(root, f)
            if p.endswith(".cs"):
                ensure_meta(p, is_folder=False)
