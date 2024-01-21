# float t = iX / (float)(TEXTURE_WIDTH - 1);
#
# iX belongs to [0, 511]
# we want to map it to: [0.0, 1.0]

# t0 = 0.00
# t1 = 0,001956947
# t2 = t1 + 0,001956947
...

# 0.00 - 0,001956947

# |   |   |   |   |   |
#   ^
#   d

# imagine a voxel with density: d=0.0005
# you want to determine which color and alpha it was assigned to using the transfer function?
# you know width (in density units) of each bin is: 1 / 511 = 0,001956947
# convert d to Texture index. But how?
# floor(d / 0,001956947 ) we get 0 which is the index of the color associated to d in the
# lookup table


# float4 sample_tf(float3 density) {
#
# }
