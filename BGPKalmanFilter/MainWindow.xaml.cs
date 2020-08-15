using DACDataVisualization;
using DACMath;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BGPKalmanFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataPointPreferences dpp;
        private readonly DataPointPreferences dpp2;
        private readonly LabelPreferences xlp;
        private readonly LabelPreferences ylp;
        private readonly AxesPreferences2D ap;
        private readonly CurvePreferences cp;
        private readonly CurvePreferences cp2;
        private PWTCountry country = null;
        public Dictionary<string, string> PlotQtyDict { get; set; }
        public string PlotType { get; set; }
        public Dictionary<string, string> KalmanFilterPlotQtyDict { get; set; }
        public string KalmanFilterPlotType { get; set; }

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

            KalmanFilterPlotQtyDict = new Dictionary<string, string>()
            {
                { "s", "Savings Rate" },
                { "g", "TFP Growth" },
                { "n", "Labor Supply Growth" }
            };
            KalmanFilterPlotType = "s";

            dpp = DataPointPreferences.CreateObject(Colors.Black, 1, 4, 4);
            dpp2 = DataPointPreferences.CreateObject(Colors.RosyBrown, 1, 4, 4);
            xlp = LabelPreferences.NewLabelPreferences(Brushes.Black, Brushes.Gray, new FontFamily("Century Gothic"), 18, FontStyles.Italic, FontWeights.SemiBold);
            ylp = LabelPreferences.NewLabelPreferences(Brushes.Tomato, Brushes.Transparent, new FontFamily("Century Gothic"), 18, FontStyles.Normal, FontWeights.SemiBold, LabelOrientations.VerticalBottomToTop);
            ap = AxesPreferences2D.CreateObject(Colors.Black, Colors.Black, 1, 1, 40, 1);
            cp = CurvePreferences.NewCurvePreferences(Brushes.DarkOliveGreen, 1, new DoubleCollection() { 3, 2 });
            cp2 = CurvePreferences.NewCurvePreferences(Brushes.DarkRed, 1, new DoubleCollection() { 3, 2 });

            dt = new DataTable();
        }

        private void LoadObservationsButton_Click(object sender, RoutedEventArgs e)
        {
            dt.Clear();

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
                CountriesViewModel.CountryItems.Add(CountryVM.NewCountryVM(PWTCountry.NewCountry(dt.Select($"countrycode = '{dr["countrycode"]}'"))));
        }

        private void KalmanFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (CountriesListView.SelectedItem == null) return;
            PWTCountry country = (CountriesListView.SelectedItem as CountryVM).CountryObject;
            SortedDictionary<int, double> kalmanDataSet = country.SavingsRateHT;
            int minYear = kalmanDataSet.Keys.Min();
            int maxYear = kalmanDataSet.Keys.Max();

            if (!double.TryParse(TimeStepTerm.Text, out double dt) || dt < 0)
            {
                MessageBox.Show("invalid time step");
                return;
            }
            int numXVals = (int)((maxYear - minYear) / dt) + 1;

            // time steps
            MathMatrix t = Sequences.SteppedSequence(minYear, maxYear, dt);

            // state matrix
            MathMatrix xt = MathMatrix.CreateMatrix(3, numXVals, 0);
            double[] timeArray = t.RowVectorArray(0);
            for (int colidx = 0; colidx < t.ColCount; ++colidx)
            {
                if (Math.Floor(timeArray[colidx]) == Math.Ceiling(timeArray[colidx]) && country.AGrowthRateHT.ContainsKey((int)timeArray[colidx]))
                {
                    int key = (int)timeArray[colidx];
                    xt[0, colidx] = country.AGrowthRateHT[key];
                    xt[1, colidx] = country.SavingsRateHT[key];
                    xt[2, colidx] = country.LGrowthRateHT[key];
                }
                else if (colidx > 0)
                {
                    xt[0, colidx] = xt[0, colidx - 1];
                    xt[1, colidx] = xt[1, colidx - 1];
                    xt[2, colidx] = xt[2, colidx - 1];
                }
            }

            // ONLY NEEDED FOR PROCESSES WHERE ONLY STARTING DATA IS USED.
            //for (int k = 1; k < numsteps; ++k)
            //    xt.AssignColumn(F * xt.ColumnVector(k - 1) + G * u, k);

            // process matrix moves state matrix from state k to k + 1
            MathMatrix F = MatrixOperations.Identity(3);
            MathMatrix FT = F;

            // control matrix
            MathMatrix u = MathMatrix.CreateMatrix(1, 1, 0);
            MathMatrix G = MathMatrix.CreateMatrix(3, 1, new double[] { 0, 0, 0 });

            MathMatrix H = MathMatrix.CreateMatrix(1, 3, new double[] { 1, 1, 1 });
            MathMatrix HT = MatrixOperations.Transpose(H);
            MathMatrix Q = MathMatrix.CreateMatrix(3, 3, new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            MathMatrix I = MatrixOperations.Identity(3);

            MathMatrix x = MathMatrix.CreateMatrix(3, numXVals, 0);
            x[0, 0] = country.AGrowthRateHT[minYear];
            x[1, 0] = country.SavingsRateHT[minYear];
            x[2, 0] = country.LGrowthRateHT[minYear];

            MathMatrix R = MathMatrix.CreateMatrix(1, 1, 3);
            MathMatrix sqrtR = MatrixOperations.Sqrt_Elmtwise(R);
            MathMatrix v = sqrtR * Distributions.Normal(numXVals);
            MathMatrix z = H * xt + v;

            if (!double.TryParse(CovarianceTerm.Text, out double covterm))
            {
                MessageBox.Show("Covariance term needs to be a double value.");
                return;
            }
            MathMatrix P = MathMatrix.CreateMatrix(3, 3, covterm);

            //for (int k = 1; k < numXVals; ++k)
            for (int k = 0; k < numXVals; ++k)
            {
                //x.AssignColumn(F * xt.ColumnVector(k - 1) + G * u, k);
                x.AssignColumn(F * xt.ColumnVector(k) + G * u, k);
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
            ap.YLabel = YAxisLabel.NewAxisLabel(KalmanFilterPlotQtyDict[KalmanFilterPlotType], 0.5, 15, ylp);
            int kalmanrowidx = KalmanFilterPlotType == "n" ? 2 : KalmanFilterPlotType == "s" ? 1 : KalmanFilterPlotType == "g" ? 0 : -1;
            if (kalmanrowidx == -1)
            {
                MessageBox.Show("A filter quantity must be selected.");
                return;
            }

            // GO THROUGH THE ARRAYS AND DROP CORRESPONDING NAN OR INFINITY ENTRIES FROM X AND XT.
            double[] numsOnlyVec = x.RowVectorArray(kalmanrowidx).Where(p => !double.IsNaN(p) && !double.IsInfinity(p)).ToArray();

            double minY = new double[] { numsOnlyVec.Min(), xt.RowVectorArray(kalmanrowidx).Min() }.Min();
            double maxY = new double[] { numsOnlyVec.Max(), xt.RowVectorArray(kalmanrowidx).Max() }.Max();

            ResultsPlot.SetAxes(minYear, maxYear, minY, maxY, ap);
            ResultsPlot.SetPlotGridLines(20, 20);

            PointCollection pc = new PointCollection();
            PointCollection pc2 = new PointCollection();
            for (int idx = 0; idx < numsOnlyVec.Length; ++idx)
            {
                pc.Add(new Point(t[0, idx], numsOnlyVec[idx]));
                pc2.Add(new Point(t[0, idx], xt[kalmanrowidx, idx]));
            }
            //ResultsPlot.PlotCurve2D(pc, cp);
            ResultsPlot.PlotPoints2D(pc, dpp); // THIS SHOULD ACTUALLY ONLY PLOT TRUE DATA POINTS AND NOT INTERPOLATED ONES.
            ResultsPlot.PlotCurve2D(pc2, cp2);
            //ResultsPlot.PlotPoints2D(pc2, dpp2);
        }

        private void CountriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KalmanFilterButton.IsEnabled = true;
            country = ((CountryVM)(sender as ListView).SelectedItem).CountryObject;
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
