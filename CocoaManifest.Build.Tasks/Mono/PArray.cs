using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CocoaManifest.Build.Mono
{
    class PArray : PObjectContainer, IEnumerable<PObject>
    {
        List<PObject> list;

        public override int Count
        {
            get { return list.Count; }
        }

        public PObject this[int i]
        {
            get
            {
                return list[i];
            }
            set
            {
                if (i < 0 || i >= Count)
                    throw new ArgumentOutOfRangeException();
                var existing = list[i];
                list[i] = value;

                OnChildReplaced(null, existing, value);
            }
        }

        public PArray()
        {
            list = new List<PObject>();
        }

        public override PObject Clone()
        {
            var array = new PArray();
            foreach (var item in this)
                array.Add(item.Clone());
            return array;
        }

        protected override bool Reload(PropertyListFormat.ReadWriteContext ctx)
        {
            SuppressChangeEvents = true;
            var result = ctx.ReadArray(this);
            SuppressChangeEvents = false;
            if (result)
                OnChanged(EventArgs.Empty);
            return result;
        }

        public void Add(PObject obj)
        {
            list.Add(obj);
            OnChildAdded(null, obj);
        }

        public void Insert(int index, PObject obj)
        {
            list.Insert(index, obj);
            OnChildAdded(null, obj);
        }

        public void Replace(PObject oldObj, PObject newObject)
        {
            for (int i = 0; i < Count; i++)
            {
                if (list[i] == oldObj)
                {
                    list[i] = newObject;
                    OnChildReplaced(null, oldObj, newObject);
                    break;
                }
            }
        }

        public void Remove(PObject obj)
        {
            if (list.Remove(obj))
                OnChildRemoved(null, obj);
        }

        public void RemoveAt(int index)
        {
            var obj = list[index];
            list.RemoveAt(index);
            OnChildRemoved(null, obj);
        }

        public void Sort(IComparer<PObject> comparer)
        {
            list.Sort(comparer);
        }

        public void Clear()
        {
            list.Clear();
            OnCleared();
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return NSArray.FromNSObjects (list.Select (x => x.Convert ()).ToArray ());
		}
#endif

        public override string ToString()
        {
            return string.Format("[PArray: Items={0}]", Count);
        }

        public void AssignStringList(string strList)
        {
            SuppressChangeEvents = true;
            try
            {
                Clear();
                foreach (var item in strList.Split(',', ' '))
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    Add(new PString(item));
                }
            }
            finally
            {
                SuppressChangeEvents = false;
                OnChanged(EventArgs.Empty);
            }
        }

        public string[] ToStringArray()
        {
            var strlist = new List<string>();

            foreach (PString str in list.OfType<PString>())
                strlist.Add(str.Value);

            return strlist.ToArray();
        }

        public string ToStringList()
        {
            var sb = new StringBuilder();
            foreach (PString str in list.OfType<PString>())
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(str);
            }
            return sb.ToString();
        }

        public IEnumerator<PObject> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public override PObjectType Type
        {
            get { return PObjectType.Array; }
        }
    }

}
