using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CocoaManifest.Build.Mono
{
    abstract class PropertyListFormat
    {
        public static readonly PropertyListFormat Xml = new XmlFormat();
        public static readonly PropertyListFormat Binary = new BinaryFormat();
        public static readonly PropertyListFormat Json = new JsonFormat();

        // Stream must be seekable
        public static ReadWriteContext CreateReadContext(Stream input)
        {
            return Binary.StartReading(input) ?? Xml.StartReading(input);
        }

        public static ReadWriteContext CreateReadContext(byte[] array, int startIndex, int length)
        {
            return CreateReadContext(new MemoryStream(array, startIndex, length));
        }

        public static ReadWriteContext CreateReadContext(byte[] array)
        {
            return CreateReadContext(new MemoryStream(array, 0, array.Length));
        }

        // returns null if the input is not of the correct format. Stream must be seekable
        public abstract ReadWriteContext StartReading(Stream input);
        public abstract ReadWriteContext StartWriting(Stream output);

        public ReadWriteContext StartReading(byte[] array, int startIndex, int length)
        {
            return StartReading(new MemoryStream(array, startIndex, length));
        }

        public ReadWriteContext StartReading(byte[] array)
        {
            return StartReading(new MemoryStream(array, 0, array.Length));
        }

        class BinaryFormat : PropertyListFormat
        {
            // magic is bplist + 2 byte version id
            static readonly byte[] BPLIST_MAGIC = { 0x62, 0x70, 0x6C, 0x69, 0x73, 0x74 };  // "bplist"
            static readonly byte[] BPLIST_VERSION = { 0x30, 0x30 }; // "00"

            public override ReadWriteContext StartReading(Stream input)
            {
                if (input.Length < BPLIST_MAGIC.Length + 2)
                    return null;

                input.Seek(0, SeekOrigin.Begin);
                for (var i = 0; i < BPLIST_MAGIC.Length; i++)
                {
                    if ((byte)input.ReadByte() != BPLIST_MAGIC[i])
                        return null;
                }

                // skip past the 2 byte version id for now
                //  we currently don't bother checking it because it seems different versions of OSX might write different values here?
                input.Seek(2, SeekOrigin.Current);
                return new Context(input, true);
            }

            public override ReadWriteContext StartWriting(Stream output)
            {
                output.Write(BPLIST_MAGIC, 0, BPLIST_MAGIC.Length);
                output.Write(BPLIST_VERSION, 0, BPLIST_VERSION.Length);

                return new Context(output, false);
            }

            class Context : ReadWriteContext
            {

                static readonly DateTime AppleEpoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc); //see CFDateGetAbsoluteTime

                //https://github.com/mono/referencesource/blob/mono/mscorlib/system/datetime.cs
                const long TicksPerMillisecond = 10000;
                const long TicksPerSecond = TicksPerMillisecond * 1000;

                Stream stream;
                int currentLength;

                CFBinaryPlistTrailer trailer;

                //for writing
                List<object> objectRefs;
                int currentRef;
                long[] offsets;

                public Context(Stream stream, bool reading)
                {
                    this.stream = stream;
                    if (reading)
                    {
                        trailer = CFBinaryPlistTrailer.Read(this);
                        ReadObjectHead();
                    }
                }

                #region Binary reading members
                protected override bool ReadBool()
                {
                    return CurrentType == PlistType.@true;
                }

                protected override void ReadObjectHead()
                {
                    var b = stream.ReadByte();
                    var len = 0L;
                    var type = (PlistType)(b & 0xF0);
                    if (type == PlistType.@null)
                    {
                        type = (PlistType)b;
                    }
                    else
                    {
                        len = b & 0x0F;
                        if (len == 0xF)
                        {
                            ReadObjectHead();
                            len = ReadInteger();
                        }
                    }
                    CurrentType = type;
                    currentLength = (int)len;
                }

                protected override long ReadInteger()
                {
                    switch (CurrentType)
                    {
                        case PlistType.integer:
                            return ReadBigEndianInteger((int)Math.Pow(2, currentLength));
                    }

                    throw new NotSupportedException("Integer of type: " + CurrentType);
                }

                protected override double ReadReal()
                {
                    var bytes = ReadBigEndianBytes((int)Math.Pow(2, currentLength));
                    switch (CurrentType)
                    {
                        case PlistType.real:
                            switch (bytes.Length)
                            {
                                case 4:
                                    return BitConverter.ToSingle(bytes, 0);
                                case 8:
                                    return BitConverter.ToDouble(bytes, 0);
                            }
                            throw new NotSupportedException(bytes.Length + "-byte real");
                    }

                    throw new NotSupportedException("Real of type: " + CurrentType);
                }

                protected override DateTime ReadDate()
                {
                    var bytes = ReadBigEndianBytes(8);
                    var seconds = BitConverter.ToDouble(bytes, 0);
                    // We need to manually convert the seconds to ticks because
                    //  .NET DateTime/TimeSpan methods dealing with (milli)seconds
                    //  round to the nearest millisecond (bxc #29079)
                    return AppleEpoch.AddTicks((long)(seconds * TicksPerSecond));
                }

                protected override byte[] ReadData()
                {
                    var bytes = new byte[currentLength];
                    stream.Read(bytes, 0, currentLength);
                    return bytes;
                }

                protected override string ReadString()
                {
                    byte[] bytes;
                    switch (CurrentType)
                    {
                        case PlistType.@string: // ASCII
                            bytes = new byte[currentLength];
                            stream.Read(bytes, 0, bytes.Length);
                            return Encoding.ASCII.GetString(bytes);
                        case PlistType.wideString: //CFBinaryPList.c: Unicode string...big-endian 2-byte uint16_t
                            bytes = new byte[currentLength * 2];
                            stream.Read(bytes, 0, bytes.Length);
                            return Encoding.BigEndianUnicode.GetString(bytes);
                    }

                    throw new NotSupportedException("String of type: " + CurrentType);
                }

                public override bool ReadArray(PArray array)
                {
                    if (CurrentType != PlistType.array)
                        return false;

                    array.Clear();

                    // save currentLength as it will be overwritten by next ReadObjectHead call
                    var len = currentLength;
                    for (var i = 0; i < len; i++)
                    {
                        var obj = ReadObjectByRef();
                        if (obj != null)
                            array.Add(obj);
                    }

                    return true;
                }

                public override bool ReadDict(PDictionary dict)
                {
                    if (CurrentType != PlistType.dict)
                        return false;

                    dict.Clear();

                    // save currentLength as it will be overwritten by next ReadObjectHead call
                    var len = currentLength;
                    var keys = new string[len];
                    for (var i = 0; i < len; i++)
                        keys[i] = ((PString)ReadObjectByRef()).Value;
                    for (var i = 0; i < len; i++)
                        dict.Add(keys[i], ReadObjectByRef());

                    return true;
                }

                PObject ReadObjectByRef()
                {
                    // read index into offset table
                    var objRef = (long)ReadBigEndianUInteger(trailer.ObjectRefSize);

                    // read offset in file from table
                    var lastPos = stream.Position;
                    stream.Seek(trailer.OffsetTableOffset + objRef * trailer.OffsetEntrySize, SeekOrigin.Begin);
                    stream.Seek((long)ReadBigEndianUInteger(trailer.OffsetEntrySize), SeekOrigin.Begin);

                    ReadObjectHead();
                    var obj = ReadObject();

                    // restore original position
                    stream.Seek(lastPos, SeekOrigin.Begin);
                    return obj;
                }

                byte[] ReadBigEndianBytes(int count)
                {
                    var bytes = new byte[count];
                    stream.Read(bytes, 0, count);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    return bytes;
                }

                long ReadBigEndianInteger(int numBytes)
                {
                    var bytes = ReadBigEndianBytes(numBytes);
                    switch (numBytes)
                    {
                        case 1:
                            return bytes[0];
                        case 2:
                            return BitConverter.ToInt16(bytes, 0);
                        case 4:
                            return BitConverter.ToInt32(bytes, 0);
                        case 8:
                            return BitConverter.ToInt64(bytes, 0);
                    }
                    throw new NotSupportedException(bytes.Length + "-byte integer");
                }

                ulong ReadBigEndianUInteger(int numBytes)
                {
                    var bytes = ReadBigEndianBytes(numBytes);
                    switch (numBytes)
                    {
                        case 1:
                            return bytes[0];
                        case 2:
                            return BitConverter.ToUInt16(bytes, 0);
                        case 4:
                            return BitConverter.ToUInt32(bytes, 0);
                        case 8:
                            return BitConverter.ToUInt64(bytes, 0);
                    }
                    throw new NotSupportedException(bytes.Length + "-byte integer");
                }

                ulong ReadBigEndianUInt64()
                {
                    var bytes = ReadBigEndianBytes(8);
                    return BitConverter.ToUInt64(bytes, 0);
                }
                #endregion

                #region Binary writing members
                public override void WriteObject(PObject value)
                {
                    if (offsets == null)
                        InitOffsetTable(value);
                    base.WriteObject(value);
                }

                protected override void Write(PBoolean boolean)
                {
                    WriteObjectHead(boolean, boolean ? PlistType.@true : PlistType.@false);
                }

                protected override void Write(PNumber number)
                {
                    if (WriteObjectHead(number, PlistType.integer))
                        Write(number.Value);
                }

                protected override void Write(PReal real)
                {
                    if (WriteObjectHead(real, PlistType.real))
                        Write(real.Value);
                }

                protected override void Write(PDate date)
                {
                    if (WriteObjectHead(date, PlistType.date))
                    {
                        var bytes = MakeBigEndian(BitConverter.GetBytes(date.Value.Subtract(AppleEpoch).TotalSeconds));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                protected override void Write(PData data)
                {
                    var bytes = data.Value;
                    if (WriteObjectHead(data, PlistType.data, bytes.Length))
                        stream.Write(bytes, 0, bytes.Length);
                }

                protected override void Write(PString str)
                {
                    var type = PlistType.@string;
                    byte[] bytes;

                    if (str.Value.Any(c => c > 127))
                    {
                        type = PlistType.wideString;
                        bytes = Encoding.BigEndianUnicode.GetBytes(str.Value);
                    }
                    else
                    {
                        bytes = Encoding.ASCII.GetBytes(str.Value);
                    }

                    if (WriteObjectHead(str, type, str.Value.Length))
                        stream.Write(bytes, 0, bytes.Length);
                }

                protected override void Write(PArray array)
                {
                    if (!WriteObjectHead(array, PlistType.array, array.Count))
                        return;

                    var curRef = currentRef;

                    foreach (var item in array)
                        Write(GetObjRef(item), trailer.ObjectRefSize);

                    currentRef = curRef;

                    foreach (var item in array)
                        WriteObject(item);
                }

                protected override void Write(PDictionary dict)
                {
                    if (!WriteObjectHead(dict, PlistType.dict, dict.Count))
                        return;

                    // it sucks we have to loop so many times, but we gotta do it
                    //  if we want to lay things out the same way apple does

                    var curRef = currentRef;

                    //write key refs
                    foreach (var item in dict)
                        Write(GetObjRef(item.Key), trailer.ObjectRefSize);

                    //write value refs
                    foreach (var item in dict)
                        Write(GetObjRef(item.Value), trailer.ObjectRefSize);

                    currentRef = curRef;

                    //write keys and values
                    foreach (var item in dict)
                        WriteObject(item.Key);
                    foreach (var item in dict)
                        WriteObject(item.Value);
                }

                bool WriteObjectHead(PObject obj, PlistType type, int size = 0)
                {
                    var id = GetObjRef(obj);
                    if (offsets[id] != 0) // if we've already been written, don't write us again
                        return false;
                    offsets[id] = stream.Position;
                    switch (type)
                    {
                        case PlistType.@null:
                        case PlistType.@false:
                        case PlistType.@true:
                        case PlistType.fill:
                            stream.WriteByte((byte)type);
                            break;
                        case PlistType.date:
                            stream.WriteByte(0x33);
                            break;
                        case PlistType.integer:
                        case PlistType.real:
                            break;
                        default:
                            if (size < 15)
                            {
                                stream.WriteByte((byte)((byte)type | size));
                            }
                            else
                            {
                                stream.WriteByte((byte)((byte)type | 0xF));
                                Write(size);
                            }
                            break;
                    }
                    return true;
                }

                void Write(double value)
                {
                    if (value >= float.MinValue && value <= float.MaxValue)
                    {
                        stream.WriteByte((byte)PlistType.real | 0x2);
                        var bytes = MakeBigEndian(BitConverter.GetBytes((float)value));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        stream.WriteByte((byte)PlistType.real | 0x3);
                        var bytes = MakeBigEndian(BitConverter.GetBytes(value));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                void Write(int value)
                {
                    if (value < 0)
                    { //they always write negative numbers with 8 bytes
                        stream.WriteByte((byte)PlistType.integer | 0x3);
                        var bytes = MakeBigEndian(BitConverter.GetBytes((long)value));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else if (value >= 0 && value < byte.MaxValue)
                    {
                        stream.WriteByte((byte)PlistType.integer);
                        stream.WriteByte((byte)value);
                    }
                    else if (value >= short.MinValue && value < short.MaxValue)
                    {
                        stream.WriteByte((byte)PlistType.integer | 0x1);
                        var bytes = MakeBigEndian(BitConverter.GetBytes((short)value));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        stream.WriteByte((byte)PlistType.integer | 0x2);
                        var bytes = MakeBigEndian(BitConverter.GetBytes(value));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                void Write(long value, int byteCount)
                {
                    byte[] bytes;
                    switch (byteCount)
                    {
                        case 1:
                            stream.WriteByte((byte)value);
                            break;
                        case 2:
                            bytes = MakeBigEndian(BitConverter.GetBytes((short)value));
                            stream.Write(bytes, 0, bytes.Length);
                            break;
                        case 4:
                            bytes = MakeBigEndian(BitConverter.GetBytes((int)value));
                            stream.Write(bytes, 0, bytes.Length);
                            break;
                        case 8:
                            bytes = MakeBigEndian(BitConverter.GetBytes(value));
                            stream.Write(bytes, 0, bytes.Length);
                            break;
                        default:
                            throw new NotSupportedException(byteCount + "-byte integer");
                    }
                }

                void InitOffsetTable(PObject topLevel)
                {
                    objectRefs = new List<object>();

                    var count = 0;
                    MakeObjectRefs(topLevel, ref count);
                    trailer.ObjectRefSize = GetMinByteLength(count);
                    offsets = new long[count];
                }

                void MakeObjectRefs(object obj, ref int count)
                {
                    if (obj == null)
                        return;

                    if (ShouldDuplicate(obj) || !objectRefs.Any(val => PObjectEqualityComparer.Instance.Equals(val, obj)))
                    {
                        objectRefs.Add(obj);
                        count++;
                    }

                    // for containers, also count their contents
                    var pobj = obj as PObject;
                    if (pobj != null)
                    {
                        switch (pobj.Type)
                        {

                            case PObjectType.Array:
                                foreach (var child in (PArray)obj)
                                    MakeObjectRefs(child, ref count);
                                break;
                            case PObjectType.Dictionary:
                                foreach (var child in (PDictionary)obj)
                                    MakeObjectRefs(child.Key, ref count);
                                foreach (var child in (PDictionary)obj)
                                    MakeObjectRefs(child.Value, ref count);
                                break;
                        }
                    }
                }

                static bool ShouldDuplicate(object obj)
                {
                    var pobj = obj as PObject;
                    if (pobj == null)
                        return false;

                    return pobj.Type == PObjectType.Boolean || pobj.Type == PObjectType.Array || pobj.Type == PObjectType.Dictionary ||
                        (pobj.Type == PObjectType.String && ((PString)pobj).Value.Any(c => c > 255)); //LAMESPEC: this is weird. Some things are duplicated
                }

                int GetObjRef(object obj)
                {
                    if (currentRef < objectRefs.Count && PObjectEqualityComparer.Instance.Equals(objectRefs[currentRef], obj))
                        return currentRef++;

                    return objectRefs.FindIndex(val => PObjectEqualityComparer.Instance.Equals(val, obj));
                }

                static int GetMinByteLength(long value)
                {
                    if (value >= 0 && value < byte.MaxValue)
                        return 1;
                    if (value >= short.MinValue && value < short.MaxValue)
                        return 2;
                    if (value >= int.MinValue && value < int.MaxValue)
                        return 4;
                    return 8;
                }

                static byte[] MakeBigEndian(byte[] bytes)
                {
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    return bytes;
                }
                #endregion

                public override void Dispose()
                {
                    if (offsets != null)
                    {
                        trailer.OffsetTableOffset = stream.Position;
                        trailer.OffsetEntrySize = GetMinByteLength(trailer.OffsetTableOffset);
                        foreach (var offset in offsets)
                            Write(offset, trailer.OffsetEntrySize);

                        //LAMESPEC: seems like they always add 6 extra bytes here. not sure why
                        for (var i = 0; i < 6; i++)
                            stream.WriteByte(0);

                        trailer.Write(this);
                    }
                }

                class PObjectEqualityComparer : IEqualityComparer<object>
                {
                    public static readonly PObjectEqualityComparer Instance = new PObjectEqualityComparer();

                    PObjectEqualityComparer()
                    {
                    }

                    public new bool Equals(object x, object y)
                    {
                        var vx = x as IPValueObject;
                        var vy = y as IPValueObject;

                        if (vx == null && vy == null)
                            return EqualityComparer<object>.Default.Equals(x, y);

                        if (vx == null && x != null && vy.Value != null)
                            return vy.Value.Equals(x);

                        if (vy == null && y != null && vx.Value != null)
                            return vx.Value.Equals(y);

                        if (vx == null || vy == null)
                            return false;

                        return vx.Value.Equals(vy.Value);
                    }

                    public int GetHashCode(object obj)
                    {
                        var valueObj = obj as IPValueObject;
                        if (valueObj != null)
                            return valueObj.Value.GetHashCode();
                        return obj.GetHashCode();
                    }
                }

                struct CFBinaryPlistTrailer
                {
                    const int TRAILER_SIZE = 26;

                    public int OffsetEntrySize;
                    public int ObjectRefSize;
                    public long ObjectCount;
                    public long TopLevelRef;
                    public long OffsetTableOffset;

                    public static CFBinaryPlistTrailer Read(Context ctx)
                    {
                        var pos = ctx.stream.Position;
                        ctx.stream.Seek(-TRAILER_SIZE, SeekOrigin.End);
                        var result = new CFBinaryPlistTrailer
                        {
                            OffsetEntrySize = ctx.stream.ReadByte(),
                            ObjectRefSize = ctx.stream.ReadByte(),
                            ObjectCount = (long)ctx.ReadBigEndianUInt64(),
                            TopLevelRef = (long)ctx.ReadBigEndianUInt64(),
                            OffsetTableOffset = (long)ctx.ReadBigEndianUInt64()
                        };
                        ctx.stream.Seek(pos, SeekOrigin.Begin);
                        return result;
                    }

                    public void Write(Context ctx)
                    {
                        byte[] bytes;
                        ctx.stream.WriteByte((byte)OffsetEntrySize);
                        ctx.stream.WriteByte((byte)ObjectRefSize);
                        //LAMESPEC: apple's comments say this is the number of entries in the offset table, but this really *is* number of objects??!?!
                        bytes = MakeBigEndian(BitConverter.GetBytes((long)ctx.objectRefs.Count));
                        ctx.stream.Write(bytes, 0, bytes.Length);
                        bytes = new byte[8]; //top level always at offset 0
                        ctx.stream.Write(bytes, 0, bytes.Length);
                        bytes = MakeBigEndian(BitConverter.GetBytes(OffsetTableOffset));
                        ctx.stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        // Adapted from:
        //https://github.com/mono/monodevelop/blob/07d9e6c07e5be8fe1d8d6f4272d3969bb087a287/main/src/addins/MonoDevelop.MacDev/MonoDevelop.MacDev.Plist/PlistDocument.cs
        class XmlFormat : PropertyListFormat
        {
            const string PLIST_HEADER = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
";
            static readonly Encoding outputEncoding = new UTF8Encoding(false, false);

            public override ReadWriteContext StartReading(Stream input)
            {
                //allow DTD but not try to resolve it from web
                var settings = new XmlReaderSettings()
                {
                    CloseInput = true,
                    DtdProcessing = DtdProcessing.Ignore,
                    XmlResolver = null,
                };

                XmlReader reader = null;
                input.Seek(0, SeekOrigin.Begin);
                try
                {
                    reader = XmlReader.Create(input, settings);
                    reader.ReadToDescendant("plist");
                    while (reader.Read() && reader.NodeType != XmlNodeType.Element)
                    {
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex);
                }

                if (reader == null || reader.EOF)
                    return null;

                return new Context(reader);
            }

            public override ReadWriteContext StartWriting(Stream output)
            {
                var writer = new StreamWriter(output, outputEncoding);
                writer.Write(PLIST_HEADER);

                return new Context(writer);
            }

            class Context : ReadWriteContext
            {
                const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ssK";

                XmlReader reader;
                TextWriter writer;

                int indentLevel;
                string indentString;

                public Context(XmlReader reader)
                {
                    this.reader = reader;
                    ReadObjectHead();
                }
                public Context(TextWriter writer)
                {
                    this.writer = writer;
                    indentString = "";
                }

                #region XML reading members
                protected override void ReadObjectHead()
                {
                    try
                    {
                        CurrentType = (PlistType)Enum.Parse(typeof(PlistType), reader.LocalName);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(string.Format("Failed to parse PList data type: {0}", reader.LocalName), ex);
                    }
                }

                protected override bool ReadBool()
                {
                    // Create the PBoolean object, then move to the xml reader to next node
                    // so we are ready to parse the next object. 'bool' types don't have
                    // content so we have to move the reader manually, unlike integers which
                    // implicitly move to the next node because we parse the content.
                    var result = CurrentType == PlistType.@true;
                    reader.Read();
                    return result;
                }

                protected override long ReadInteger()
                {
                    return reader.ReadElementContentAsLong();
                }

                protected override double ReadReal()
                {
                    return reader.ReadElementContentAsDouble();
                }

                protected override DateTime ReadDate()
                {
                    return DateTime.ParseExact(reader.ReadElementContentAsString(), DATETIME_FORMAT, CultureInfo.InvariantCulture).ToUniversalTime();
                }

                protected override byte[] ReadData()
                {
                    return Convert.FromBase64String(reader.ReadElementContentAsString());
                }

                protected override string ReadString()
                {
                    return reader.ReadElementContentAsString();
                }

                public override bool ReadArray(PArray array)
                {
                    if (CurrentType != PlistType.array)
                        return false;

                    array.Clear();

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        return true;
                    }

                    // advance to first node
                    reader.ReadStartElement();
                    while (!reader.EOF && reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (!reader.Read())
                            break;
                    }

                    while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            ReadObjectHead();

                            var val = ReadObject();
                            if (val != null)
                                array.Add(val);
                        }
                        else if (!reader.Read())
                        {
                            break;
                        }
                    }

                    if (!reader.EOF && reader.NodeType == XmlNodeType.EndElement && reader.Name == "array")
                    {
                        reader.ReadEndElement();
                        return true;
                    }

                    return false;
                }

                public override bool ReadDict(PDictionary dict)
                {
                    if (CurrentType != PlistType.dict)
                        return false;

                    dict.Clear();

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        return true;
                    }

                    reader.ReadToDescendant("key");

                    while (!reader.EOF && reader.NodeType == XmlNodeType.Element)
                    {
                        var key = reader.ReadElementString();

                        while (!reader.EOF && reader.NodeType != XmlNodeType.Element && reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement)
                                throw new FormatException(string.Format("No value found for key {0}", key));
                        }

                        ReadObjectHead();
                        var result = ReadObject();
                        if (result != null)
                        {
                            // Keys are not required to be unique. The last entry wins.
                            dict[key] = result;
                        }

                        do
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "key")
                                break;

                            if (reader.NodeType == XmlNodeType.EndElement)
                                break;
                        } while (reader.Read());
                    }

                    if (!reader.EOF && reader.NodeType == XmlNodeType.EndElement && reader.Name == "dict")
                    {
                        reader.ReadEndElement();
                        return true;
                    }

                    return false;
                }
                #endregion

                #region XML writing members
                protected override void Write(PBoolean boolean)
                {
                    WriteLine(boolean.Value ? "<true/>" : "<false/>");
                }

                protected override void Write(PNumber number)
                {
                    WriteLine("<integer>" + SecurityElement.Escape(number.Value.ToString(CultureInfo.InvariantCulture)) + "</integer>");
                }

                protected override void Write(PReal real)
                {
                    WriteLine("<real>" + SecurityElement.Escape(real.Value.ToString(CultureInfo.InvariantCulture)) + "</real>");
                }

                protected override void Write(PDate date)
                {
                    WriteLine("<date>" + SecurityElement.Escape(date.Value.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture)) + "</date>");
                }

                protected override void Write(PData data)
                {
                    WriteLine("<data>" + SecurityElement.Escape(Convert.ToBase64String(data.Value)) + "</data>");
                }

                protected override void Write(PString str)
                {
                    WriteLine("<string>" + SecurityElement.Escape(str.Value) + "</string>");
                }

                protected override void Write(PArray array)
                {
                    if (array.Count == 0)
                    {
                        WriteLine("<array/>");
                        return;
                    }

                    WriteLine("<array>");
                    IncreaseIndent();

                    foreach (var item in array)
                        WriteObject(item);

                    DecreaseIndent();
                    WriteLine("</array>");
                }

                protected override void Write(PDictionary dict)
                {
                    if (dict.Count == 0)
                    {
                        WriteLine("<dict/>");
                        return;
                    }

                    WriteLine("<dict>");
                    IncreaseIndent();

                    foreach (var kv in dict)
                    {
                        WriteLine("<key>" + SecurityElement.Escape(kv.Key) + "</key>");
                        WriteObject(kv.Value);
                    }

                    DecreaseIndent();
                    WriteLine("</dict>");
                }

                void WriteLine(string value)
                {
                    writer.Write(indentString);
                    writer.Write(value);
                    writer.Write('\n');
                }

                void IncreaseIndent()
                {
                    indentString = new string('\t', ++indentLevel);
                }

                void DecreaseIndent()
                {
                    indentString = new string('\t', --indentLevel);
                }
                #endregion

                public override void Dispose()
                {
                    if (writer != null)
                    {
                        writer.Write("</plist>\n");
                        writer.Flush();
                        writer.Dispose();
                    }
                }
            }
        }

        class JsonFormat : PropertyListFormat
        {
            static readonly Encoding outputEncoding = new UTF8Encoding(false, false);

            public override ReadWriteContext StartReading(Stream input)
            {
                throw new NotImplementedException();
            }

            public override ReadWriteContext StartWriting(Stream output)
            {
                var writer = new StreamWriter(output, outputEncoding);

                return new Context(writer);
            }

            class Context : ReadWriteContext
            {
                const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ssK";

                readonly TextWriter writer;

                string indentString;
                int indentLevel;

                public Context(TextWriter writer)
                {
                    this.writer = writer;
                    indentString = "";
                }

                #region XML reading members
                protected override void ReadObjectHead()
                {
                    throw new NotImplementedException();
                }

                protected override bool ReadBool()
                {
                    throw new NotImplementedException();
                }

                protected override long ReadInteger()
                {
                    throw new NotImplementedException();
                }

                protected override double ReadReal()
                {
                    throw new NotImplementedException();
                }

                protected override DateTime ReadDate()
                {
                    throw new NotImplementedException();
                }

                protected override byte[] ReadData()
                {
                    throw new NotImplementedException();
                }

                protected override string ReadString()
                {
                    throw new NotImplementedException();
                }

                public override bool ReadArray(PArray array)
                {
                    throw new NotImplementedException();
                }

                public override bool ReadDict(PDictionary dict)
                {
                    throw new NotImplementedException();
                }
                #endregion

                #region XML writing members
                void Quote(string text)
                {
                    var quoted = new StringBuilder(text.Length + 2, (text.Length * 2) + 2);

                    quoted.Append("\"");
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (text[i] == '\\' || text[i] == '"')
                            quoted.Append('\\');
                        quoted.Append(text[i]);
                    }
                    quoted.Append("\"");

                    writer.Write(quoted);
                }

                protected override void Write(PBoolean boolean)
                {
                    writer.Write(boolean.Value ? "true" : "false");
                }

                protected override void Write(PNumber number)
                {
                    writer.Write(number.Value.ToString(CultureInfo.InvariantCulture));
                }

                protected override void Write(PReal real)
                {
                    writer.Write(real.Value.ToString(CultureInfo.InvariantCulture));
                }

                protected override void Write(PDate date)
                {
                    writer.Write("\"" + date.Value.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture) + "\"");
                }

                protected override void Write(PData data)
                {
                    Quote(Convert.ToBase64String(data.Value));
                }

                protected override void Write(PString str)
                {
                    Quote(str.Value);
                }

                protected override void Write(PArray array)
                {
                    if (array.Count == 0)
                    {
                        writer.Write("[]");
                        return;
                    }

                    writer.WriteLine("[");
                    IncreaseIndent();

                    int i = 0;
                    foreach (var item in array)
                    {
                        writer.Write(indentString);
                        WriteObject(item);
                        if (++i < array.Count)
                            writer.Write(',');
                        writer.WriteLine();
                    }

                    DecreaseIndent();
                    writer.Write(indentString);
                    writer.Write("]");
                }

                protected override void Write(PDictionary dict)
                {
                    if (dict.Count == 0)
                    {
                        writer.Write("{}");
                        return;
                    }

                    writer.WriteLine("{");
                    IncreaseIndent();

                    int i = 0;
                    foreach (var kv in dict)
                    {
                        writer.Write(indentString);
                        Quote(kv.Key);
                        writer.Write(": ");
                        WriteObject(kv.Value);
                        if (++i < dict.Count)
                            writer.Write(',');
                        writer.WriteLine();
                    }

                    DecreaseIndent();
                    writer.Write(indentString);
                    writer.Write("}");
                }

                void IncreaseIndent()
                {
                    indentString = new string(' ', (++indentLevel) * 2);
                }

                void DecreaseIndent()
                {
                    indentString = new string(' ', (--indentLevel) * 2);
                }
                #endregion

                public override void Dispose()
                {
                    if (writer != null)
                    {
                        writer.WriteLine();
                        writer.Flush();
                        writer.Dispose();
                    }
                }
            }
        }

        public abstract class ReadWriteContext : IDisposable
        {
            // Binary: The type is encoded in the 4 high bits; the low bits are data (except: null, true, false)
            // Xml: The enum value name == element tag name (this actually reads a superset of the format, since null, fill and wideString are not plist xml elements afaik)
            protected enum PlistType : byte
            {
                @null = 0x00,
                @false = 0x08,
                @true = 0x09,
                fill = 0x0F,
                integer = 0x10,
                real = 0x20,
                date = 0x30,
                data = 0x40,
                @string = 0x50,
                wideString = 0x60,
                array = 0xA0,
                dict = 0xD0,
            }

            #region Reading members
            public PObject ReadObject()
            {
                switch (CurrentType)
                {
                    case PlistType.@true:
                    case PlistType.@false:
                        return new PBoolean(ReadBool());
                    case PlistType.fill:
                        ReadObjectHead();
                        return ReadObject();

                    case PlistType.integer:
                        return new PNumber((int)ReadInteger()); //FIXME: should PNumber handle 64-bit values? ReadInteger can if necessary
                    case PlistType.real:
                        return new PReal(ReadReal());    //FIXME: we should probably make PNumber take floating point as well as ints

                    case PlistType.date:
                        return new PDate(ReadDate());
                    case PlistType.data:
                        return new PData(ReadData());

                    case PlistType.@string:
                    case PlistType.wideString:
                        return new PString(ReadString());

                    case PlistType.array:
                        var array = new PArray();
                        ReadArray(array);
                        return array;

                    case PlistType.dict:
                        var dict = new PDictionary();
                        ReadDict(dict);
                        return dict;
                }
                return null;
            }

            protected abstract void ReadObjectHead();
            protected PlistType CurrentType { get; set; }

            protected abstract bool ReadBool();
            protected abstract long ReadInteger();
            protected abstract double ReadReal();
            protected abstract DateTime ReadDate();
            protected abstract byte[] ReadData();
            protected abstract string ReadString();

            public abstract bool ReadArray(PArray array);
            public abstract bool ReadDict(PDictionary dict);
            #endregion

            #region Writing members
            public virtual void WriteObject(PObject value)
            {
                switch (value.Type)
                {
                    case PObjectType.Boolean:
                        Write((PBoolean)value);
                        return;
                    case PObjectType.Number:
                        Write((PNumber)value);
                        return;
                    case PObjectType.Real:
                        Write((PReal)value);
                        return;
                    case PObjectType.Date:
                        Write((PDate)value);
                        return;
                    case PObjectType.Data:
                        Write((PData)value);
                        return;
                    case PObjectType.String:
                        Write((PString)value);
                        return;
                    case PObjectType.Array:
                        Write((PArray)value);
                        return;
                    case PObjectType.Dictionary:
                        Write((PDictionary)value);
                        return;
                }
                throw new NotSupportedException(value.Type.ToString());
            }

            protected abstract void Write(PBoolean boolean);
            protected abstract void Write(PNumber number);
            protected abstract void Write(PReal real);
            protected abstract void Write(PDate date);
            protected abstract void Write(PData data);
            protected abstract void Write(PString str);
            protected abstract void Write(PArray array);
            protected abstract void Write(PDictionary dict);
            #endregion

            public abstract void Dispose();
        }
    }

}
