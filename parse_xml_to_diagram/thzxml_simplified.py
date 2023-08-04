import xml.etree.ElementTree as ET
import os
from compas import json_dump
import re
import matplotlib.pyplot as plt
import matplotlib.lines as mlines

def parse_xml(xml_string):
    root = ET.fromstring(xml_string)
    return parse_element(root)

def parse_element(element):
    element_data = {}  # Create an empty dictionary to store element data

    # Process element attributes
    element_data['tag'] = element.tag
    element_data['attributes'] = element.attrib

    # Process element text content
    if element.text:
        element_data['text'] = element.text.strip()

    # Process child elements recursively
    children_data = []
    for child in element:
        child_data = parse_element(child)  # Recursively parse child element
        children_data.append(child_data)

    element_data['children'] = children_data

    return element_data

def filter_element(element_data, tag=None, attributes=None, text=None):
    """
    Filter an element by tag, attributes, and/or text content.
    """
    if element_data['tag'] == tag and element_data['attributes']['name'] == attributes:
        elements = element_data['children']
        return elements
    else:
        for children in element_data['children']:
            elements = filter_element(children, tag='chunk', attributes='DefinitionObjects')
            if elements is not None:
                return elements

def generate_tree_string(element_data, indent=0):
    tree_string = ''

    if isinstance(element_data, list):
        for element in element_data:
            children = element['children']

            tree_string += '  ' * indent + f'- {element["tag"]}\n'

            if 'attributes' in element:
                attributes = element['attributes']
                for attr, value in attributes.items():
                    tree_string += '  ' * (indent + 1) + f'- {attr}: {value}\n'

            if 'text' in element:
                text = element['text']
                if text:
                    tree_string += '  ' * (indent + 1) + f'- text: {text}\n'

            if children:
                tree_string += generate_tree_string(children, indent=indent + 1)

    return tree_string

def find_items_by_key(data_dict, key_to_find, value_to_match, result_list=None, prev_dict=None):
    if result_list is None:
        result_list = []

    if isinstance(data_dict, dict):
        if key_to_find in data_dict and data_dict[key_to_find] == value_to_match:
            result_list.extend([data_dict, prev_dict])
        for value in data_dict.values():
            find_items_by_key(value, key_to_find, value_to_match, result_list, data_dict)
    elif isinstance(data_dict, list):
        for item in data_dict:
            find_items_by_key(item, key_to_find, value_to_match, result_list, prev_dict)

    return result_list

def extract_data_to_dict(element_data):
    print("Number of Components = " + element_data[0]['children'][0]['text'])
    component_dict = []
    for components in element_data[1]['children']:
        component = {}
        component["position"] = [find_items_by_key(components, 'name', 'Bounds')[1]["children"][0]["text"], 
                                      find_items_by_key(components, 'name', 'Bounds')[1]["children"][1]["text"]]
        component["size"] = [find_items_by_key(components, 'name', 'Bounds')[1]["children"][2]["text"],
                                   find_items_by_key(components, 'name', 'Bounds')[1]["children"][3]["text"]]
        component["guid"] = find_items_by_key(components, 'name', 'InstanceGuid')[1]['text']
        component["name"] = find_items_by_key(components, 'name', 'Name')[1]['text']
        component["value"] = find_items_by_key(components, 'name', 'Value')[1]['text'] if len(find_items_by_key(components, 'name', 'Value')) > 0 else None

        input = []

        for i in range(1,len(find_items_by_key(find_items_by_key(components, 'name', 'Container'),'name', 'Source')),2):
            input.append(find_items_by_key(find_items_by_key(components, 'name', 'Container'),'name', 'Source')[i]['text'])

        component["input"] = input
        
        #print(find_items_by_key(find_items_by_key(components, 'name', 'Container'),'name', 'Source') if len((find_items_by_key(components, 'name', 'Container'),'name', 'Source'))>0 else None)    

        if component["input"] is not None:
            pass#print(component["input"])

        component["output"] = find_items_by_key(find_items_by_key(components, 'name', 'OutputParam'),'name', 'InstanceGuid')[1]['text'] if len(find_items_by_key(components, 'name', 'InputParam'))>0 else None
        if component["output"] is not None:
            pass#print(component["output"])

        component["pivot"] = [find_items_by_key(components, 'name', 'Pivot')[1]['children'][0]['text'], 
                            find_items_by_key(components, 'name', 'Pivot')[1]['children'][1]['text']]
        component_dict.append(component)

    return component_dict

