using System.Collections.Generic;

namespace BGPKalmanFilter
{
    public class PWTCountry
    {
        // Identifier variables
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string CurrencyUnit { get; set; }
        public double StartingRGDPopc { get; internal set; }
        public double StartingPopulation { get; internal set; }
        public double StartingLaborSupply { get; internal set; }
        public double StartingTFP { get; internal set; }
        public double StartingCapitalStock { get; internal set; }

        internal List<PWTObservation> Observations { get; set; }

        private PWTCountry() { }

        public static PWTCountry NewCountry(string countrycode, string countryname, string currency)
        {
            return new PWTCountry
            {
                CountryCode = countrycode,
                CountryName = countryname,
                CurrencyUnit = currency
            };
        }
    }
}
