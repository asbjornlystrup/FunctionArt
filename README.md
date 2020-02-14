See this video for what it looks like: https://youtu.be/kfbmfZsouOk

This is a Unity project that visualizes random-generated functions in real-time. The functions are represented as abstract syntax trees, which in my case are trees whose leaf nodes are function inputs, and whose other nodes are operators that does a calculation on its children. E.g. addition or the binary xor operator. The calculation trickles upward in the tree until the root node (the final operator node) calculates the final value; the output. This value is modulo'd with 256 to get into a valid color range, and ends up as a color channel value (red, green, or blue) for a pixel of the image. All numbers are unsigned 32-bit integers. The random generation works by first creating a given number of operator nodes at random. It then randomly connects these operator nodes to each other, and finally it creates random leaf nodes to fill the missing operator node inputs. These leaf nodes, or input nodes, are things like the x and y coordinate of the pixel in the image, the index of the color channel (red = 0, green = 1, blue = 2), random constants, polar coordinates of the pixel in the image, and an input image (like the Mona Lisa seen in the YouTube video linked above).

A lot of complexity arises from quite few nodes. For instance, the video has 15 operator nodes in every function, except for in the functions of the final function type, which has 10 operator nodes. Each generated image/video in the YouTube video has one random-generated function associated with it, and all of its pixels run through that function.

See the "Builds" folder for six Windows executables corresponding to the six function types in the video. These can be run even if you don't have Unity.

The Unity project was developed in Unity version 2019.2.13f1 on a Windows 10 laptop with an NVIDIA GTX 1060 GPU. Older GPUs or less powerful GPUs might not be able to run the project well.
