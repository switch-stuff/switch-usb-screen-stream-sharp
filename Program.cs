using ScpDriverInterface;
using static UsbIO;

namespace SwitchUSBScreenStreamSharp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var scp  = new ScpBus();
            var ctrl = new X360Controller();

            scp.PlugIn(1);

            using (var Context = new UsbCtx())
            {
                while (true)
                {
                    WriteScreen(Context);

                    var pkg = ReadPkg(Context);
        
                    void map(InputKeys inkey, X360Buttons outkey)
                    {
                        if ((pkg.HeldKeys & (ulong)inkey) > 0)
                            ctrl.Buttons  |= outkey;
                        else ctrl.Buttons &= ~outkey;
                    }

                    map(InputKeys.A,     X360Buttons.B);
                    map(InputKeys.B,     X360Buttons.A);
                    map(InputKeys.X,     X360Buttons.Y);
                    map(InputKeys.Y,     X360Buttons.X);

                    map(InputKeys.L,     X360Buttons.LeftBumper);
                    map(InputKeys.R,     X360Buttons.RightBumper);

                    map(InputKeys.LS,    X360Buttons.LeftStick);
                    map(InputKeys.RS,    X360Buttons.RightStick);

                    map(InputKeys.Plus,  X360Buttons.Start);
                    map(InputKeys.Minus, X360Buttons.Back);

                    map(InputKeys.Up,    X360Buttons.Up);
                    map(InputKeys.Down,  X360Buttons.Down);
                    map(InputKeys.Left,  X360Buttons.Left);
                    map(InputKeys.Right, X360Buttons.Right);

                    if ((pkg.HeldKeys & (ulong)InputKeys.ZL) > 0)
                        ctrl.LeftTrigger   = byte.MaxValue;
                    else ctrl.LeftTrigger  = byte.MinValue;

                    if ((pkg.HeldKeys & (ulong)InputKeys.ZR) > 0)
                        ctrl.RightTrigger  = byte.MaxValue;
                    else ctrl.RightTrigger = byte.MinValue;

                    ctrl.LeftStickX  = pkg.LJoyX;
                    ctrl.LeftStickY  = pkg.LJoyY;

                    ctrl.RightStickX = pkg.RJoyX;
                    ctrl.RightStickY = pkg.RJoyY;

                    scp.Report(1, ctrl.GetReport());
                }
            }
        }
    }
}