using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotControlWin
{
    class Chasing
    {
        protected int N;
        protected double[] d;
        protected double[] Aa;
        protected double[] Ab;
        protected double[] Ac;
        protected double[] L;
        protected double[] U;
        public double[] S;

        public bool Init(double[] a, double[] b, double[] c, double[] d)
        {
            int na = a.Length;
            int nb = b.Length;
            int nc = c.Length;
            int nd = d.Length;

            if (nb < 3) 
                return false;
            N = nb;

            if (na != N - 1 || nc != N - 1 || nd != N)
                return false;
            S = new double[N];
            L = new double[N - 1];
            U = new double[N];

            Aa = new double[N - 1];
            Ab = new double[N];
            Ac = new double[N - 1];
            this.d = new double[N];

            for (int i = 0; i <= N - 2; i++)
            {
                Ab[i] = b[i];
                this.d[i] = d[i];

                Aa[i] = a[i];
                Ac[i] = c[i];
            }

            Ab[N - 1] = b[N - 1];
            this.d[N - 1] = d[N - 1];
            return true;
        }
        public bool Solve(out double[] R)
        {
            R = new double[Ab.Length];
            U[0] = Ab[0];

            for (int i = 2; i <= N; i++)
            {
                L[i - 2] = Aa[i - 2] / U[i - 2];
                U[i - 1] = Ab[i - 1] - Ac[i - 2] * L[i - 2];
            }

            double[] Y = new double[d.Length];
            Y[0] = d[0];

            for (int i = 2; i <= N; i++)
            {
                Y[i - 1] = d[i - 1] - (L[i - 2]) * (Y[i - 2]);
            }

            R[N - 1] = Y[N - 1] / U[N - 1];

            for (int i = N - 1; i >= 1; i--)
            {
                R[i - 1] = (Y[i - 1] - Ac[i - 1] * R[i]) / U[i - 1];
            }

            for (int i = 0; i < R.Length; i++)
            {
                if (double.IsInfinity(R[i]) || double.IsNaN(R[i]))
                    return false;
            }

            return true;
        }
    }
}
