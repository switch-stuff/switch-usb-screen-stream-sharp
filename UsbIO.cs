using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Main;

internal static class UsbIO
{
    private static Rectangle ScreenBounds = Screen.PrimaryScreen.Bounds;
    private static Rectangle FrmBufBounds = new Rectangle(0, 0, 640, 360);

    private const int Size = 640 * 360 * 3;

    private static readonly UsbDeviceFinder Switch = new UsbDeviceFinder(0x57e, 0x3000);

    public struct InputPkg
    {
        public ulong HeldKeys;
        public short LJoyX;
        public short LJoyY;
        public short RJoyX;
        public short RJoyY;
    }

    public enum InputKeys : ulong
    {
        A     = 1,
        B     = 1 << 1,
        X     = 1 << 2,
        Y     = 1 << 3,
        LS    = 1 << 4,
        RS    = 1 << 5,
        L     = 1 << 6,
        R     = 1 << 7,
        ZL    = 1 << 8,
        ZR    = 1 << 9,
        Plus  = 1 << 10,
        Minus = 1 << 11,
        Left  = 1 << 12,
        Up    = 1 << 13,
        Right = 1 << 14,
        Down  = 1 << 15
    }

    public class UsbCtx : IDisposable
    {
        public UsbDevice Switch;
        public UsbEndpointWriter Write;
        public UsbEndpointReader Read;
        public bool IsSet;

        public void Dispose()
        {
            Read.Dispose();
            Write.Dispose();
            Switch.Close();
        }
    }

    public static void SetCtx(UsbCtx ctx)
    {
        ctx.Switch = UsbDevice.OpenUsbDevice(Switch);
        ctx.Write  = ctx.Switch.OpenEndpointWriter(WriteEndpointID.Ep01);
        ctx.Read   = ctx.Switch.OpenEndpointReader(ReadEndpointID.Ep01);
        ctx.IsSet  = true;
    }

    public static InputPkg ReadPkg(UsbCtx ctx)
    {
        if (!ctx.IsSet) SetCtx(ctx);

        var buf = new byte[0x10];
        var pkg = new InputPkg();

        ctx.Read.Read(buf, 1000, out int len);

        pkg.HeldKeys = BitConverter.ToUInt64(buf, 0);
        pkg.LJoyX    = BitConverter.ToInt16(buf, 8);
        pkg.LJoyY    = BitConverter.ToInt16(buf, 10);
        pkg.RJoyX    = BitConverter.ToInt16(buf, 12);
        pkg.RJoyY    = BitConverter.ToInt16(buf, 14);

        return pkg;
    }

    public static int WriteBuf(UsbCtx ctx, byte[] buffer)
    {
        if (!ctx.IsSet) SetCtx(ctx);
        ctx.Write.Write(buffer, 1000, out int len);
        return len;
    }

    public static void WriteScreen(UsbCtx ctx)
    {
        using (var initBmp  = new Bitmap(ScreenBounds.Width, ScreenBounds.Height, PixelFormat.Format24bppRgb))
        using (var graphics = Graphics.FromImage(initBmp))
        {
            graphics.CopyFromScreen(ScreenBounds.Left, ScreenBounds.Top, 0, 0, initBmp.Size, CopyPixelOperation.SourceCopy);
            using (var finalBmp = new Bitmap(initBmp, FrmBufBounds.Width, FrmBufBounds.Height))
            {
                var locked = finalBmp.LockBits(FrmBufBounds, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                WriteBuf(ctx, locked.Scan0.Bytes());
                finalBmp.UnlockBits(locked);
            }
        }
    }

    private static byte[] Bytes(this IntPtr buffer)
    {
        byte[] buf = new byte[Size];
        Marshal.Copy(buffer, buf, 0, Size);

        // Hacky endianness flip
        for (int i = 0; i < buf.Length; i += 3)
        {
            var temp   = buf[i];
            buf[i]     = buf[i + 2];
            buf[i + 2] = temp;
        }

        return buf;
    }

}
