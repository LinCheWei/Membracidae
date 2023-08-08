import base64
from io import BytesIO
from PIL import Image
import sys
import os

path = os.path.dirname(__file__)
file_r = os.path.join(path, "base64.txt")  

# Base64-encoded image data
with open(file_r, "rb") as file_file:
    base64_data = file_file.read()
# Decode base64 to binary data

# Remove any whitespace or newline characters from the encoded string
#encoded_data = base64_data.replace(" ", "").replace("\n", "")

# Decode the base64-encoded data
decoded_bytes = base64.b64decode(base64_data)

# Optionally, you can save the decoded bytes to a file
with open("output_file.ext", "wb") as output_file:
    output_file.write(decoded_bytes)
