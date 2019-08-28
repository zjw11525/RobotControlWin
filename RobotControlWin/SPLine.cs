using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotControlWin
{
    class SPLine
    {
        double[] Xi;
        double[] Yi;
        double[] A;
        double[] B;
        double[] C;
        double[] H;
        double[] Lamda;
        double[] Mu;
        double[] G;
        double[] M;
        int N;
        int n;

        public SPLine()
        {
            N = 0;
            n = 0;
        }

        public bool Init(double[] Xi, double[] Yi)
        {
            if (Xi.Length != Yi.Length)
                return false;
            if (Xi.Length == 0)
                return false;

            this.N = Xi.Length;
            n = N - 1;

            A = new double[N - 1];
            B = new double[N];
            C = new double[N - 1];

            this.Xi = new double[N];
            this.Yi = new double[N];

            H = new double[N - 1];
            Lamda = new double[N - 1];
            Mu = new double[N - 1];

            G = new double[N];
            M = new double[N];

            for (int i = 0; i <= n; i++)
            {
                this.Xi[i] = Xi[i];
                this.Yi[i] = Yi[i];
            }

            GetH();
            GetLamda_Mu_G();
            GetABC();
            Chasing chase = new Chasing();
            chase.Init(A, B, C, G);
            chase.Solve(out M);
            return true;
        }

        private void GetH()
        {
            for (int i = 0; i <= n - 1; i++)
            {
                H[i] = Xi[i + 1] - Xi[i];
            }
        }
        private void GetLamda_Mu_G()
        {
            double t1, t2;
            for (int i = 1; i <= n - 1; i++)
            {
                Lamda[i] = H[i] / (H[i] + H[i - 1]);
                Mu[i] = 1 - Lamda[i];

                t1 = (Yi[i] - Yi[i - 1]) / H[i - 1];
                t2 = (Yi[i + 1] - Yi[i]) / H[i];
                G[i] = 3 * (Lamda[i] * t1 + Mu[i] * t2);
            }
            G[0] = 3 * (Yi[1] - Yi[0]) / H[0];
            G[n] = 3 * (Yi[n] - Yi[n - 1]) / H[n - 1];
            Mu[0] = 1;
            Lamda[0] = 0;
        }
        private void GetABC()
        {
            for (int i = 1; i <= n - 1; i++)
            {
                A[i - 1] = Lamda[i];
                C[i] = Mu[i];
            }
            A[n - 1] = 1;
            C[0] = 1;

            for (int i = 0; i <= n; i++)
            {
                B[i] = 2;
            }
        }
        private double fai0(double x)
        {
            double t1, t2;
            double s;
            t1 = 2 * x + 1;
            t2 = (x - 1) * (x - 1);
            s = t1 * t2;

            return s;
        }
        private double fai1(double x)
        {
            double s;
            s = x * (x - 1) * (x - 1);
            return s;
        }
        public double Interpolate(double x)
        {
            double s = 0;
            double P1, P2;
            double t = x;
            int iNum;

            iNum = GetSection(x);
            if (iNum == -1)
            {
                iNum = 0;
                t = Xi[iNum];
                return Yi[0];
            }
            if (iNum == -999)
            {
                iNum = n - 1;
                t = Xi[iNum + 1];
                return Yi[n];
            }
            P1 = (t - Xi[iNum]) / H[iNum];
            P2 = (Xi[iNum + 1] - t) / H[iNum];

            s = Yi[iNum] * fai0(P1) + Yi[iNum + 1] * fai0(P2) +
                M[iNum] * H[iNum] * fai1(P1) - M[iNum + 1] * H[iNum] * fai1(P2);
            return s;
        }
        private int GetSection(double x)
        {
            int iNum = -1;
            if (x < Xi[0])
            {
                return -1;
            }
            if (x > Xi[N - 1])
            {
                return -999;
            }

            for (int i = 0; i <= n - 1; i++)
            {
                if (x >= Xi[i] && x <= Xi[i + 1])
                {
                    iNum = i;
                    break;
                }
            }
            return iNum;
        }
    }
}
