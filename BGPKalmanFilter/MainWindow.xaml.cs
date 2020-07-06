using DACDataVisualization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System;
using DACMath;
using System.Linq;

namespace BGPKalmanFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CountriesListVM CountriesViewModel { get; set; }

        private readonly DataTable dt;

        public MainWindow()
        {
            CountriesViewModel = CountriesListVM.CreateObject();
            InitializeComponent();
            DataContext = this;

            dpp = DataPointPreferences.CreateObject(Colors.Black, 1, 4, 4);
            ap = AxesPreferences2D.CreateObject(Colors.Black, Colors.Black, 1, 1, 40, 1);

            dt = new DataTable();
        }

        private void LoadObservationsButton_Click(object sender, RoutedEventArgs e)
        {
            string connstr = @"Server=DAC-VM-SRVR\SQLEXPRESS;Database=research_WPT_9_1;User Id=wptuser;Password=Ue3j!t!;";
            using (SqlConnection dbh = new SqlConnection(connstr))
            {
                string sqlcmdstr = "SELECT * FROM pwt91 ORDER BY country ASC";
                using (SqlCommand sth = new SqlCommand(sqlcmdstr, dbh))
                {
                    dbh.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(sth))
                    {
                        da.Fill(dt);
                    }
                    dbh.Close();
                }
            }

            CountriesViewModel.CountryItems.Clear();
            string[] columns = { "countrycode", "country", "currency_unit" };
            foreach (DataRow dr in dt.DefaultView.ToTable(true, columns).Rows)
                CountriesViewModel.CountryItems.Add(PWTCountry.NewCountry(dr["countrycode"].ToString(), dr["country"].ToString(), dr["currency_unit"].ToString()));
        }

        private readonly DataPointPreferences dpp;
        private readonly AxesPreferences2D ap;

        private void KalmanFilterButton_Click(object sender, RoutedEventArgs e)
        {
            PWTCountry country = (PWTCountry)CountriesListView.SelectedItem;

            double delta = 0.1;
            double s = 0.33;
            double n = 0.03;
            double g = 0.2;

            double dt = 0.1;
            int numsteps = 100;
            MathMatrix t = Sequences.SteppedSequence(0, numsteps * dt, dt);
            MathMatrix F = MathMatrix.CreateMatrix(4, 4, new double[] { 1 + g * dt, 0, 0, 0, 0, 1 + (s - delta) * dt, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 + n * dt });
            MathMatrix FT = MatrixOperations.Transpose(F);
            MathMatrix G = MathMatrix.CreateMatrix(4, 1, new double[] { 0, 0, 0, 0 });
            MathMatrix H = MathMatrix.CreateMatrix(1, 4, new double[] { 1, 1, 1, 1 });
            MathMatrix HT = MatrixOperations.Transpose(H);
            MathMatrix Q = MathMatrix.CreateMatrix(4, 4, new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            MathMatrix u = MathMatrix.CreateMatrix(1, 1, 0);
            MathMatrix I = MatrixOperations.Identity(4);

            MathMatrix xt = MathMatrix.CreateMatrix(4, numsteps, 0);
            xt[0, 0] = country.StartingTFP;
            xt[1, 0] = country.StartingCapitalStock;
            xt[2, 0] = 0;
            xt[3, 0] = country.StartingLaborSupply;
            MathMatrix x = MathMatrix.CreateMatrix(4, numsteps, 0);
            x[0, 0] = xt[0, 0];
            x[1, 0] = xt[1, 0];
            x[2, 0] = xt[2, 0];
            x[3, 0] = xt[3, 0];

            for (int k = 1; k < numsteps; ++k)
                xt.AssignColumn(F * xt.ColumnVector(k - 1) + G * u, k);

            MathMatrix R = MathMatrix.CreateMatrix(1, 1, 4);
            MathMatrix sqrtR = MatrixOperations.Sqrt_Elmtwise(R);
            MathMatrix v = sqrtR * Distributions.Normal(numsteps);
            MathMatrix z = H * xt + v;

            MathMatrix P = MathMatrix.CreateMatrix(4, 4, new double[] { 10, 0, 0, 0.1, 10, 0, 0, 0.1, 10, 0, 0, 0.1, 10, 0, 0, 0.1 });

            for (int k = 1; k < numsteps; ++k)
            {
                x.AssignColumn(F * x.ColumnVector(k - 1) + G * u, k);
                P = F * P * FT + Q;

                // HACK HERE SINCE WE DO NOT YET HAVE MATRIX INVERSION.
                MathMatrix Knumerator = P * HT;
                MathMatrix Kdenominator = (H * P * HT + R);
                Kdenominator[0, 0] = 1 / Kdenominator[0, 0];
                MathMatrix K = Knumerator * Kdenominator;

                x.AssignColumn(x.ColumnVector(k) + K * (z.ColumnVector(k) - H * x.ColumnVector(k)), k);
                P = (I - K * H) * P;
            }

            ResultsPlot.ClearPlotArea();
            ResultsPlot.SetAxes(0, numsteps * dt, x.RowVectorArray(1).Min(), x.RowVectorArray(1).Max(), ap);

            PointCollection pc = new PointCollection();
            for (int idx = 0; idx < x.ColCount; ++idx)
                pc.Add(new Point(t[0, idx], x[1, idx]));

            ResultsPlot.PlotCurve2D(pc);
            ResultsPlot.PlotPoints2D(pc, dpp);
        }

        private void CountriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PWTCountry country = (PWTCountry)(sender as ListView).SelectedItem;
            string countryFilter = $"countrycode = '{country.CountryCode}'";

            int minYear = int.MaxValue;
            int maxYear = int.MinValue;
            double minRGDPopc = double.MaxValue;
            double maxRGDPopc = double.MinValue;

            PointCollection pc = new PointCollection();
            foreach (DataRow dr in dt.Select(countryFilter, "year ASC"))
            {
                if (!int.TryParse(dr["year"].ToString(), out int year)) { continue; }
                if (!double.TryParse(dr["pop"].ToString(), out double pop)) { continue; }
                if (!double.TryParse(dr["rgdpo"].ToString(), out double rgdpo)) { continue; }
                if (!double.TryParse(dr["emp"].ToString(), out double emp)) { continue; }
                if (!double.TryParse(dr["rtfpna"].ToString(), out double rtfpna)) { continue; }
                if (!double.TryParse(dr["rnna"].ToString(), out double rnna)) { continue; }

                double rgdpopc = rgdpo / pop;

                minYear = Math.Min(year, minYear);
                maxYear = Math.Max(year, maxYear);
                minRGDPopc = Math.Min(rgdpopc, minRGDPopc);
                maxRGDPopc = Math.Max(rgdpopc, maxRGDPopc);

                pc.Add(new Point(year, rgdpopc));
            }

            countryFilter += $" AND year = {minYear}";
            DataRow startingDR = dt.Select(countryFilter).FirstOrDefault();
            if (double.TryParse(startingDR["rgdpo"].ToString(), out double startingRgdpo))
                if (double.TryParse(startingDR["pop"].ToString(), out double startingPop))
                {
                    country.StartingRGDPopc = startingRgdpo / startingPop;
                    country.StartingPopulation = startingPop;
                }
                else
                    MessageBox.Show("trouble parsing starting pop");
            else
                MessageBox.Show("trouble parsing starting rgdpo");

            if (double.TryParse(startingDR["emp"].ToString(), out double startingLabor))
                country.StartingLaborSupply = startingLabor;
            else
                MessageBox.Show("trouble parsing starting labor supply");

            if (double.TryParse(startingDR["rtfpna"].ToString(), out double startingTFP))
                country.StartingTFP = startingTFP;
            else
                MessageBox.Show("trouble parsing starting TFP");

            if (double.TryParse(startingDR["rnna"].ToString(), out double startingCapital))
                country.StartingCapitalStock = startingCapital;
            else
                MessageBox.Show("trouble parsing starting capital stock");

            ResultsPlot.ClearPlotArea();
            ResultsPlot.SetAxes(minYear, maxYear, minRGDPopc, maxRGDPopc, ap);
            ResultsPlot.PlotCurve2D(pc);
            ResultsPlot.PlotPoints2D(pc, dpp);
        }
    }
}
