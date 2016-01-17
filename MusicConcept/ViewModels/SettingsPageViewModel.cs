using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels
{
    public class SettingsPageViewModel
    {
        public class AlphabetViewModel : NotifyPropertyChangedObject
        {
            public string AlphabetLocalizedName { get; private set; }
            public string Alphabet { get; private set; }

            private Action<AlphabetViewModel> isEnabledChanged;
            private bool _isEnabled;
            public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; NotifyPropertyChanged("IsEnabled"); isEnabledChanged(this); } }

            public AlphabetViewModel(string alphabetLocalizedName, string alphabet, bool isEnabled, Action<AlphabetViewModel> isEnabledChanged)
            {
                this.AlphabetLocalizedName = alphabetLocalizedName;
                this.Alphabet = alphabet;
                this._isEnabled = isEnabled;
                this.isEnabledChanged = isEnabledChanged;
            }
        }

        public IEnumerable<AlphabetViewModel> Alphabets { get; private set; }

        public SettingsPageViewModel()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();

            var currentAlphabet = ApplicationSettings.SortingAlphabet.Read();
           this.Alphabets = MusicConcept.Library.SortingHelper.Alphabets.Select(p =>
               new AlphabetViewModel(resourceLoader.GetString("Alphabet" + p.Key), p.Value, currentAlphabet.Contains(p.Value),
                   UpdateAlphabet)).ToArray();
        }

        private void UpdateAlphabet(AlphabetViewModel alphabet)
        {
            var alphabets = this.Alphabets.Where(p => p.IsEnabled);
            if(!alphabets.Any())
                this.Alphabets.First(p => p != alphabet).IsEnabled = true;
            else
            {
                var newAlphabet = MusicConcept.Library.SortingHelper.SymbolChar + String.Concat(alphabets.Select(p => p.Alphabet));
                ApplicationSettings.SortingAlphabet.Save(newAlphabet);
            }
        }
    }
}
