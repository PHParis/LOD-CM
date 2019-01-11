using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace PatternDiscovery
{
    [JsonObject]
    public class ItemSet<T> : List<T>
        where T : IComparable<T>
    {
        //protected List<long> mTransactionIDList = new List<long>();
        //protected int mDbSize = 0;

        // public bool IsMaximal { get; set; }

        [JsonProperty]
        public List<T> TransactionIDList { get; set; } = new List<T>();

        [JsonProperty]
        public int DbSize { get; set; } = 0;
        [JsonIgnore]
        public double Support
        {
            get
            {
                return DbSize == 0 ? 0 : (double)TransactionCount / DbSize;
            }
        }

        //protected int mTransactionCount = 0;
        [JsonProperty]
        public int TransactionCount { get; set; } = 0;

        public virtual ItemSet<T> Clone()
        {
            ItemSet<T> clone = new ItemSet<T>();
            clone.TransactionCount = TransactionCount;
            for (int i = 0; i < Count; ++i)
            {
                clone.Add(this[i]);
            }
            for (int i = 0; i < TransactionIDList.Count; ++i)
            {
                var id = TransactionIDList[i];
                clone.TransactionIDList.Add(id);
            }
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ItemSet<T>)) return false;
            var rhs = obj as ItemSet<T>;
            if (Count != rhs.Count) return false;

            for (int i = 0; i < rhs.Count; ++i)
            {
                if (!this[i].Equals(rhs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < Count; ++i)
            {
                hash = hash * 31 + this[i].GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            for (int j = 0; j < this.Count; ++j)
            {
                if (j == 0)
                {
                    sb.AppendFormat("{0}", this[j]);
                }
                else
                {
                    sb.AppendFormat(", {0}", this[j]);
                }
            }
            sb.Append(" }");
            return sb.ToString();
        }

    }
}
