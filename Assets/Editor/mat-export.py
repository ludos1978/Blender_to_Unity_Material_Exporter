import bpy
import json
import os

print ("V 0.1")

# Steintisch muesste texturen haben, hat aber keine...
# INFO: Steintisch.001 : traversing 'Texture Coordinate' (ShaderNodeTexCoord)
# WARNING: Steintisch.001 : recursion detected in 'Texture Coordinate-ShaderNodeTexCoord' ({'Texture Coordinate-ShaderNodeTexCoord'})
# INFO: Steintisch.001 : traversing 'Material Output' (ShaderNodeOutputMaterial)
# INFO: Steintisch.001 : traversing 'Displacement' (ShaderNodeDisplacement)
# WARNING: Steintisch.001 : recursion detected in 'Displacement-ShaderNodeDisplacement' ({'Displacement-ShaderNodeDisplacement', 'Texture Coordinate-ShaderNodeTexCoord', 'Material Output-ShaderNodeOutputMaterial'})
# INFO: Steintisch.001 : traversing 'Principled BSDF' (ShaderNodeBsdfPrincipled)
# WARNING: Steintisch.001 : recursion detected in 'Principled BSDF-ShaderNodeBsdfPrincipled' ({'Displacement-ShaderNodeDisplacement', 'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Material Output-ShaderNodeOutputMaterial'})
# INFO: Steintisch.001 : traversing 'RockDark001_6K' (ShaderNodeGroup)
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})
# WARNING: Steintisch.001 : recursion detected in 'RockDark001_6K-ShaderNodeGroup' ({'Principled BSDF-ShaderNodeBsdfPrincipled', 'Texture Coordinate-ShaderNodeTexCoord', 'Displacement-ShaderNodeDisplacement', 'RockDark001_6K-ShaderNodeGroup', 'Material Output-ShaderNodeOutputMaterial'})

# Prepare a dictionary to hold material and texture data
material_texture_data = {}
material_other_data = {}

node_name_mappings = {
    "COLOR":"COLOR", 
    "NORMAL":"NORMAL", 
    "METALNESS":"METALNESS", 
    "ROUGHNESS":"ROUGHNESS"
}
link_to_socket_name_mappings = {
    "Base Color": "COLOR",
    "Alpha": "OPACITY",
    "Metallic": "METALNESS",
    "Roughness": "ROUGHNESS",
    "Specular IOR Level": "SPECULAR",
    "A": "COLOR"
}
link_from_node_name_mappings = {
    "NRM": "NORMAL",
    "DISP16": "DISPLACEMENT",
    "AO": "AMBIENT_OCCLUSION",
    "GLOSS": "GLOSS",
    "TRANSMISSION": "ALPHA",
    "BUMP": "BUMP",
    "DISPLACEMENT": "DISPLACEMENT",
    "A": "COLOR",
    "COL": "COLOR",
    "SSS": "SUBSURFACE_SCATTERING" # not sure about this one?
}
link_to_node_name_mappings = {
    "Normal Map": "NORMAL",
    "Displacement": "DISPLACEMENT",
    "COLOR * AO": "COLOR"
}

def replace_starting_double_slash(s):
    if s.startswith('//'):
        return '' + s[2:]  # Remove '//' at the beginning
    return s  # Return the original string if it doesn't start with '//'

