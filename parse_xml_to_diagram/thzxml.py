import xml.etree.ElementTree as ET
import os

# Get the current working directory
current_dir = os.path.dirname(os.path.realpath(__file__))

# Specify the XML file name
file_name = 'thz.ghx'

# Create the file path by joining the current directory and the file name
xml_file = os.path.join(current_dir, file_name)

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

# Sample XML string
with open(xml_file, 'r', encoding='utf-8') as xml_file:
    xml_string = xml_file.read()

# Parse XML
parsed_data = parse_xml(xml_string)

# Print the simplified hierarchical tree
def print_tree(element_data, indent=0):
    print('  ' * indent + f'- {element_data["tag"]}')

    if 'attributes' in element_data:
        attributes = element_data['attributes']
        for attr, value in attributes.items():
            print('  ' * (indent + 1) + f'- {attr}: {value}')

    if 'text' in element_data:
        text = element_data['text']
        if text:
            print('  ' * (indent + 1) + f'- text: {text}')

    if isinstance(element_data.get('children'), list):
        children = element_data['children']
        for child_data in children:
            print_tree(child_data, indent=indent + 1)

print_tree(parsed_data)
