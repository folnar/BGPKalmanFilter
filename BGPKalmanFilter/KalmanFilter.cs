﻿using DACMath;

namespace BGPKalmanFilter
{
    class KalmanFilter
    {
        private KalmanFilter() { }

        public static KalmanFilter NewFilter()
        {
            KalmanFilter kf = new KalmanFilter();

            return kf;
        }

        public void RunFilter()
        {
            //double dt = 0.1;
            //int N = 100;
            //MathMatrix t = Sequences.SteppedSequence(0, N * dt, dt);
            //MathMatrix F = MathMatrix.CreateMatrix(2, 2, new double[] { 1, dt, 0, 1 });
            //MathMatrix FT = MatrixOperations.Transpose(F);
            //MathMatrix G = MathMatrix.CreateMatrix(2, 1, new double[] { 0.5 * dt * dt, dt });
            //MathMatrix H = MathMatrix.CreateMatrix(1, 2, new double[] { 1, 0 });
            //MathMatrix HT = MatrixOperations.Transpose(H);
            //MathMatrix Q = MathMatrix.CreateMatrix(2, 2, new double[] { 0, 0, 0, 0 });
            //MathMatrix u = MathMatrix.CreateMatrix(1, 1, -9.80665);
            //MathMatrix I = MatrixOperations.Identity(2);
            //double y0 = 100;
            //double v0 = 0;
            //MathMatrix xt = MathMatrix.CreateMatrix(2, N, 0);
            //xt[0, 0] = y0;
            //xt[1, 0] = v0;
            //MathMatrix x = MathMatrix.CreateMatrix(2, N, 0);
            //x[0, 0] = 105;
            //if (double.TryParse(Y0.Text, out double qt))
            //{
            //    xt[0, 0] = qt;
            //    x[0, 0] = qt;
            //}
            //qt = 0;
            //x[1, 0] = qt;
            //if (double.TryParse(V0.Text, out qt))
            //{
            //    xt[1, 0] = qt;
            //    x[1, 0] = qt;
            //}

            //for (int k = 1; k < N; ++k)
            //    xt.AssignColumn(F * xt.ColumnVector(k - 1) + G * u, k);

            //MathMatrix R = MathMatrix.CreateMatrix(1, 1, 4);
            //MathMatrix sqrtR = MatrixOperations.Sqrt_Elmtwise(R);
            //MathMatrix v = sqrtR * Distributions.Normal(N);
            //MathMatrix z = H * xt + v;

            //MathMatrix P = MathMatrix.CreateMatrix(2, 2, new double[] { 10, 0, 0, 0.1 });

            //for (int k = 1; k < N; ++k)
            //{
            //    x.AssignColumn(F * x.ColumnVector(k - 1) + G * u, k);
            //    P = F * P * FT + Q;

            //    // HACK HERE SINCE WE DO NOT YET HAVE MATRIX INVERSION.
            //    MathMatrix Knumerator = P * HT;
            //    MathMatrix Kdenominator = (H * P * HT + R);
            //    Kdenominator[0, 0] = 1 / Kdenominator[0, 0];
            //    MathMatrix K = Knumerator * Kdenominator;

            //    x.AssignColumn(x.ColumnVector(k) + K * (z.ColumnVector(k) - H * x.ColumnVector(k)), k);
            //    P = (I - K * H) * P;
            //}
        }
    }
}