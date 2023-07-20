import base64
from io import BytesIO
from PIL import Image

file = "base64.txt"
# Base64-encoded image data
with open(file, "rb") as file:
    base64_data = file.read()
# Decode base64 to binary data
image_data = base64.b64decode(base64_data)

# Create an image object from binary data
image = Image.open(BytesIO(image_data))

# Display the image
image.show()
image.save("image.png")
