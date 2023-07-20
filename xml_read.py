import xml.etree.ElementTree as et

data = et.parse("test.ghx")
root = data.getroot()

for child in root.findall(".**[@name='Definition']**[@name='DefinitionObjects']**[@name='Object']//"):
    print (child.attrib)
