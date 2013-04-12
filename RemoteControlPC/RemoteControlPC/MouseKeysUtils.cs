using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
namespace RemoteControlPC
{
    class MouseKeysUtils
    {
        #region Reference external library
        [Flags]
        enum MouseEventFlag : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            XDown = 0x0080,
            XUp = 0x0100,
            Wheel = 0x0800,
            VirtualDesk = 0x4000,
            Absolute = 0x8000
        }
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int
          wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteDC(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteObject(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pt);
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlag flags, int dx, int dy, uint data, UIntPtr extraInfo);
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();
        #endregion

        #region Method for public static
        #region Process Commands Function
        public static void processCommand(String command)
        {
            try
            {
                if (command != null && command.StartsWith(GlobalConstants.DOLLAR))//Execute Dos/Shell commands
                {
                    executCommand(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.MOVEMOUSE.ToLower()))
                {
                    mouseMove(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.MOVEADD.ToLower()))
                {
                    mouseAdd(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.MOUSEDOWN.ToLower()))
                {
                    mouseDown();
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.MOUSEUP.ToLower()))
                {
                    mouseUp();
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.MOUSEWHEEL.ToLower()))
                {
                    mouseWheel(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.CLICK.ToLower()))
                {
                    Point point = getcorsorPos();
                    mouse_event(MouseEventFlag.LeftDown, point.X, point.Y, 0, UIntPtr.Zero);
                    mouse_event(MouseEventFlag.LeftUp, point.X, point.Y, 0, UIntPtr.Zero);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.KEYDOWN.ToLower()))
                {

                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.KEYUP.ToLower()))
                {
                    keyUp(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.POUND.ToLower()))//Execute Customer Define Commands
                {
                    customerDefine(command);
                }
                else if (command != null && command.ToLower().StartsWith(GlobalConstants.RIGHTCLICK.ToLower()))
                {
                    Point point = getcorsorPos();
                    //mouse_event(MouseEventFlag.RightDown, point.X, point.Y, 0, UIntPtr.Zero);
                    mouse_event(MouseEventFlag.RightUp, point.X, point.Y, 0, UIntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message.ToString());
            }
        }
        #endregion

        #region Function for Catching Current Screen with specified size
        public static byte[] catchScreen(int width, int height)
        {
            byte[] screen = null;
            try
            {
                Size sz = Screen.PrimaryScreen.Bounds.Size;
                IntPtr hDesk = GetDesktopWindow();
                IntPtr hSrce = GetWindowDC(hDesk);
                IntPtr hDest = CreateCompatibleDC(hSrce);
                IntPtr hBmp = CreateCompatibleBitmap(hSrce, sz.Width, sz.Height);
                IntPtr hOldBmp = SelectObject(hDest, hBmp);
                bool b = BitBlt(hDest, 0, 0, sz.Width, sz.Height, hSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                Bitmap bmp = Bitmap.FromHbitmap(hBmp);
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
                Bitmap newbitmap = new System.Drawing.Bitmap(bmp, width, height);
                System.IO.MemoryStream memorryStre = new System.IO.MemoryStream();
                newbitmap.Save(memorryStre, ImageFormat.Png);
                screen = memorryStre.ToArray();
                bmp.Dispose();
                newbitmap.Dispose();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message.ToString());
            }
            return screen;
        }
        #endregion
        #endregion

        #region Methods for private static
        #region Function for executing dos/shell commands
        private static void executCommand(String command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            if (command != null && command.StartsWith(GlobalConstants.DOLLAR))
            {
                command = (command.Substring(1)).Trim();
            }
            process.StandardInput.WriteLine(command.Trim());
        }
        #endregion

        #region Function for moving mouse position
        private static void mouseMove(String command)
        {
            if (command.IndexOf(GlobalConstants.COLON) > 0)
            {

                String[] move = command.Trim().Split('|');
                String[] pos = move[move.Length - 1].Split(':');
                int x = -5000, y = -5000;
                try
                {

                    x = Convert.ToInt32(pos[1]);
                    y = Convert.ToInt32(pos[2]);
                }
                catch (Exception emovemouse)
                {
                    System.Console.WriteLine("emovemouse: " + emovemouse.Message.ToString() + " cmd value: " + command);
                }
                if (x != -5000 && y != -5000)
                {
                    moveMouse(x, y);
                }
            }
        }
        #endregion

        #region Function for moving mouse position via adding increment
        private static void mouseAdd(String command)
        {
            if (command.IndexOf(GlobalConstants.COLON) > 0)
            {
                String[] move = command.Trim().Split('|');
                String[] pos = move[move.Length - 1].Split(':');
                int x = -5000, y = -5000;
                try
                {
                    x = Convert.ToInt32(pos[1]);
                    y = Convert.ToInt32(pos[2]);
                }
                catch (Exception emoveadd)
                {
                    System.Console.WriteLine("emoveadd: " + emoveadd.Message.ToString());
                }
                if (x != -5000 && y != -5000)
                {
                    Point point = getcorsorPos();
                    moveMouse(point.X + x, point.Y + y);
                }
            }
        }
        #endregion

        #region Function for mouse down event
        private static void mouseDown()
        {
            Point point = getcorsorPos();
            mouse_event(MouseEventFlag.LeftDown, point.X, point.Y, 0, UIntPtr.Zero);
        }
        #endregion

        #region Function for mouse up event
        private static void mouseUp()
        {
            Point point = getcorsorPos();
            mouse_event(MouseEventFlag.LeftUp, point.X, point.Y, 0, UIntPtr.Zero);
        }
        #endregion

        #region Function for MouseWheel
        private static void mouseWheel(String command)
        {
            if (command.IndexOf(GlobalConstants.COLON) >= 0)
            {
                String[] paras = command.Split(':');
                Point point = getcorsorPos();
                mouse_event(MouseEventFlag.Wheel, point.X, point.Y, Convert.ToUInt32(paras[1]), UIntPtr.Zero);
            }
        }
        #endregion

        #region Function for Keyboard up
        private static void keyUp(String command)
        {
            if (command.IndexOf(GlobalConstants.COLON) >= 0)
            {
                String[] paras = command.Split(':');
                String sendedkey = "";
                try
                {
                    sendedkey = Convert.ToChar(Convert.ToInt32(paras[1])).ToString().Trim();
                }
                catch (Exception e)
                {
                    sendedkey = paras[1].ToString().Trim();
                }
                SendKeys.SendWait(sendedkey);
                System.Threading.Thread.Sleep(100);
                SendKeys.Flush();
            }
        }
        #endregion

        #region Function for commands customer defined
        private static void customerDefine(String command)
        {
            if (command.ToLower().IndexOf(GlobalConstants.CLOSECURRENT.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.ALTF4);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.F5.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.F5VALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.PRE.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.PREVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.NEXT.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.NEXTVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.DOWN.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.DOWNVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.UP.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.UPVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.ESC.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.ESCVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.ENTER.ToLower()) >= 0)
            {
                sendkey(GlobalConstants.ENTERVALUE);
            }
            else if (command.ToLower().IndexOf(GlobalConstants.LOCKSCREEN.ToLower()) >= 0)
            {
                LockWorkStation();
            }
            else if (command.ToLower().IndexOf(GlobalConstants.DOUBLECLICK.ToLower()) >= 0)
            {
                Point point = getcorsorPos();
                mouse_event(MouseEventFlag.LeftDown, point.X, point.Y, 0, UIntPtr.Zero);
                mouse_event(MouseEventFlag.LeftUp, point.X, point.Y, 0, UIntPtr.Zero);
                mouse_event(MouseEventFlag.LeftDown, point.X, point.Y, 0, UIntPtr.Zero);
                mouse_event(MouseEventFlag.LeftUp, point.X, point.Y, 0, UIntPtr.Zero);
            }
        }
        #endregion

        #region private method for sendkeys.sendWait
        private static void sendkey(String key)
        {
            SendKeys.SendWait(key);
            SendKeys.Flush();
        }
        #endregion

        #region Function for getting current position of corsor
        private static Point getcorsorPos()
        {
            Point currentPoint = new Point();
            GetCursorPos(out currentPoint);
            return currentPoint;
        }
        #endregion

        #region Function for win32 api move mouse
        private static void moveMouse(int x, int y)
        {
            SetCursorPos(x, y);
        }
        #endregion

        #region Function for resizing bitmap with specified bitmap and size
        private static Bitmap resize(Bitmap bmp, int width, int height)
        {
            Bitmap newbitma = null;
            int finalWidth = 0, finalHeight = 0;
            if (bmp != null)
            {
                if (bmp.Width > width)
                {
                    finalWidth = Convert.ToInt32(bmp.Width * (Convert.ToDouble(height) / Convert.ToDouble(bmp.Height)));
                }
                if (bmp.Height > height)
                {
                    finalHeight = Convert.ToInt32(bmp.Height * (Convert.ToDouble(width) / Convert.ToDouble(bmp.Width)));
                }
                finalWidth = (finalWidth == 0) ? bmp.Width : finalWidth;
                finalHeight = (finalHeight == 0) ? bmp.Height : finalHeight;
                newbitma = new System.Drawing.Bitmap(bmp, finalWidth, finalHeight);
            }
            return newbitma;
        }
        #endregion

        #region Function for make bitmap gray
        private static Bitmap makeGray(Bitmap bmp)
        {
            Bitmap newbmp = new Bitmap(bmp);
            Color c = new Color();
            Color NewC;
            Byte r, g, b, gray;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    c = bmp.GetPixel(i, j);
                    r = c.R;
                    g = c.G;
                    b = c.B;
                    gray = (Byte)((r * 19595 + g * 38469 + b * 7472) >> 16);
                    NewC = Color.FromArgb(gray, gray, gray);
                    newbmp.SetPixel(i, j, NewC);
                }
            }
            return newbmp;
        }
        #endregion
        #endregion
    }
}
