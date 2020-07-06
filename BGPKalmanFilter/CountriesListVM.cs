using System.Collections.ObjectModel;

namespace BGPKalmanFilter
{
    public class CountriesListVM
    {
        public ObservableCollection<PWTCountry> CountryItems { get; set; }

        private CountriesListVM() { }

        public static CountriesListVM CreateObject()
        {
            return new CountriesListVM
            {
                CountryItems = new ObservableCollection<PWTCountry>()
            };
        }
    }
}