using DACDataVisualization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System;
using DACMath;
using System.Linq;
using System.Collections.Generic;

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

            PlotQtyDict = new Dictionary<string, string>()
            {
                { "N", "Population" },
                { "y", "Production (per capita)" },
                { "A", "TFP" },
                { "K", "Kapital Supply" },
                { "L", "Labor Supply" }
            };
            PlotType = "N";

            dpp = DataPointPreferences.CreateObject(Colors.Black, 1, 4, 4);
            xlp = LabelPreferences.NewLabelPreferences(Brushes.Black, Brushes.Gray, new FontFamily("Century Gothic"), 18, FontStyles.Italic, FontWeights.SemiBold);
            ylp = LabelPreferences.NewLabelPreferences(Brushes.Tomato, Brushes.Transparent, new FontFamily("Century Gothic"), 18, FontStyles.Normal, FontWeights.SemiBold, LabelOrientations.VerticalBottomToTop);
            ap = AxesPreferences2D.CreateObject(Colors.Black, Colors.Black, 1, 1, 40, 1);
            cp = CurvePreferences.NewCurvePreferences(Brushes.DarkOliveGreen, 1, new DoubleCollection() { 3, 2 });

            dt = new DataTable();
        }

        private void LoadObservationsButton_Click(object sender, RoutedEventArgs e)
        {
            //ResultsPlot.ClearPlotArea();
            //ap.XLabel = XAxisLabel.NewAxisLabel("Year", 0.5, 15, xlp);
            //ap.YLabel = YAxisLabel.NewAxisLabel("Kapital Stock", 0.5, 15, ylp);
            //ResultsPlot.SetAxes(-10, 10, -10, 10, ap);
            //ResultsPlot.SetPlotGridLines(20, 20);
            //return;

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
        private readonly LabelPreferences xlp;
        private readonly LabelPreferences ylp;
        private readonly AxesPreferences2D ap;
        private readonly CurvePreferences cp;
        private PWTCountry country = null;
        public Dictionary<string, string> PlotQtyDict { get; set; }
        public string PlotType { get; set; }
        private readonly Dictionary<string, bool> HasBeenPlotted = new Dictionary<string, bool>();

        private void KalmanFilterButton_Click(object sender, RoutedEventArgs e)
        {
            PWTCountry country = (PWTCountry)CountriesListView.SelectedItem;

            //double delta = 0.1;
            double s = 0.33;
            double n = 0.03; // (emp(k) - emp(k - 1)) / emp(k - 1)
            double g = 0.2;  // (rtfpna(k) - rtfpna(k - 1)) / rtfpna(k - 1)

            // time steps
            double dt = 0.1;
            int numsteps = 100;
            MathMatrix t = Sequences.SteppedSequence(0, numsteps * dt, dt);

            // state matrix
            MathMatrix xt = MathMatrix.CreateMatrix(3, numsteps, 0);
            xt[0, 0] = g;
            xt[1, 0] = s;
            xt[2, 0] = n;

            // process matrix moves state matrix from state k to k + 1
            MathMatrix F = MatrixOperations.Identity(3);
            MathMatrix FT = F;

            // control matrix
            // MathMatrix u = MathMatrix.CreateMatrix(1, 1, 0);

            MathMatrix G = MathMatrix.CreateMatrix(4, 1, new double[] { 0, 0, 0, 0 });
            MathMatrix H = MathMatrix.CreateMatrix(1, 4, new double[] { 1, 1, 1, 1 });
            MathMatrix HT = MatrixOperations.Transpose(H);
            MathMatrix Q = MathMatrix.CreateMatrix(4, 4, new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            MathMatrix I = MatrixOperations.Identity(3);

            MathMatrix x = MathMatrix.CreateMatrix(3, numsteps, 0);
            x[0, 0] = xt[0, 0];
            x[1, 0] = xt[1, 0];
            x[2, 0] = xt[2, 0];

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
            ap.XLabel = XAxisLabel.NewAxisLabel("Year", 0.5, 15, xlp);
            ap.YLabel = YAxisLabel.NewAxisLabel("Kapital Stock", 0.5, 15, ylp);
            ResultsPlot.SetAxes(0, numsteps * dt, x.RowVectorArray(1).Min(), x.RowVectorArray(1).Max(), ap);
            ResultsPlot.SetPlotGridLines(20, 20);

            PointCollection pc = new PointCollection();
            for (int idx = 0; idx < x.ColCount; ++idx)
                pc.Add(new Point(t[0, idx], x[1, idx]));

            ResultsPlot.PlotCurve2D(pc, cp);
            ResultsPlot.PlotPoints2D(pc, dpp);
        }

        private void CountriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KalmanFilterButton.IsEnabled = true;
            country = (PWTCountry)(sender as ListView).SelectedItem;
            PlotCountryValues();
        }

        private void PlotCountryValues()
        {
            if (country == null) return;
            string countryFilter = $"countrycode = '{country.CountryCode}'";

            int minYear = int.MaxValue;
            int maxYear = int.MinValue;
            double minYAxisValue = double.MaxValue;
            double maxYAxisValue = double.MinValue;

            PointCollection pc = new PointCollection();
            foreach (DataRow dr in dt.Select(countryFilter, "year ASC"))
            {
                if (!int.TryParse(dr["year"].ToString(), out int year)) { continue; }
                double pop = 0;
                if ((PlotType == "y" || PlotType == "N") && !double.TryParse(dr["pop"].ToString(), out pop)) { continue; }
                double rgdpo = 0;
                if (PlotType == "y" && !double.TryParse(dr["rgdpo"].ToString(), out rgdpo)) { continue; }
                double emp = 0;
                if (PlotType == "L" && !double.TryParse(dr["emp"].ToString(), out emp)) { continue; }
                double rtfpna = 0;
                if (PlotType == "A" && !double.TryParse(dr["rtfpna"].ToString(), out rtfpna)) { continue; }
                double rnna = 0;
                if (PlotType == "K" && !double.TryParse(dr["rnna"].ToString(), out rnna)) { continue; }

                // WE NEED A PLOTTYPE CLASS TO ENCAPSULATE WHETHER IT IS A PER CAPITA TYPE AND TO CLEAN THIS STUFF UP.
                // CLASS COULD CONTAIN A STRING CORRELATING TO DB FIELD NAME SINCE THEY'RE ALL DOUBLES.
                double YValue;
                switch (PlotType)
                {
                    case "N":
                        YValue = pop;
                        break;
                    case "y":
                        YValue = rgdpo / pop;
                        break;
                    case "L":
                        YValue = emp;
                        break;
                    case "A":
                        YValue = rtfpna;
                        break;
                    case "K":
                        YValue = rnna;
                        break;
                    default:
                        YValue = 0;
                        break;
                }

                minYear = Math.Min(year, minYear);
                maxYear = Math.Max(year, maxYear);
                minYAxisValue = Math.Min(YValue, minYAxisValue);
                maxYAxisValue = Math.Max(YValue, maxYAxisValue);

                pc.Add(new Point(year, YValue));
            }

            if (!HasBeenPlotted.ContainsKey(country.CountryCode))
            {
                countryFilter += $" AND year = {minYear}";
                DataRow startingDR = dt.Select(countryFilter).FirstOrDefault();
                if (startingDR == null)
                {
                    MessageBox.Show($"No data for: {country.CountryName}");
                    return;
                }
                if (double.TryParse(startingDR["rgdpo"].ToString(), out double startingRgdpo))
                    if (double.TryParse(startingDR["pop"].ToString(), out double startingPop))
                    {
                        country.StartingRGDPopc = startingRgdpo / startingPop;
                        country.StartingPopulation = startingPop;
                    }
                //    else
                //        MessageBox.Show("trouble parsing starting pop");
                //else
                //    MessageBox.Show("trouble parsing starting rgdpo");

                if (double.TryParse(startingDR["emp"].ToString(), out double startingLabor))
                    country.StartingLaborSupply = startingLabor;
                //else
                //    MessageBox.Show("trouble parsing starting labor supply");

                if (double.TryParse(startingDR["rtfpna"].ToString(), out double startingTFP))
                    country.StartingTFP = startingTFP;
                //else
                //    MessageBox.Show("trouble parsing starting TFP");

                if (double.TryParse(startingDR["rnna"].ToString(), out double startingCapital))
                    country.StartingCapitalStock = startingCapital;
                //else
                //    MessageBox.Show("trouble parsing starting capital stock");

                HasBeenPlotted.Add(country.CountryCode, true);
            }

            ResultsPlot.ClearPlotArea();
            ap.XLabel = XAxisLabel.NewAxisLabel("Year", 0.5, 15, xlp);
            ap.YLabel = YAxisLabel.NewAxisLabel(PlotQtyDict[PlotType], 0.5, 15, ylp);
            ResultsPlot.SetAxes(minYear, maxYear, minYAxisValue, maxYAxisValue, ap);
            ResultsPlot.SetPlotGridLines(20, 20);
            ResultsPlot.PlotCurve2D(pc, cp);
            ResultsPlot.PlotPoints2D(pc, dpp);
        }

        private void PlotTypeMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlotCountryValues();
        }
    }
}
