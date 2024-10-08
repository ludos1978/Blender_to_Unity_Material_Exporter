Script to create and apply override materials in Unity3d from a Blender File.
use a blender file directly in unity and use this script to create unity materials by copying the filesnames of unity materials in a json. Creating and applying new materials in unity. Detects Base-Map, Metallic, Normal and Smoothness values.

Warning!:
- This script has been barely tested.
- This script overwrites files without asking you first (it should only overwrite Materials in the Materials folder next to a Model)!
- This script manipulates 3d model import settings (the material override settings).
- Use at your own risk!

Installation:
- Copy the scripts from the Editor folder in Assets into your project (also in an Editor folder). 
- Open the Material Texture Updater from the Tools dropdown.

Usage:
- Drop the Unity Blender File (no fbx) from the Project/Assets into the selected Model in the "Material Texture Updater" window.
- "Export JSON from Blender" creates a file (filename_materials_data.json) with the material data values next to the 3d model.
- "Create new Materials from JSON" create the new materials in the Materials folder according to the selected json file.
- "Apply Material Remap" apply the materials to the 3d model's override materials.

Reset:
- "Clear Material Remapping for Model" to clear all material remappings.

No warranty at all for anything this script does or doesnt.
