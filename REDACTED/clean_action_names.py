# Run in Blender: Scripting tab > Open this file > Run Script.
# The FBX importer names actions "Armature|ClipName". glTF uses the action name as the
# clip name in Godot, so strip everything before the last "|" and keep every action
# (fake user) so none get dropped on export.

import bpy

renamed = 0
for action in bpy.data.actions:
    if "|" in action.name:
        action.name = action.name.split("|")[-1]
        renamed += 1
    action.use_fake_user = True

print(f"Renamed {renamed} actions; {len(bpy.data.actions)} total.")
