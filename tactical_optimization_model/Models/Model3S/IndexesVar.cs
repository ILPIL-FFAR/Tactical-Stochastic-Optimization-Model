using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6
{
    class IndexesVar
    {

        public int getIx7(int a, int b, int c, int d, int e, int f, int g, int nA, int nB, int nC, int nD, int nE, int nF, int nG)
        {
            int result = a * nB * nC * nD * nE * nF * nG + b * nC * nD * nE * nF * nG + c * nD * nE * nF * nG + d * nE * nF * nG + e * nF * nG + f + nG * g;
            return result;
        }

        public int getIx6(int a, int b, int c, int d, int e, int f, int nA, int nB, int nC, int nD, int nE, int nF)
        {
            int result = a * nB * nC * nD * nE * nF + b * nC * nD * nE * nF + c * nD * nE * nF + d * nE * nF + e * nF + f;
            return result;
        }

        public int getIx5(int a, int b, int c, int d, int e, int nA, int nB, int nC, int nD, int nE)
        {
            int result = a * nB * nC * nD * nE + b * nC * nD * nE + c * nD * nE + d * nE + e;
            return result;
        }

        public int getIx4(int a, int b, int c, int d, int nA, int nB, int nC, int nD)
        {
            int result = a * nB * nC * nD + b * nC * nD + c * nD + d;
            return result;
        }

        public int getIx3(int a, int b, int c, int nA, int nB, int nC)
        {
            int result = a * nB * nC + b * nC + c;
            return result;
        }

        public int getIx2(int a, int b, int nA, int nB)
        {
            int result = a * nB + b;
            return result;
        }
    }

}
