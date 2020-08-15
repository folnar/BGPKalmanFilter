using System.Collections.Generic;
using System.Data;

namespace BGPKalmanFilter
{
    public class PWTCountry
    {
        internal string CountryCode { get; set; }
        internal string CountryName { get; set; }
        internal string CurrencyUnit { get; set; }

        internal DataRow[] Observations { get; set; }
        internal SortedDictionary<int, double> SavingsRateHT { get; set; }
        internal SortedDictionary<int, double> LGrowthRateHT { get; set; }
        internal SortedDictionary<int, double> AGrowthRateHT { get; set; }

        private PWTCountry() { }

        internal static PWTCountry NewCountry(DataRow[] dataRows)
        {
            PWTCountry pwtc = new PWTCountry
            {
                Observations = dataRows,
                CountryCode = dataRows[0]["countrycode"]?.ToString() ?? "",
                CountryName = dataRows[0]["country"]?.ToString() ?? "",
                CurrencyUnit = dataRows[0]["currency_unit"]?.ToString() ?? ""
            };

            pwtc.SavingsRateHT = new SortedDictionary<int, double>();
            pwtc.LGrowthRateHT = new SortedDictionary<int, double>();
            pwtc.AGrowthRateHT = new SortedDictionary<int, double>();
            pwtc.CalculateRates();

            return pwtc;
        }

        private void CalculateRates()
        {
            for (int idx = 1; idx < Observations.Length; ++idx)
            {
                if (int.TryParse(Observations[idx]["year"].ToString(), out int obsYear))
                {
                    if (!double.TryParse(Observations[idx - 1]["rtfpna"].ToString(), out double rtfpna_k0) ||
                        !double.TryParse(Observations[idx]["rtfpna"].ToString(), out double rtfpna_k1) ||
                        !double.TryParse(Observations[idx - 1]["emp"].ToString(), out double emp_k0) ||
                        !double.TryParse(Observations[idx]["emp"].ToString(), out double emp_k1) ||
                        !double.TryParse(Observations[idx]["csh_c"].ToString(), out double csh_c) ||
                        !double.TryParse(Observations[idx]["csh_g"].ToString(), out double csh_g))
                        continue;

                    AGrowthRateHT.Add(obsYear, (rtfpna_k1 - rtfpna_k0) / rtfpna_k1);
                    LGrowthRateHT.Add(obsYear, (emp_k1 - emp_k0) / emp_k1);
                    SavingsRateHT.Add(obsYear, 1 - csh_c - csh_g);
                }
            }
        }
    }
}
