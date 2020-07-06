namespace BGPKalmanFilter
{
    internal class PWTObservation
    {
        // Identifier variables
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string CurrencyUnit { get; set; }
        public int Year { get; set; }

        // Real GDP, employment and population levels
        public double Rgdpe { get; set; }
        public double Rgdpo { get; set; }
        public double Pop { get; set; }
        public double Emp { get; set; }
        public double Avh { get; set; }
        public double Hc { get; set; }

        // Current price GDP, capital and TFP
        public double Ccon { get; set; }
        public double Cda { get; set; }
        public double Cgdpe { get; set; }
        public double Cgdpo { get; set; }
        public double Cn { get; set; }
        public double Ck { get; set; }
        public double Ctfp { get; set; }
        public double Cwtfp { get; set; }

        // National accounts-based variables
        public double Rgdpna { get; set; }
        public double Rconna { get; set; }
        public double Rdana { get; set; }
        public double Rnna { get; set; }
        public double Rkna { get; set; }
        public double Rtfpna { get; set; }
        public double Rwtfpna { get; set; }
        public double Labsh { get; set; }
        public double Irr { get; set; }
        public double Delta { get; set; }

        // Exchange rates and GDP price levels
        public double Xr { get; set; }
        public double Pl_con { get; set; }
        public double Pl_da { get; set; }
        public double Pl_gdpo { get; set; }

        // Data information variables
        public string I_cig { get; set; }
        public string I_xm { get; set; }
        public string I_xr { get; set; }
        public string I_outlier { get; set; }
        public string I_irr { get; set; }
        public double Cor_exp { get; set; }
        public double Statcap { get; set; }

        // Shares in CGDPo
        public double Csh_c { get; set; }
        public double Csh_i { get; set; }
        public double Csh_g { get; set; }
        public double Csh_x { get; set; }
        public double Csh_m { get; set; }
        public double Csh_r { get; set; }

        // Price levels, expenditure categories and capital
        public double Pl_c { get; set; }
        public double Pl_i { get; set; }
        public double Pl_g { get; set; }
        public double Pl_x { get; set; }
        public double Pl_m { get; set; }
        public double Pl_n { get; set; }
        public double Pl_k { get; set; }

        private PWTObservation() { }

        public static PWTObservation NewObservation(object[] dataIn)
        {
            PWTObservation pwto = new PWTObservation();

            return pwto;
        }
    }
}