def plot_rectangle(data):
    position = [int(data['position'][0]), int(data['position'][1])]
    size = [int(data['size'][0]), int(data['size'][1])]
    guid = data['guid']
    name = data['name']
    pivot = [float(data['pivot'][0]), float(data['pivot'][1])]

    # Create a rectangle
    rectangle = plt.Rectangle(position, size[0], size[1], linewidth=1, edgecolor='black', facecolor='lightblue')
    plt.gca().add_patch(rectangle)

    # Add text annotations for guid and name at the center of the rectangle
    center_x = position[0] + size[0] / 2
    center_y = position[1] + size[1] / 2
    plt.text(center_x, center_y, f"Guid: {guid}\nName: {name}", ha='center', va='center')

    # Add text annotations for pivot if available
    if 'pivot' in data:
        plt.text(pivot[0], pivot[1], 'Pivot', ha='center', va='center', color='red')

def plot_lines_between_components(components):
    for component in components:
        output = component['output']
        if output:
            output_position = [int(component['position'][0]) + int(component['size'][0]), int(component['position'][1]) + int(component['size'][1]) / 2]
            for target_component in components:
                input_list = target_component['input']
                if input_list and output in input_list:
                    input_position = [int(target_component['position'][0]), int(target_component['position'][1]) + int(target_component['size'][1]) / 2]
                    line = mlines.Line2D([output_position[0], input_position[0]], [output_position[1], input_position[1]], color='black')
                    plt.gca().add_line(line)

        elif not output:
            output_position = [int(component['position'][0]) + int(component['size'][0]), int(component['position'][1]) + int(component['size'][1]) / 2]
            for target_component in components:
                input_list = target_component['input']
                if input_list and component['guid'] in input_list:
                    input_position = [int(target_component['position'][0]), int(target_component['position'][1]) + int(target_component['size'][1]) / 2]
                    line = mlines.Line2D([output_position[0], input_position[0]], [output_position[1], input_position[1]], color='black')
                    plt.gca().add_line(line)

def draw_diagram(components):
    plt.figure(figsize=(640,480))

    # Plot rectangles for each data dictionary
    for component in components:
        plot_rectangle(component)

    # Plot lines between components with matching output and input
    plot_lines_between_components(components)

    # Remove axis and grid
    plt.axis('off')
    plt.grid(False)

    # Automatically adjust the canvas to fit the content and center it
    plt.gca().autoscale_view()

    # Show the plot
    plt.show()

# Get the current working directory
current_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))

# Specify the XML file name
file_name = 'test.ghx'

# Create the file path by joining the current directory and the file name
xml_file = os.path.join(current_dir, file_name)

# Sample XML string
with open(xml_file, 'r', encoding='utf-8') as xml_file:
    xml_string = xml_file.read()

# Parse XML
parsed_data = parse_xml(xml_string)
elements = filter_element(parsed_data, tag='chunk', attributes='DefinitionObjects')

# Generate the simplified hierarchical tree as a string
tree_string = generate_tree_string(elements)

# Extract objects
data_list = extract_data_to_dict(elements)

# Draw the graph
draw_diagram(data_list)

# Save the tree string to a file
output_json = os.path.join(current_dir, 'tree.json')
output_yaml = os.path.join(current_dir, 'tree.yaml')

#with open(output_yaml, 'w', encoding='utf-8') as file:    
#    file.write(tree_string)