# Function to recursively traverse the nodes and extract texture names and channels
def traverse_nodes(node, material_name, visited):
    if f"{node.name}-{node.bl_idname}" in visited:
        print (f"WARNING: {material_name} : recursion detected in '{node.name}-{node.bl_idname}' ({visited})")
        # print (f"Testing {node.bl_rna} {node.bl_rna.identifier} {node.bl_rna.name} {node.bl_rna.name_property}")
        # > Testing <bpy_struct, Struct("ShaderNodeBsdfPrincipled") at 0x106a37300> ShaderNodeBsdfPrincipled Principled BSDF <bpy_struct, StringProperty("name") at 0x106a16bb0>
        # > bl_rna: '__doc__', '__module__', '__slots__', 'base', 'bl_rna', 'description', 'functions', 'identifier', 'input_template', 'is_registered_node_type', 'name', 'name_property', 'nested', 'output_template', 'properties', 'property_tags', 'rna_type', 'translation_context']
        # material_data[material_name] = material_data[]
        return
    else:
        # dir(node)
        # > ['__doc__', '__module__', '__slots__', 'bl_description', 'bl_height_default', 'bl_height_max', 'bl_height_min', 'bl_icon', 'bl_idname', 'bl_label', 'bl_rna', 'bl_static_type', 'bl_width_default', 'bl_width_max', 'bl_width_min', 'color', 'color_mapping', 'dimensions', 'draw_buttons', 'draw_buttons_ext', 'extension', 'height', 'hide', 'image', 'image_user', 'input_template', 'inputs', 'internal_links', 'interpolation', 'is_registered_node_type', 'label', 'location', 'mute', 'name', 'output_template', 'outputs', 'parent', 'poll', 'poll_instance', 'projection', 'projection_blend', 'rna_type', 'select', 'show_options', 'show_preview', 'show_texture', 'socket_value_update', 'texture_mapping', 'type', 'update', 'use_custom_color', 'width']
        print (f"INFO: {material_name} : traversing '{node.name}' ({node.bl_idname}, {node.type})")
        # print (dir(node))
        # break
    
    visited.add(f"{node.name}-{node.bl_idname}")

    # Check if the node is an image texture node
    if node.type == 'TEX_IMAGE' and node.image:
        if node.image.packed_file:  # Check if the image is packed
            image_name = node.image.name  # Just the name if packed
            print (f"ERROR: {material_name} uses packed image {image_name}")
        else:  # It's a linked image
            image_name = replace_starting_double_slash(node.image.filepath)  # Get the full path

        # Identify the channel based on the node's connections
        channels = []
        comments = ""
        for output in node.outputs:
            for link in output.links:
                # connected_node = link.from_node
                # You can customize this to capture specific channel types if needed
                if (node.name in node_name_mappings.keys()):
                    channels.append(node_name_mappings[node.name])
                elif (link.to_socket.name in link_to_socket_name_mappings.keys()):
                    channels.append(link_to_socket_name_mappings[link.to_socket.name])
                elif (link.from_node.name in link_from_node_name_mappings.keys()):
                    channels.append(link_from_node_name_mappings[link.from_node.name])
                elif (link.to_node.name in link_to_node_name_mappings.keys()):
                    channels.append(link_to_node_name_mappings[link.to_node.name])
                else:
                    channels.append("UNDEFINED")
                
                comments += f"| output.name: {output.name}, node.name: {node.name}, \
link.from_socket.name:  {link.from_socket.name}, \
link.to_socket.name: {link.to_socket.name}, \
link.from_node.name: {link.from_node.name}, \
link.to_node.name: {link.to_node.name} |"

        if len(channels) == 1:
            if (image_name!= ""):
                # Append the image name and channels to the material's entry
                if material_name not in material_texture_data:
                    material_texture_data[material_name] = []
                material_texture_data[material_name].append({
                    'image_name': image_name,
                    'channel': channels[0],
                    'comment': comments
                })
                print (f"INFO: {material_name} : node {node.name} has: {image_name} on {channels[0]}")

        elif len(channels) > 1:
            print (f"WARNING: {material_name} : node {node.name} has multiple or no channels: {channels}")
    # Inside the traverse_nodes function, where you have the TODO comment
    elif (node.type == 'GROUP'):
        print(f"INFO: {material_name} : traversing group node '{node.name}' ({node.bl_idname})")
        
        # traverse_nodes(node.node_tree, material_name, visited)

        # Access the node tree of the group
        node_group = node.node_tree

        if node_group:  # Check if the node group exists
            # Iterate over the output nodes of the group
            output_nodes = [n for n in node_group.nodes if n.type == 'OUTPUT_GROUP']
            
            for output_node in output_nodes:
                # Recursively traverse from the output node
                traverse_nodes(output_node, material_name, visited)

            # Optionally, you might also want to traverse all nodes in the group
            for group_node in node_group.nodes:
                if group_node not in visited:  # Avoid recursion within the group
                    traverse_nodes(group_node, material_name, visited)
        else:
            print(f"WARNING: {material_name} : group node '{node.name}' has no node tree")
    elif node.type == 'BSDF_PRINCIPLED':
        # Access the roughness value
        roughness_value = node.inputs['Roughness'].default_value
        print(f"Material: {mat.name}, Roughness: {roughness_value}")
        if material_name not in material_other_data:
            material_other_data[material_name] = {}
        material_other_data[material_name].update({
            'roughness': roughness_value
        })
    else:
        # node is not an image
        pass

    # Recursively check for other nodes connected to the current node
    for output in node.outputs:
        for link in output.links:
            connected_node = link.from_node
            traverse_nodes(connected_node, material_name, visited)

# Loop through all materials in the current Blender file
for mat in bpy.data.materials:
    if mat.use_nodes:
        visited_nodes = set()  # Set to track visited nodes
        for node in mat.node_tree.nodes:  # Iterate over nodes
            traverse_nodes(node, mat.name, visited_nodes)
    else:
        print (f"{mat} is not using nodes")

# Get the current Blender file's name without extension
blend_file_name = bpy.data.filepath
if blend_file_name:
    base_name = os.path.splitext(os.path.basename(blend_file_name))[0]
else:
    base_name = "materials_data"

# convert the data to the output format
material_data_output = []
for material_key, material_value in material_texture_data.items():
    material_data_dict = {
        "materialName": material_key, 
        "textureInfos": material_value
    }
    if (material_key in material_other_data.keys()):
        print (f"Updating {material_data_dict} with {material_other_data[material_key]}")
        material_data_dict.update(material_other_data[material_key])
        # for key, value in material_other_data[material_key].items():
        #     material_data_dict[key](material_other_data[material_key])
    
    material_data_output.append( material_data_dict )



# Set the output file path
output_file_path = os.path.join(os.path.dirname(blend_file_name), f"{base_name}_materials_data.json")

# Save the material data to a JSON file
with open(output_file_path, 'w') as f:
    json.dump({"materials":material_data_output}, f, indent=4)

print(f"Material data saved to {output_file_path}")
