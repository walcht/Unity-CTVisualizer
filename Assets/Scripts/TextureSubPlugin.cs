using System.Runtime.InteropServices;
using System;

public static class TextureSubPlugin {
    public enum Event {
        TextureSubImage2D = 0,
        TextureSubImage3D = 1,
        CreateTexture3D = 2,
        ClearTexture3D = 3
    };
    public enum Format {
        R8_UINT = 0,
        R16_UINT = 1
    }
    [DllImport("TextureSubPlugin")]
    public static extern IntPtr GetRenderEventFunc();

    [DllImport("TextureSubPlugin")]
    public static extern void UpdateTextureSubImage3DParams(
        IntPtr texture_handle,
        Int32 xoffset,
        Int32 yoffset,
        Int32 zoffset,
        Int32 width,
        Int32 height,
        Int32 depth,
        IntPtr data_ptr,
        Int32 level,
        Int32 format
    );

    [DllImport("TextureSubPlugin")]
    public static extern void UpdateCreateTexture3DParams(
       UInt32 width,
       UInt32 height,
       UInt32 depth,
       Int32 format
    );

    [DllImport("TextureSubPlugin")]
    public static extern void UpdateClearTexture3DParams(
        IntPtr texture_handle
    );

    [DllImport("TextureSubPlugin")]
    public static extern IntPtr RetrieveCreatedTexture3D();

};