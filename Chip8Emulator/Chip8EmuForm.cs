using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Chip8Emulator
{
    public partial class Chip8EmuForm : Form
    {
        public Chip8EmuForm()
        {
            InitializeComponent();
            Width = 640 + (Width - ClientSize.Width);
            Height = 320 + (Height - ClientSize.Height);
            ClientSize = new Size(640, 320);
        }

        public bool[] KeysPressed = new bool[0x10];

        /*
         * |1|2|3|C|  =>  |1|2|3|4|
         * |4|5|6|D|  =>  |Q|W|E|R|
         * |7|8|9|E|  =>  |A|S|D|F|
         * |A|0|B|F|  =>  |Z|X|C|V|
         */
        public static Keys[] KeyMapping = {
            Keys.X,  Keys.D1, Keys.D2, Keys.D3,
            Keys.Q,  Keys.W,  Keys.E,  Keys.A,
            Keys.S,  Keys.D,  Keys.Z,  Keys.C,
            Keys.D4, Keys.R,  Keys.F,  Keys.V
        };
        
        private Timer EmuTimer;
        private Emulator Emulator;
        private readonly Pixel[,] PixelRectangles = new Pixel[64,32];

        private class Pixel
        {
            public readonly Rectangle Rectangle;
            public bool IsWhite;
            private const int Width = 10;
            private const int Height = 10;

            public Pixel(int x, int y)
            {
                Rectangle = new Rectangle(x * 10, y * 10, Width, Height);
            }
        }

        private void Chip8EmuForm_Load(object sender, EventArgs e)
        {
            Emulator = new Emulator(this);
            for (var i = 0; i < 64; i++)
            {
                for (var j = 0; j < 32; j++)
                {
                    PixelRectangles[i, j] = new Pixel(i, j);
                }
            }
            EmuTimer = new Timer
            {
                Interval = 16,
            };
            EmuTimer.Tick += OnTimer;
            EmuTimer.Start();
        }

        private void OnTimer(object sender, EventArgs e)
        {
            Emulator.Loop();
        }

        private void Chip8EmuForm_KeyDown(object sender, KeyEventArgs e)
        {
            var index = Array.IndexOf(KeyMapping, e.KeyCode);
            if (index != -1)
            {
                KeysPressed[index] = true;
            }
        }

        private void Chip8EmuForm_KeyUp(object sender, KeyEventArgs e)
        {
            var index = Array.IndexOf(KeyMapping, e.KeyCode);
            if (index != -1)
            {
                KeysPressed[index] = false;
            }
        }

        private void Chip8EmuForm_Paint(object sender, PaintEventArgs e)
        {
            UpdateAllPixels();
        }

        private void UpdateAllPixels()
        {
            using (var graphics = CreateGraphics())
            using (var white = new SolidBrush(Color.White))
            using (var black = new SolidBrush(Color.Black))
            {
                foreach (var pixel in PixelRectangles)
                {
                    graphics.FillRectangle(pixel.IsWhite ? white : black, pixel.Rectangle);
                }
            }
        }

        private void UpdatePixel(Pixel pixel)
        {
            using (var graphics = CreateGraphics())
            using (var white = new SolidBrush(Color.White))
            using (var black = new SolidBrush(Color.Black))
            {
                graphics.FillRectangle(pixel.IsWhite ? white : black, pixel.Rectangle);
            }
        }

        public bool DrawSprite(int x, int y, byte[] data)
        {
            var hasOverwritten = false;
            const int width = 8;
            var height = data.Length;
            for (var i = 0; i < height; i++)
            {
                var yPos = (y + i) % 32;
                var line = data[i];
                for (var j = 0; j < width; j++)
                {
                    var xPos = (x + j) % 64;
                    var pixel = PixelRectangles[xPos, yPos];
                    var newValue = ((line >> (7 - j)) & 1) != 0;
                    if (!newValue) continue;
                    if (pixel.IsWhite)
                    {
                        hasOverwritten = true;
                    }
                    pixel.IsWhite = !pixel.IsWhite;
                    UpdatePixel(pixel);
                }
            }

            return hasOverwritten;
        }

        public void ClearAll()
        {
            foreach (var pixel in PixelRectangles)
            {
                pixel.IsWhite = false;
            }
            UpdateAllPixels();
        }
    }

    public class Emulator
    {
        private Chip8EmuForm Display;
        private byte[] Memory = new byte[0x1000];
        private byte[] V = new byte[0x10];
        private char I;
        private char Pc;
        private byte DelayTimer;
        private byte SoundTimer;
        private byte Sp;
        private char[] Stack = new char[0x10];

        public Emulator(Chip8EmuForm display)
        {
            Display = display;
            InitializeFonts();
            LoadGameIntoMemory();
            Pc = (char) 0x200;
            Sp = 0;
        }

        public void InitializeFonts()
        {
            var fonts = new byte[]
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };
            for (var offset = 0; offset < fonts.Length; offset++)
            {
                Memory[offset] = fonts[offset];
            }
        }

        public void LoadGameIntoMemory()
        {
            const string parent = "D:\\Documents\\My Games\\Chip-8\\Chip-8 Games";
            var bytes = File.ReadAllBytes($"{parent}\\Space Invaders [David Winter].ch8");
            for (var offset = 0; offset < bytes.Length; offset++)
            {
                Memory[0x200 + offset] = bytes[offset];
            }
        }

        public void Loop()
        {
            if (DelayTimer > 0)
            {
                DelayTimer--;
            }
            if (SoundTimer > 0)
            {
                SoundTimer--;
            }
            var opcode = (Memory[Pc] << 8) | Memory[Pc + 1];
            Console.WriteLine(opcode.ToString("X"));
            var nnn = opcode & 0x0FFF;
            var n = opcode & 0x000F;
            var x = (opcode & 0x0F00) >> 8;
            var y = (opcode & 0x00F0) >> 4;
            var kk = opcode & 0x00FF;
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (nnn)
                    {
                        case 0x0E0:
                            Display.ClearAll();
                            break;
                        case 0x0EE:
                            Sp--;
                            Pc = Stack[Sp];
                            break;
                        default:
                            Console.WriteLine($"Unsupported Opcode: {opcode}");
                            break;
                    }
                    Pc += (char) 2;
                    break;
                case 0x1000:
                    Pc = (char) nnn;
                    break;
                case 0x2000:
                    Stack[Sp] = Pc;
                    Sp++;
                    Pc = (char) nnn;
                    break;
                case 0x3000:
                    if (V[x] == kk)
                        Pc += (char) 2;
                    Pc += (char) 2;
                    break;
                case 0x4000:
                    if (V[x] != kk)
                        Pc += (char) 2;
                    Pc += (char) 2;
                    break;
                case 0x5000:
                    if (V[x] == V[y])
                        Pc += (char) 2;
                    Pc += (char) 2;
                    break;
                case 0x6000:
                    V[x] = (byte) kk;
                    Pc += (char) 2;
                    break;
                case 0x7000:
                    V[x] += (byte) kk;
                    Pc += (char) 2;
                    break;
                case 0x8000:
                    switch (n)
                    {
                        case 0x0:
                            V[x] = V[y];
                            break;
                        case 0x1:
                            V[x] = (byte) (V[x] | V[y]);
                            break;
                        case 0x2:
                            V[x] = (byte) (V[x] & V[y]);
                            break;
                        case 0x3:
                            V[x] = (byte) (V[x] ^ V[y]);
                            break;
                        case 0x4:
                            var sum = V[x] + V[y];
                            V[0xF] = (byte) (sum >= 0x100 ? 1 : 0);
                            V[x] = (byte) (sum & 0xFF);
                            break;
                        case 0x5:
                            var sub = V[x] - V[y];
                            V[0xF] = (byte) (sub >= 0 ? 1 : 0);
                            V[x] = (byte) (sub & 0xFF);
                            break;
                        case 0x6:
                            V[0xF] = (byte) (V[x] & 1);
                            V[x] = (byte) (V[x] >> 1);
                            break;
                        case 0x7:
                            var subn = V[y] - V[x];
                            V[0xF] = (byte)(subn > 0 ? 1 : 0);
                            V[x] = (byte)(subn & 0xFF);
                            break;
                        case 0xE:
                            V[0xF] = (byte)(V[x] >> 7);
                            V[x] = (byte)(V[x] << 1);
                            break;
                        default:
                            Console.WriteLine($"Unsupported Opcode: {opcode}");
                            break;
                    }
                    Pc += (char) 2;
                    break;
                case 0x9000:
                    if (V[x] != V[y])
                        Pc += (char)2;
                    Pc += (char)2;
                    break;
                case 0xA000:
                    I = (char) nnn;
                    Pc += (char)2;
                    break;
                case 0xB000:
                    Pc = (char) (nnn + V[0]);
                    break;
                case 0xC000:
                    V[x] = (byte) (new Random().Next(0x100) & kk);
                    Pc += (char)2;
                    break;
                case 0xD000:
                    var bytes = new byte[n];
                    for (var offset = 0; offset < n; offset++)
                    {
                        bytes[offset] = Memory[I + offset];
                    }
                    V[0xF] = (byte) (Display.DrawSprite(V[x], V[y], bytes) ? 1 : 0);
                    Pc += (char)2;
                    break;
                case 0xE000:
                    switch (kk)
                    {
                        case 0x9E:
                            if (Display.KeysPressed[V[x]])
                                Pc += (char)2;
                            break;
                        case 0xA1:
                            if (!Display.KeysPressed[V[x]])
                                Pc += (char)2;
                            break;
                        default:
                            Console.WriteLine($"Unsupported Opcode: {opcode}");
                            break;
                    }
                    Pc += (char)2;
                    break;
                case 0xF000:
                    switch (kk)
                    {
                        case 0x07:
                            V[x] = DelayTimer;
                            break;
                        case 0x0A:
                            var index = Array.IndexOf(Display.KeysPressed, true);
                            if (index == -1)
                                Pc -= (char) 2;
                            else
                                V[x] = (byte) index;
                            break;
                        case 0x15:
                            DelayTimer = V[x];
                            break;
                        case 0x18:
                            SoundTimer = V[x];
                            break;
                        case 0x1E:
                            I += (char) V[x];
                            break;
                        case 0x29:
                            I = (char) (V[x] * 5);
                            break;
                        case 0x33:
                            Memory[I + 2] = (byte) (V[x] % 10);
                            Memory[I + 1] = (byte) (V[x]/10 % 10);
                            Memory[I] = (byte) (V[x]/100);
                            break;
                        case 0x55:
                            for (var offset = 0; offset <= x; offset++)
                            {
                                Memory[I + offset] = V[offset];
                            }
                            I += (char) (x + 1);
                            break;
                        case 0x65:
                            for (var offset = 0; offset <= x; offset++)
                            {
                                V[offset] = Memory[I + offset];
                            }
                            I += (char)(x + 1);
                            break;
                        default:
                            Console.WriteLine($"Unsupported Opcode: {opcode}");
                            break;
                    }
                    Pc += (char)2;
                    break;
                default:
                    Console.WriteLine($"Unsupported Opcode: {opcode}");
                    break;
            }
        }
    }
}
