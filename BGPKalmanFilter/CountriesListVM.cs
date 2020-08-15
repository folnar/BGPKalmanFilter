using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace BGPKalmanFilter
{
    public class CountriesListVM
    {
        public ObservableCollection<CountryVM> CountryItems { get; set; }

        public ICollectionView View { get; set; }

        private CountriesListVM() { }

        public static CountriesListVM CreateObject()
        {
            CountriesListVM clvm = new CountriesListVM
            {
                CountryItems = new ObservableCollection<CountryVM>()
            };
            clvm.View = CollectionViewSource.GetDefaultView(clvm.CountryItems);
            clvm.View.Filter = o =>
            {
                CountryVM c = o as CountryVM;
                return c.CountryDataIsSufficient;
            };
            return clvm;
        }
    }
}