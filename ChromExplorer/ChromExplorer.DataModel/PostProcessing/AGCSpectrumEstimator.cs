using System;

namespace ChromExplorer.DataModel.PostProcessing
{
    public class AGCSpectrumEstimator
    {
        private const int PTS_PER_15_DA = 25 * 15;       // as of now, resolution is about 25 points per DA
        private const int MASS_RANGE = 4000;			// assuming largest possible mass range
        private const int MAXQUANTS = MASS_RANGE / 15;

        private static int[] st_x = new int[MAXQUANTS + 4];
        private static long[] st_indRC = new long[MAXQUANTS + 4];
        private static int[] st_ave = new int[MAXQUANTS + 4];
        private static int[] st_index = new int[MAXQUANTS + 4];

        public long full_integral;

        public long IntegrateUsefulSignalVersion2(
                                    int[] y,				// data array
                                    int reducer			    // 1 - default. May be larger if data values are too high  													
                                    )
        {
            var npoints = y.Length;
            int quant, nquants;

            long sum = 0, sum2 = 0;
            long retval = 0;

            int i, j, ind;
            int nbits = 0; //3;

            quant = PTS_PER_15_DA;
            if ((quant % 2) == 0)
                quant += 1;

            nquants = npoints / quant;

            if (nquants > MAXQUANTS + 4)
            {
                // Warning! only part of spectra will be used in this unexpected case
                nquants = MAXQUANTS + 4;

            }

            if (nquants < 4)
                return retval;

            int random = 0;

            full_integral = 0;
            long val;
            long _base;


            for (i = 0; i < nquants; i++)
            {
                sum2 = 0;
                var fsPart = SumIntArr(y, i * quant, quant);
                st_ave[i] = (int)fsPart;
                full_integral += fsPart;
                _base = st_ave[i] / quant;
                st_ave[i] -= random * quant;

                for (j = 0; j < quant; j++)
                {
                    ind = i * quant + j;
                    val = (long)(y[ind] - _base) / reducer; //64;
                    sum2 = sum2 + val * val;
                }

                st_indRC[i] = sum2;
                st_x[i] = i * quant + quant / 2;
                st_index[i] = i;
            }

            for (i = (nquants) * quant; i < npoints; i++)
            {
                full_integral += y[i];
            }

            insert_int_array_and_index((uint)nquants, st_indRC, st_index);

            int sigma = reducer * IntRoot(st_indRC[nquants / 3] / (quant - 1));
            if (sigma <= 0)
                sigma = 1;

            int X1 = -1, X2, X3 = 0;
            long Y1 = 0, Y2 = 0, Y3 = 0;
            int initialized = 0;

            X2 = npoints / 2;

            for (i = 0; i < nquants * 3 / 4; i++)
            {
                if (X1 == -1)
                {
                    X1 = st_x[st_index[i]];
                    Y1 = st_ave[st_index[i]]; // actually not ave but sum
                    X3 = st_x[st_index[i]];
                    Y3 = st_ave[st_index[i]]; // actually not ave but sum

                }
                else
                {
                    if (st_x[st_index[i]] > X3)
                    {
                        X3 = st_x[st_index[i]];
                        Y3 = st_ave[st_index[i]]; // actually not ave but sum
                    }
                    if (st_x[st_index[i]] < X1)
                    {
                        X1 = st_x[st_index[i]];
                        Y1 = st_ave[st_index[i]]; // actually not ave but sum
                    }
                }

            }

            // to provide integer multiplication by mutually prinme p/q

            int XNUM = 2 * (X2 - X1) / quant + 1;
            int XDENUM = 2 * (X3 - X1) / quant + 1;

            Y2 = Y1 + (Y3 - Y1) * XNUM / XDENUM;//(X2-X1)/(X3-X1);

            Y1 /= quant;
            Y3 /= quant;
            int conservation = 2 * npoints; //infinity in this context
            int singul = 0;
            if (Y3 - Y1 != 0)
            {
                conservation = Math.Abs(X3 - X1) / (int)Math.Abs(Y3 - Y1);
                singul = X1 % conservation;
            }



            if (conservation < 2)
                conservation = 0;

            long useful = 0L;
            int nsignificant = 0;

            // If conservation is used, error increases slightly because actual singularity point is unknown
            // Set conservation to 0, although it accelerates a lot
            // Disable for now
            conservation = 0;
            Y2 = Y1 + (Y3 - Y1) * (0 - X1) / (X3 - X1);

            if (conservation != 0)
            {
                for (i = 0; i < npoints; i++)
                {
                    // value of bkgd within segment
                    if ((i % conservation) == singul)
                        Y2 = Y1 + (Y3 - Y1) * (i - X1) / (X3 - X1);
                    if (y[i] > Y2 + 3 * sigma)
                    {
                        useful += (y[i] - Y2);
                        nsignificant++;
                    }
                }
            }
            else
            {
                for (i = 0; i < npoints; i++)
                {
                    Y2 = Y1 + (Y3 - Y1) * (i - X1) / (X3 - X1);
                    if (y[i] > Y2 + 3 * sigma)
                    {
                        useful += (y[i] - Y2);
                        nsignificant++;
                    }
                }
            }



            retval = useful;
            if (retval < 0)
                retval = 0;


            return retval;
        }


        static long SumIntArr(int[] y, int offset, int npoints)
        {
            long sum = 0;
            for (int i = offset; i < offset + npoints; i++)
                sum = sum + y[i];
            return sum;
        }

        void insert_int_array_and_index(uint n, long[] a, int[] index)
        {
            long i, j, inc;
            long v;
            int vv;
            inc = 1;
            //a--;
            //index--;
            do
            {
                inc *= 3;
                inc++;
            } while (inc <= n);
            do
            {
                inc /= 3;
                for (i = inc + 1; i <= n; i++)
                {
                    if (i == 0)
                    {
                        Console.WriteLine("AAA");
                    }
                    v = a[i - 1];
                    vv = index[i - 1];
                    j = i;
                    while (a[j - inc - 1] > v)
                    {
                        a[j - 1] = a[j - inc - 1];
                        index[j - 1] = index[j - inc - 1];
                        j -= inc;
                        if (j <= inc) break;
                    }
                    a[j - 1] = v;
                    index[j - 1] = vv;
                }
            } while (inc > 1);
        }

        int IntRoot(long N)
        {
            // to work for N<=2^^30
            int i = 0, mid = 0, A = 0, B = 0;

            int seed = 1;

            if (N == 0) return 0;
            if (N <= 2) return 1;
            if (N <= 4) return 2;


            if (N >= (seed << 30))
                return seed << 15;

            // determine A and B
            for (i = 0; i < 14; i++)
            {
                A = seed << i;
                B = seed << (i + 1);
                if (A * A == N)
                    return A;
                if (B * B == N)
                    return B;
                if (A * A < N && B * B > N)
                    break;
            }


            while (B - A > 2)
            {

                mid = (A + B) / 2;

                if (mid * mid == N)
                    return mid;

                if (mid * mid > N)
                {
                    B = mid;
                }
                else
                {
                    A = mid;
                }

            }

            return (A + B) / 2;

        }

        public double IntegrateUsefulSignalVersion2(double[] data, int reducer)
        {
            var ints = new int[data.Length];
            for(int i = 0; i < ints.Length; i++)
            {
                ints[i] = (int) data[i];

            }
            return IntegrateUsefulSignalVersion2(ints, reducer);
        }
    }
}
