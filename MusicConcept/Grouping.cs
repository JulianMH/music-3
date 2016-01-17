using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept
{
    class Grouping<TKey, TValue> : ObservableCollection<TValue>,  IGrouping<TKey, TValue>
    {
        private TKey key;

        public TKey Key
        {
            get { return key; }
        }

        public Grouping(TKey key, IEnumerable<TValue> items) : base(items == null ? new TValue[0] : items)
        {
            this.key = key;
        }
    }
}
