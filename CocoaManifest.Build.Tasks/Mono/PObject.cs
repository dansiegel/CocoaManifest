using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CocoaManifest.Build.Mono
{
    abstract class PObject
    {
        public static PObject Create(PObjectType type)
        {
            switch (type)
            {
                case PObjectType.Dictionary:
                    return new PDictionary();
                case PObjectType.Array:
                    return new PArray();
                case PObjectType.Number:
                    return new PNumber(0);
                case PObjectType.Real:
                    return new PReal(0);
                case PObjectType.Boolean:
                    return new PBoolean(true);
                case PObjectType.Data:
                    return new PData(new byte[0]);
                case PObjectType.String:
                    return new PString("");
                case PObjectType.Date:
                    return new PDate(DateTime.Now);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<KeyValuePair<string, PObject>> ToEnumerable(PObject obj)
        {
            if (obj is PDictionary)
                return (PDictionary)obj;

            if (obj is PArray)
                return ((PArray)obj).Select(k => new KeyValuePair<string, PObject>(k is IPValueObject ? ((IPValueObject)k).Value.ToString() : null, k));

            return Enumerable.Empty<KeyValuePair<string, PObject>>();
        }

        PObjectContainer parent;
        public PObjectContainer Parent
        {
            get { return parent; }
            set
            {
                if (parent != null && value != null)
                    throw new NotSupportedException("Already parented.");

                parent = value;
            }
        }

        public abstract PObject Clone();

        public void Replace(PObject newObject)
        {
            var p = Parent;
            if (p is PDictionary dict)
            {
                var key = dict.GetKey(this);
                if (key == null)
                    return;
                Remove();
                dict[key] = newObject;
            }
            else if (p is PArray arr)
            {
                arr.Replace(this, newObject);
            }
        }

        public string Key
        {
            get
            {
                if (Parent is PDictionary dict)
                {
                    return dict.GetKey(this);
                }
                return null;
            }
        }

        public void Remove()
        {
            if (Parent is PDictionary dict)
            {
                dict.Remove(Key);
            }
            else if (Parent is PArray arr)
            {
                arr.Remove(this);
            }
            else
            {
                if (Parent == null)
                    throw new InvalidOperationException("Can't remove from null parent");
                throw new InvalidOperationException("Can't remove from parent " + Parent);
            }
        }

#if POBJECT_MONOMAC
		public abstract NSObject Convert ();
#endif

        public abstract PObjectType Type { get; }

        public static implicit operator PObject(string value)
        {
            return new PString(value);
        }

        public static implicit operator PObject(int value)
        {
            return new PNumber(value);
        }

        public static implicit operator PObject(double value)
        {
            return new PReal(value);
        }

        public static implicit operator PObject(bool value)
        {
            return new PBoolean(value);
        }

        public static implicit operator PObject(DateTime value)
        {
            return new PDate(value);
        }

        public static implicit operator PObject(byte[] value)
        {
            return new PData(value);
        }

        protected virtual void OnChanged(EventArgs e)
        {
            if (SuppressChangeEvents)
                return;

            Changed?.Invoke(this, e);

            if (Parent != null)
                Parent.OnCollectionChanged(Key, this);
        }

        protected bool SuppressChangeEvents
        {
            get; set;
        }

        public event EventHandler Changed;

        public byte[] ToByteArray(PropertyListFormat format)
        {
            using (var stream = new MemoryStream())
            {
                using (var context = format.StartWriting(stream))
                    context.WriteObject(this);
                return stream.ToArray();
            }
        }

        public byte[] ToByteArray(bool binary)
        {
            var format = binary ? PropertyListFormat.Binary : PropertyListFormat.Xml;

            return ToByteArray(format);
        }

        public string ToJson()
        {
            return Encoding.UTF8.GetString(ToByteArray(PropertyListFormat.Json));
        }

        public string ToXml()
        {
            return Encoding.UTF8.GetString(ToByteArray(PropertyListFormat.Xml));
        }

#if POBJECT_MONOMAC
		static readonly IntPtr selObjCType = Selector.GetHandle ("objCType");

		public static PObject FromNSObject (NSObject val)
		{
			if (val == null)
				return null;
			
			var dict = val as NSDictionary;
			if (dict != null) {
				var result = new PDictionary ();
				foreach (var pair in dict) {
					string k = pair.Key.ToString ();
					result[k] = FromNSObject (pair.Value);
				}
				return result;
			}
			
			var arr = val as NSArray;
			if (arr != null) {
				var result = new PArray ();
				uint count = arr.Count;
				for (uint i = 0; i < count; i++) {
					var obj = Runtime.GetNSObject (arr.ValueAt (i));
					if (obj != null)
						result.Add (FromNSObject (obj));
				}
				return result;
			}
			
			var str = val as NSString;
			if (str != null)
				return str.ToString ();
			
			var nr = val as NSNumber;
			if (nr != null) {
				char t;
				unsafe {
					t = (char) *((byte*) MonoMac.ObjCRuntime.Messaging.IntPtr_objc_msgSend (val.Handle, selObjCType));
				}
				if (t == 'c' || t == 'C' || t == 'B')
					return nr.BoolValue;
				return nr.Int32Value;
			}
			
			var date = val as NSDate;
			if (date != null)
				return (DateTime) date;
			
			var data = val as NSData;
			if (data != null) {
				var bytes = new byte[data.Length];
				System.Runtime.InteropServices.Marshal.Copy (data.Bytes, bytes, 0, (int)data.Length);
				return bytes;
			}
			
			throw new NotSupportedException (val.ToString ());
		}
#endif

        public static PObject FromByteArray(byte[] array, int startIndex, int length, out bool isBinary)
        {
            var ctx = PropertyListFormat.Binary.StartReading(array, startIndex, length);

            isBinary = true;

            try
            {
                if (ctx == null)
                {
                    isBinary = false;
                    ctx = PropertyListFormat.CreateReadContext(array, startIndex, length);
                    if (ctx == null)
                        return null;
                }

                return ctx.ReadObject();
            }
            finally
            {
                if (ctx != null)
                    ctx.Dispose();
            }
        }

        public static PObject FromByteArray(byte[] array, out bool isBinary)
        {
            return FromByteArray(array, 0, array.Length, out isBinary);
        }

        public static PObject FromString(string str)
        {
            var ctx = PropertyListFormat.CreateReadContext(Encoding.UTF8.GetBytes(str));
            if (ctx == null)
                return null;
            return ctx.ReadObject();
        }

        public static PObject FromStream(Stream stream)
        {
            var ctx = PropertyListFormat.CreateReadContext(stream);
            if (ctx == null)
                return null;
            return ctx.ReadObject();
        }

        public static PObject FromFile(string fileName)
        {
            return FromFile(fileName, out _);
        }

        public static Task<PObject> FromFileAsync(string fileName)
        {
            return Task.Run(() => {
                return FromFile(fileName, out bool isBinary);
            });
        }

        public static PObject FromFile(string fileName, out bool isBinary)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                var ctx = PropertyListFormat.Binary.StartReading(stream);

                isBinary = true;

                try
                {
                    if (ctx == null)
                    {
                        ctx = PropertyListFormat.CreateReadContext(stream);
                        isBinary = false;

                        if (ctx == null)
                            throw new FormatException("Unrecognized property list format.");
                    }

                    return ctx.ReadObject();
                }
                finally
                {
                    if (ctx != null)
                        ctx.Dispose();
                }
            }
        }

        public Task SaveAsync(string filename, bool atomic = false, bool binary = false)
        {
            return Task.Run(() => Save(filename, atomic, binary));
        }

        public void Save(string filename, bool atomic = false, bool binary = false)
        {
            var tempFile = atomic ? GetTempFileName(filename) : filename;

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(tempFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

                using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    using (var ctx = binary ? PropertyListFormat.Binary.StartWriting(stream) : PropertyListFormat.Xml.StartWriting(stream))
                        ctx.WriteObject(this);
                }

                if (atomic)
                {
                    if (File.Exists(filename))
                        File.Replace(tempFile, filename, null, true);
                    else
                        File.Move(tempFile, filename);
                }
            }
            finally
            {
                if (atomic)
                    File.Delete(tempFile); // just in case- no exception is raised if file is not found
            }
        }

        static string GetTempFileName(string filename)
        {
            var tempfile = filename + ".tmp";
            var i = 1;

            while (File.Exists(tempfile))
                tempfile = filename + ".tmp." + (i++);

            return tempfile;
        }
    }

}
