namespace BGPKalmanFilter
{
    public class CountryVM
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string CurrencyUnit { get; set; }
        public bool CountryDataIsSufficient { get; set; }

        public PWTCountry CountryObject { get; set; }

        private CountryVM() { }

        public static CountryVM NewCountryVM(PWTCountry pwtc)
        {
            CountryVM cvm = new CountryVM
            {
                CountryObject = pwtc,
                CountryCode = pwtc.Observations[0]["countrycode"]?.ToString() ?? "",
                CountryName = pwtc.Observations[0]["country"]?.ToString() ?? "",
                CurrencyUnit = pwtc.Observations[0]["currency_unit"]?.ToString() ?? ""
            };
            cvm.CountryDataIsSufficient = true;
            if (pwtc.AGrowthRateHT.Count == 0)
                cvm.CountryDataIsSufficient = false;
            return cvm;
        }
    }
}
