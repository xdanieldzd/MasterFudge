using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.IO
{
    /* http://www.smspower.org/uploads/Development/sc3000h-20040729.txt 
     * https://sites.google.com/site/mavati56/sega_sf7000
     */

    // TODO: make joypad work, too

    public enum KeyboardKeys
    {
        None = 0,

        D1 = (0 * 8), D2, D3, D4, D5, D6, D7, P1Up,
        Q = (8 * 1), W, E, R, T, Y, U, P1Down,
        A = (8 * 2), S, D, F, G, H, J, P1Left,
        Z = (8 * 3), X, C, V, B, N, M, P1Right,
        EngDiers = (8 * 4), Space, HomeClr, InsDel, Unmapped36, Unmapped37, Unmapped38, P1Button1,
        Comma = (8 * 5), Period, Slash, Pi, ArrowDown, ArrowLeft, ArrowRight, P1Button2,
        K = (8 * 6), L, Semicolon, Colon, BracketClose, CR, ArrowUp, P2Up,
        I = (8 * 7), O, P, At, BracketOpen, Unmapped61, Unmapped62, P2Down,
        D8 = (8 * 8), D9, D0, Minus, Caret, Yen, Break, P2Left,
        Unmapped72 = (8 * 9), Unmapped73, Unmapped74, Unmapped75, Unmapped76, Unmapped77, Graph, P2Right,
        Unmapped80 = (8 * 10), Unmapped81, Unmapped82, Unmapped83, Unmapped84, Unmapped85, Ctrl, P2Button1,
        Unmapped88 = (8 * 11), Unmapped89, Unmapped90, Unmapped91, Unmapped92, Func, Shift, P2Button2
    }

    public class SCKeyboard
    {
        i8255PPI ppi;

        bool[,] keyMatrix;

        public SCKeyboard(i8255PPI ppi)
        {
            this.ppi = ppi;

            keyMatrix = new bool[12, 8];
        }

        public void Reset()
        {
            for (int i = 0; i < keyMatrix.GetLength(0); i++)
                for (int j = 0; j < keyMatrix.GetLength(1); j++)
                    keyMatrix[i, j] = false;
        }

        public void SetKeys(KeyboardKeys key, bool pressed)
        {
            keyMatrix[(int)key / 8, (int)key % 8] = pressed;
            Refresh();
        }

        public void Refresh()
        {
            int matrixRow = (ppi.PortCOutput & 0x07);
            if (matrixRow != 0x07)
            {
                byte rowStateA = 0xFF, rowStateB = 0xFF;

                for (int i = 0; i < 8; i++)
                    if (keyMatrix[i, matrixRow]) rowStateA &= (byte)~(1 << i);

                for (int i = 0; i < 4; i++)
                    if (keyMatrix[8 + i, matrixRow]) rowStateB &= (byte)~(1 << i);

                ppi.PortAInput = rowStateA;
                ppi.PortBInput = (byte)((ppi.PortBInput & 0xF0) | (rowStateB & 0x0F));
            }
        }
    }
}
