using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CocoaManifest.Build.Mono
{
    class PDictionary : PObjectContainer, IEnumerable<KeyValuePair<string, PObject>>
    {
        static readonly byte[] BeginMarkerBytes = Encoding.ASCII.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        static readonly byte[] EndMarkerBytes = Encoding.ASCII.GetBytes("</plist>");

        readonly Dictionary<string, PObject> dict;
        readonly List<string> order;

        public PObject this[string key]
        {
            get
            {
                PObject value;
                if (dict.TryGetValue(key, out value))
                    return value;
                return null;
            }
            set
            {
                PObject existing;
                bool exists = dict.TryGetValue(key, out existing);
                if (!exists)
                    order.Add(key);

                dict[key] = value;

                if (exists)
                    OnChildReplaced(key, existing, value);
                else
                    OnChildAdded(key, value);
            }
        }

        public void Add(string key, PObject value)
        {
            dict.Add(key, value);
            order.Add(key);

            OnChildAdded(key, value);
        }

        public void InsertAfter(string keyBefore, string key, PObject value)
        {
            dict.Add(key, value);
            order.Insert(order.IndexOf(keyBefore) + 1, key);

            OnChildAdded(key, value);
        }

        public override int Count
        {
            get { return dict.Count; }
        }

        #region IEnumerable[KeyValuePair[System.String,PObject]] implementation
        public IEnumerator<KeyValuePair<string, PObject>> GetEnumerator()
        {
            foreach (var key in order)
                yield return new KeyValuePair<string, PObject>(key, dict[key]);
        }
        #endregion

        #region IEnumerable implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        public PDictionary()
        {
            dict = new Dictionary<string, PObject>();
            order = new List<string>();
        }

        public override PObject Clone()
        {
            var clone = new PDictionary();

            foreach (var kv in this)
                clone.Add(kv.Key, kv.Value.Clone());

            return clone;
        }

        public bool ContainsKey(string name)
        {
            return dict.ContainsKey(name);
        }

        public bool Remove(string key)
        {
            PObject obj;
            if (dict.TryGetValue(key, out obj))
            {
                dict.Remove(key);
                order.Remove(key);
                OnChildRemoved(key, obj);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            dict.Clear();
            order.Clear();
            OnCleared();
        }

        public bool ChangeKey(PObject obj, string newKey)
        {
            return ChangeKey(obj, newKey, null);
        }

        public bool ChangeKey(PObject obj, string newKey, PObject newValue)
        {
            var oldkey = GetKey(obj);
            if (oldkey == null || dict.ContainsKey(newKey))
                return false;

            dict.Remove(oldkey);
            dict.Add(newKey, newValue ?? obj);
            order[order.IndexOf(oldkey)] = newKey;
            if (newValue != null)
            {
                OnChildRemoved(oldkey, obj);
                OnChildAdded(newKey, newValue);
            }
            else
            {
                OnChildRemoved(oldkey, obj);
                OnChildAdded(newKey, obj);
            }
            return true;
        }

        public string GetKey(PObject obj)
        {
            foreach (var pair in dict)
            {
                if (pair.Value == obj)
                    return pair.Key;
            }
            return null;
        }

        public T Get<T>(string key) where T : PObject
        {
            PObject obj;

            if (!dict.TryGetValue(key, out obj))
                return null;

            return obj as T;
        }

        public bool TryGetValue<T>(string key, out T value) where T : PObject
        {
            PObject obj;

            if (!dict.TryGetValue(key, out obj))
            {
                value = default(T);
                return false;
            }

            value = obj as T;

            return value != null;
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			List<NSObject> objs = new List<NSObject> ();
			List<NSObject> keys = new List<NSObject> ();
			
			foreach (var key in order) {
				var val = dict[key].Convert ();
				objs.Add (val);
				keys.Add (new NSString (key));
			}
			return NSDictionary.FromObjectsAndKeys (objs.ToArray (), keys.ToArray ());
		}
#endif

        static int IndexOf(byte[] haystack, int startIndex, byte[] needle)
        {
            int maxLength = haystack.Length - needle.Length;
            int n;

            for (int i = startIndex; i < maxLength; i++)
            {
                for (n = 0; n < needle.Length; n++)
                {
                    if (haystack[i + n] != needle[n])
                        break;
                }

                if (n == needle.Length)
                    return i;
            }

            return -1;
        }

        public static new PDictionary FromByteArray(byte[] array, int startIndex, int length, out bool isBinary)
        {
            return (PDictionary)PObject.FromByteArray(array, startIndex, length, out isBinary);
        }

        public static new PDictionary FromByteArray(byte[] array, out bool isBinary)
        {
            return (PDictionary)PObject.FromByteArray(array, out isBinary);
        }

        public static PDictionary FromBinaryXml(byte[] array)
        {
            //find the raw plist within the .mobileprovision file
            int start = IndexOf(array, 0, BeginMarkerBytes);
            bool binary;
            int length;

            if (start < 0 || (length = (IndexOf(array, start, EndMarkerBytes) - start)) < 1)
                throw new Exception("Did not find XML plist in buffer.");

            length += EndMarkerBytes.Length;

            return FromByteArray(array, start, length, out binary);
        }

        [Obsolete("Use FromFile")]
        public static PDictionary Load(string fileName)
        {
            bool isBinary;
            return FromFile(fileName, out isBinary);
        }

        public new static PDictionary FromFile(string fileName)
        {
            bool isBinary;
            return FromFile(fileName, out isBinary);
        }

        public new static Task<PDictionary> FromFileAsync(string fileName)
        {
            return Task.Run(() => {
                bool isBinary;
                return FromFile(fileName, out isBinary);
            });
        }

        public new static PDictionary FromFile(string fileName, out bool isBinary)
        {
            return (PDictionary)PObject.FromFile(fileName, out isBinary);
        }

        public static PDictionary FromBinaryXml(string fileName)
        {
            return FromBinaryXml(File.ReadAllBytes(fileName));
        }

        protected override bool Reload(PropertyListFormat.ReadWriteContext ctx)
        {
            SuppressChangeEvents = true;
            var result = ctx.ReadDict(this);
            SuppressChangeEvents = false;
            if (result)
                OnChanged(EventArgs.Empty);
            return result;
        }

        public override string ToString()
        {
            return string.Format("[PDictionary: Items={0}]", dict.Count);
        }

        public void SetString(string key, string value)
        {
            var result = Get<PString>(key);

            if (result == null)
                this[key] = new PString(value);
            else
                result.Value = value;
        }

        public PString GetString(string key)
        {
            var result = Get<PString>(key);

            if (result == null)
                this[key] = result = new PString("");

            return result;
        }

        public PArray GetArray(string key)
        {
            var result = Get<PArray>(key);

            if (result == null)
                this[key] = result = new PArray();

            return result;
        }

        public override PObjectType Type
        {
            get { return PObjectType.Dictionary; }
        }
    }

}
