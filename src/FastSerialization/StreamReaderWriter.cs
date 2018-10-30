﻿//     Copyright (c) Microsoft Corporation.  All rights reserved.
// This file is best viewed using outline mode (Ctrl-M Ctrl-O)
//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Diagnostics;
using System.IO;
using System.Text;      // For StringBuilder.

namespace FastSerialization
{
    /// <summary>
    /// A MemoryStreamReader is an implementation of the IStreamReader interface that works over a given byte[] array.  
    /// </summary>
#if STREAMREADER_PUBLIC
    public
#endif
    class MemoryStreamReader : IStreamReader
    {
        /// <summary>
        /// Create a IStreamReader (reads binary data) from a given byte buffer
        /// </summary>
        public MemoryStreamReader(byte[] data) : this(data, 0, data.Length) { }
        /// <summary>
        /// Create a IStreamReader (reads binary data) from a given subregion of a byte buffer 
        /// </summary>
        public MemoryStreamReader(byte[] data, int start, int length)
        {
            bytes = data;
            position = start;
            endPosition = length;
        }

        /// <summary>
        /// The total length of bytes that this reader can read.  
        /// </summary>
        public virtual long Length { get { return endPosition; } }

        #region implemenation of IStreamReader
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public byte ReadByte()
        {
            if (position >= endPosition)
            {
                Fill(1);
            }

            return bytes[position++];
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public short ReadInt16()
        {
            if (position + sizeof(short) > endPosition)
            {
                Fill(sizeof(short));
            }

            int ret = bytes[position] + (bytes[position + 1] << 8);
            position += sizeof(short);
            return (short)ret;
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public int ReadInt32()
        {
            if (position + sizeof(int) > endPosition)
            {
                Fill(sizeof(int));
            }

            int ret = bytes[position] + ((bytes[position + 1] + ((bytes[position + 2] + (bytes[position + 3] << 8)) << 8)) << 8);
            position += sizeof(int);
            return ret;
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public long ReadInt64()
        {
            uint low = (uint)ReadInt32();
            uint high = (uint)ReadInt32();
            return (long)((((ulong)high) << 32) + low);        // TODO find the most efficient way of doing this. 
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public string ReadString()
        {
            int len = ReadInt32();          // Expect first a character inclusiveCountRet.  -1 means null.
            if (len < 0)
            {
                Debug.Assert(len == -1);
                return null;
            }

            if (sb == null)
            {
                sb = new StringBuilder(len);
            }

            sb.Length = 0;

            Debug.Assert(len < Length);
            while (len > 0)
            {
                int b = ReadByte();
                if (b < 0x80)
                {
                    sb.Append((char)b);
                }
                else if (b < 0xE0)
                {
                    // TODO test this for correctness
                    b = (b & 0x1F);
                    b = b << 6 | (ReadByte() & 0x3F);
                    sb.Append((char)b);
                }
                else
                {
                    // TODO test this for correctness
                    b = (b & 0xF);
                    b = b << 6 | (ReadByte() & 0x3F);
                    b = b << 6 | (ReadByte() & 0x3F);

                    sb.Append((char)b);
                }
                --len;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public StreamLabel ReadLabel()
        {
            return (StreamLabel)ReadInt32();
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public virtual void Goto(StreamLabel label)
        {
            Debug.Assert(label != StreamLabel.Invalid);
            position = (int)label;
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public virtual StreamLabel Current
        {
            get
            {
                return (StreamLabel)position;
            }
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public virtual void GotoSuffixLabel()
        {
            Goto((StreamLabel)(Length - sizeof(StreamLabel)));
            Goto(ReadLabel());
        }
        /// <summary>
        /// Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing) { }
        #endregion

        #region private 
        internal /*protected*/ virtual void Fill(int minBytes)
        {
            throw new Exception("Streamreader read past end of buffer");
        }
        internal /*protected*/  byte[] bytes;
        internal /*protected*/  int position;
        internal /*protected*/  int endPosition;
        private StringBuilder sb;
        #endregion
    }

    // TODO is unsafe code worth it?
#if true
    /// <summary>
    /// A StreamWriter is an implementation of the IStreamWriter interface that generates a byte[] array. 
    /// </summary>
#if STREAMREADER_PUBLIC
    public
#endif
    class MemoryStreamWriter : IStreamWriter
    {
        /// <summary>
        /// Create IStreamWriter that writes its data to an internal byte[] buffer.  It will grow as needed. 
        /// Call 'GetReader' to get a IStreamReader for the written bytes. 
        /// 
        /// Call 'GetBytes' call to get the raw array.  Only the first 'Length' bytes are valid
        /// </summary>
        public MemoryStreamWriter(int initialSize = 64)
        {
            bytes = new byte[initialSize];
        }

        /// <summary>
        /// Returns a IStreamReader that will read the written bytes.  You cannot write additional bytes to the stream after making this call. 
        /// </summary>
        /// <returns></returns>
        public virtual MemoryStreamReader GetReader()
        {
            var readerBytes = bytes;
            if (bytes.Length - endPosition > 500000)
            {
                readerBytes = new byte[endPosition];
                Array.Copy(bytes, readerBytes, endPosition);
            }
            return new MemoryStreamReader(readerBytes, 0, endPosition);
        }
        /// <summary>
        /// The number of bytes written so far.  
        /// </summary>
        public virtual long Length { get { return endPosition; } }
        /// <summary>
        /// The the array that holds the serialized data.   
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetBytes() { return bytes; }

        /// <summary>
        /// Clears any data that was previously written.  
        /// </summary>
        public virtual void Clear() { endPosition = 0; }

        #region Implementation of IStreamWriter
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(byte value)
        {
            if (endPosition >= bytes.Length)
            {
                MakeSpace();
            }

            bytes[endPosition++] = value;
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(short value)
        {
            if (endPosition + sizeof(short) > bytes.Length)
            {
                MakeSpace();
            }

            int intValue = value;
            bytes[endPosition++] = (byte)intValue; intValue = intValue >> 8;
            bytes[endPosition++] = (byte)intValue; intValue = intValue >> 8;
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(int value)
        {
            if (endPosition + sizeof(int) > bytes.Length)
            {
                MakeSpace();
            }

            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(long value)
        {
            if (endPosition + sizeof(long) > bytes.Length)
            {
                MakeSpace();
            }

            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(StreamLabel value)
        {
            Write((int)value);
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void Write(string value)
        {
            if (value == null)
            {
                Write(-1);          // negative charCount means null. 
            }
            else
            {
                Write(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c <= 0x7F)
                    {
                        Write((byte)value[i]);                 // Only need one byte for UTF8
                    }
                    else if (c <= 0x7FF)
                    {
                        // TODO confirm that this is correct!
                        Write((byte)(0xC0 | (c >> 6)));                // Encode 2 byte UTF8
                        Write((byte)(0x80 | (c & 0x3F)));
                    }
                    else
                    {
                        // TODO confirm that this is correct!
                        Write((byte)(0xE0 | ((c >> 12) & 0xF)));        // Encode 3 byte UTF8
                        Write((byte)(0x80 | ((c >> 6) & 0x3F)));
                        Write((byte)(0x80 | (c & 0x3F)));
                    }
                }
            }
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public virtual StreamLabel GetLabel()
        {
            return (StreamLabel)Length;
        }
        /// <summary>
        /// Implementation of IStreamWriter
        /// </summary>
        public void WriteSuffixLabel(StreamLabel value)
        {
            // This is guaranteed to be uncompressed, but since we are not compressing anything, we can
            // simply write the value.  
            Write(value);
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing) { }
        #endregion 
        #region private

        /// <summary>
        /// Makespace makes at least sizeof(long) bytes available (or throws OutOfMemory)
        /// </summary>
        internal /* protected */ virtual void MakeSpace()
        {
            const int maxLength = 0x7FFFFFC7; // Max array length for byte[]
            int newLength = 0;

            // Make sure we don't exceed max possible size
            if (bytes.Length < (maxLength / 3) * 2)
            {
                newLength = (bytes.Length / 2) * 3;
            }
            else if (bytes.Length < maxLength - sizeof(long))      // Write(long) expects Makespace to make at 8 bytes of space.   
            {
                newLength = maxLength;                             // If we can do this, use up the last available length 
            }
            else
            {
                throw new OutOfMemoryException();                  // Can't make space anymore
            }

            Debug.Assert(bytes.Length + sizeof(long) <= newLength);
            byte[] newBytes = new byte[newLength];
            Array.Copy(bytes, newBytes, bytes.Length);
            bytes = newBytes;
        }
        internal /* protected */ byte[] bytes;
        internal /* protected */ int endPosition;
        #endregion
    }
#else
    /// <summary>
    /// A StreamWriter is an implementation of the IStreamWriter interface that generates a byte[] array. 
    /// </summary>
    unsafe class MemoryStreamWriter : IStreamWriter
    {
        public MemoryStreamWriter() : this(64) { }
        public MemoryStreamWriter(int size)
        {
            bytes = new byte[size];
            pinningHandle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            fixed (byte* bytesAsPtr = &bytes[0])
                bufferStart = bytesAsPtr;
            bufferCur = bufferStart;
            bufferEnd = &bufferStart[bytes.Length];
        }

        // TODO 
        public virtual long Length { get { return (int) (bufferCur - bufferStart); } }
        public virtual void Clear() { throw new Exception(); }

        public void Write(byte value)
        {
            if (bufferCur + sizeof(byte) > bufferEnd)
                DoMakeSpace();
            *((byte*)(bufferCur)) = value;
            bufferCur += sizeof(byte);
        }
        public void Write(short value)
        {
            if (bufferCur + sizeof(short) > bufferEnd)
                DoMakeSpace();
            *((short*)(bufferCur)) = value;
            bufferCur += sizeof(short);
        }
        public void Write(int value)
        {
            if (bufferCur + sizeof(int) <= bufferEnd)
            {
                *((int*)(bufferCur)) = value;
                bufferCur += sizeof(int);
            }
            else 
                WriteSlow(value);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void WriteSlow(int value)
        {
            DoMakeSpace();
            Debug.Assert(bufferCur + 8 < bufferEnd);
            Write(value);
        }

        public void Write(long value)
        {
            if (bufferCur + sizeof(long) > bufferEnd)
                DoMakeSpace();
            *((long*)(bufferCur)) = value;
            bufferCur += sizeof(long);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void DoMakeSpace()
        {
            endPosition = (int)(bufferCur - bufferStart);
            MakeSpace();
            bufferCur = &bufferStart[endPosition];
            bufferEnd = &bufferStart[bytes.Length];
        }

        public void Write(StreamLabel value)
        {
            Write((int)value);
        }
        public void Write(string value)
        {
            if (value == null)
            {
                Write(-1);          // negative bufferSize means null. 
            }
            else
            {
                Write(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c < 128)
                        Write((byte)value[i]);                 // Only need one byte for UTF8
                    else if (c < 2048)
                    {
                        // TODO confirm that this is correct!
                        Write((byte) (0xC0 | (c >> 6)));                // Encode 2 byte UTF8
                        Write((byte) (0x80 | (c & 0x3F)));
                    }
                    else
                    {
                        // TODO confirm that this is correct!
                        Write((byte) (0xE0 | ((c >> 12) & 0xF)));        // Encode 3 byte UTF8
                        Write((byte) (0x80 | ((c >> 6) & 0x3F)));
                        Write((byte) (0x80 | (c & 0x3F)));
                    }
                }
            }
        }
        public virtual StreamLabel GetLabel()
        {
            return (StreamLabel)Length;
        }
        public void WriteSuffixLabel(StreamLabel value)
        {
            // This is guaranteed to be uncompressed, but since we are not compressing anything, we can
            // simply write the value.  
            Write(value);
        }

        public void WriteToStream(Stream outputStream)
        {
            // TODO really big streams will overflow;
            outputStream.Write(bytes, 0, (int)Length);
        }
        // Note that the returned MemoryStreamReader is not valid if more writes are done.  
        public MemoryStreamReader GetReader() { return new MemoryStreamReader(bytes); }

    #region private
        ~MemoryStreamWriter()
        {
            this.Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (pinningHandle.IsAllocated)
                pinningHandle.Free();
        }

        internal /*protected*/  virtual void MakeSpace()
        {
            byte[] newBytes = new byte[bytes.Length * 3 / 2];
            Array.Copy(bytes, newBytes, bytes.Length);
            bytes = newBytes;
            fixed (byte* bytesAsPtr = &bytes[0])
                bufferStart = bytesAsPtr;
            bufferCur = &bufferStart[endPosition];
            bufferEnd = &bufferStart[bytes.Length];
        }
        internal /*protected*/  byte[] bytes;
        internal /*protected*/  int endPosition;

        private System.Runtime.InteropServices.GCHandle pinningHandle;
        byte* bufferStart;
        byte* bufferCur;
        byte* bufferEnd;
    #endregion
    }
#endif

    /// <summary>
    /// A IOStreamStreamReader hooks a MemoryStreamReader up to an input System.IO.Stream.  
    /// </summary>
#if STREAMREADER_PUBLIC
    public
#endif
    class IOStreamStreamReader : MemoryStreamReader, IDisposable
    {
        /// <summary>
        /// Create a new IOStreamStreamReader from the given file.  
        /// </summary>
        /// <param name="fileName"></param>
        public IOStreamStreamReader(string fileName)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)) { }

        /// <summary>
        /// Create a new IOStreamStreamReader from the given System.IO.Stream.   Optionally you can specify the size of the read buffer
        /// The stream will be closed by the IOStreamStreamReader when it is closed.  
        /// </summary>
        public IOStreamStreamReader(Stream inputStream, int bufferSize = defaultBufferSize, bool leaveOpen = false)
            : base(new byte[bufferSize + align], 0, 0)
        {
            Debug.Assert(bufferSize % align == 0);
            this.inputStream = inputStream;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// close the file or underlying stream and clean up 
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        #region overrides 
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public override StreamLabel Current
        {
            get
            {
                return (StreamLabel)(positionInStream + position);
            }
        }
        /// <summary>
        /// Implementation of IStreamReader
        /// </summary>
        public override void Goto(StreamLabel label)
        {
            uint offset = (uint)label - positionInStream;
            if (offset > (uint)endPosition)
            {
                positionInStream = (uint)label;
                position = endPosition = 0;
            }
            else
            {
                position = (int)offset;
            }
        }
        /// <summary>
        /// Implementation of MemoryStreamReader
        /// </summary>
        public override long Length { get { return inputStream.Length; } }
        #endregion 

        #region private
        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!leaveOpen)
                {
                    inputStream.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        internal /*protected*/  const int align = 8;        // Needs to be a power of 2
        internal /*protected*/  const int defaultBufferSize = 0x4000;  // 16K 

        /// <summary>
        /// Fill the buffer, making sure at least 'minimum' byte are available to read.  Throw an exception
        /// if there are not that many bytes.  
        /// </summary>
        /// <param name="minimum"></param>
        internal /*protected*/  override void Fill(int minimum)
        {
            if (endPosition != position)
            {
                int slideAmount = position & ~(align - 1);             // round down to stay aligned.  
                for (int i = slideAmount; i < endPosition; i++)        // Slide everything down.  
                {
                    bytes[i - slideAmount] = bytes[i];
                }

                endPosition -= slideAmount;
                position -= slideAmount;
                positionInStream += (uint)slideAmount;
            }
            else
            {
                positionInStream += (uint)position;
                endPosition = 0;
                position = 0;
                // if you are within one read of the end of file, go backward to read the whole block.  
                uint lastBlock = (uint)(((int)inputStream.Length - bytes.Length + align) & ~(align - 1));
                if (positionInStream >= lastBlock)
                {
                    position = (int)(positionInStream - lastBlock);
                }
                else
                {
                    position = (int)positionInStream & (align - 1);
                }

                positionInStream -= (uint)position;
            }

            Debug.Assert(positionInStream % align == 0);
            lock (inputStream)
            {
                inputStream.Seek(positionInStream + endPosition, SeekOrigin.Begin);
                for (; ; )
                {
#if !NETSTANDARD1_3
                    System.Threading.Thread.Sleep(0);       // allow for Thread.Interrupt
#endif
                    int count = inputStream.Read(bytes, endPosition, bytes.Length - endPosition);
                    if (count == 0)
                    {
                        break;
                    }

                    endPosition += count;
                    if (endPosition == bytes.Length)
                    {
                        break;
                    }
                }
            }
            if (endPosition - position < minimum)
            {
                throw new Exception("Read past end of stream.");
            }
        }
        internal /*protected*/  Stream inputStream;
        private bool leaveOpen;
        internal /*protected*/  uint positionInStream;
        #endregion
    }

    /// <summary>
    /// A PinnedStreamReader is an IOStream reader that will pin its read buffer.
    /// This allows it it support a 'GetPointer' API efficiently.   The 
    /// GetPointer API lets you access data from the stream as raw byte 
    /// blobs without having to copy the data.  
    /// </summary>
#if STREAMREADER_PUBLIC
    public
#endif
    sealed unsafe class PinnedStreamReader : IOStreamStreamReader
    {
        /// <summary>
        /// Create a new PinnedStreamReader that gets its data from a given file.  You can optionally set the size of the read buffer.  
        /// </summary>
        public PinnedStreamReader(string fileName, int bufferSize = defaultBufferSize)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read,
            FileShare.Read | FileShare.Delete), bufferSize)
        { }

        /// <summary>
        /// Create a new PinnedStreamReader that gets its data from a given System.IO.Stream.  You can optionally set the size of the read buffer.  
        /// The stream will be closed by the PinnedStreamReader when it is closed.  
        /// </summary>
        public PinnedStreamReader(Stream inputStream, int bufferSize = defaultBufferSize)
            : base(inputStream, bufferSize)
        {
            // Pin the array
            pinningHandle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            fixed (byte* bytesAsPtr = &bytes[0])
            {
                bufferStart = bytesAsPtr;
            }
        }

        /// <summary>
        /// Clone the PinnnedStreamReader so that it reads from the same stream as this one.   They will share the same
        /// System.IO.Stream, but each will lock and seek when accessing that stream so they can both safely share it.  
        /// </summary>
        /// <returns></returns>
        public PinnedStreamReader Clone()
        {
            PinnedStreamReader ret = new PinnedStreamReader(inputStream, bytes.Length - align);
            return ret;
        }

        /// <summary>
        /// Get a byte* pointer to the input buffer at 'Position' in the IReadStream that is at least 'length' bytes long.  
        /// (thus ptr to ptr+len is valid).   Note that length cannot be larger than the buffer size passed to the reader
        /// when it was constructed.  
        /// </summary>
        public unsafe byte* GetPointer(StreamLabel Position, int length)
        {
            Goto(Position);
            return GetPointer(length);
        }

        /// <summary>
        /// Get a byte* pointer to the input buffer at the current read position is at least 'length' bytes long.  
        /// (thus ptr to ptr+len is valid).   Note that length cannot be larger than the buffer size passed to the reader
        /// when it was constructed.  
        /// </summary>
        public unsafe byte* GetPointer(int length)
        {
            if (position + length > endPosition)
            {
                Fill(length);
            }
#if DEBUG
            fixed (byte* bytesAsPtr = &bytes[0])
                Debug.Assert(bytesAsPtr == bufferStart, "Error, buffer not pinnned");
            Debug.Assert(position < bytes.Length);
#endif
            return (byte*)(&bufferStart[position]);
        }

        #region private
        ~PinnedStreamReader()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (pinningHandle.IsAllocated)
            {
                pinningHandle.Free();
            }

            base.Dispose(disposing);
        }

        private System.Runtime.InteropServices.GCHandle pinningHandle;
        private byte* bufferStart;
        #endregion
    }

#if PINNEDSTREAMREADER_TESTS
    public static class PinnedStreamTests
    {
        public static void Tests()
        {
            string testOrig = "text.orig";

            Random r = new Random(23);

            for (int j = 0; j < 10; j++)
            {
                for (int fileSize = 1023; fileSize <= 1025; fileSize++)
                {
                    CreateDataFile(testOrig, fileSize);
                    byte[] origData = File.ReadAllBytes(testOrig);

                    for (int bufferSize = 16; bufferSize < 300; bufferSize += 24)
                    {
                        FileStream testData = File.OpenRead(testOrig);
                        PinnedStreamReader reader = new PinnedStreamReader(testData, bufferSize);

                        // Try reading back in various seek positions. 
                        for (int i = 0; i < 100; i++)
                        {
                            int position = r.Next(0, origData.Length);
                            int size = r.Next(0, bufferSize) + 1;

                            reader.Goto((StreamLabel)position);
                            Compare(reader, origData, position, size);
                        }
                        reader.Close();
                    }
                }
                Console.WriteLine("Finished Round " + j);
            }
        }

        static int compareCount = 0;

        private static void Compare(PinnedStreamReader reader, byte[] buffer, int offset, int chunkSize)
        {
            compareCount++;
            if (compareCount == -1)
                Debugger.Break();

            for (int pos = offset; pos < buffer.Length; pos += chunkSize)
            {
                if (pos + chunkSize > buffer.Length)
                    chunkSize = buffer.Length - pos;
                CompareBuffer(reader.GetPointer(chunkSize), buffer, pos, chunkSize);
                reader.Skip(chunkSize);
            }
        }

        private unsafe static bool CompareBuffer(IntPtr ptr, byte[] buffer, int offset, int size)
        {
            byte* bytePtr = (byte*)ptr;

            for (int i = 0; i < size; i++)
            {
                if (buffer[i + offset] != bytePtr[i])
                {
                    Debug.Assert(false);
                    return false;
                }
            }
            return true;
        }
        private static void CreateDataFile(string name, int length)
        {
            FileStream stream = File.Open(name, FileMode.Create);
            byte val = 0;
            for (int i = 0; i < length; i++)
                stream.WriteByte(val++);
            stream.Close();
        }

    }
#endif

    /// <summary>
    /// A IOStreamStreamWriter hooks a MemoryStreamWriter up to an output System.IO.Stream
    /// </summary>
#if STREAMREADER_PUBLIC
    public
#endif
    class IOStreamStreamWriter : MemoryStreamWriter, IDisposable
    {
        /// <summary>
        /// Create a IOStreamStreamWriter that writes its data to a given file that it creates
        /// </summary>
        /// <param name="fileName"></param>
        public IOStreamStreamWriter(string fileName) : this(new FileStream(fileName, FileMode.Create)) { }

        /// <summary>
        /// Create a IOStreamStreamWriter that writes its data to a System.IO.Stream
        /// </summary>
        public IOStreamStreamWriter(Stream outputStream, int bufferSize = defaultBufferSize + sizeof(long), bool leaveOpen = false)
            : base(bufferSize)
        {
            this.outputStream = outputStream;
            this.leaveOpen = leaveOpen;
            streamLength = outputStream.Length;
        }

        /// <summary>
        /// Flush any written data to the underlying System.IO.Stream
        /// </summary>
        public void Flush()
        {
            outputStream.Write(bytes, 0, endPosition);
            streamLength += endPosition;
            endPosition = 0;
            outputStream.Flush();
        }

        /// <summary>
        /// Insures the bytes in the stream are written to the stream and cleans up resources.  
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Access the underlying System.IO.Stream.   You should avoid using this if at all possible.  
        /// </summary>
        public Stream RawStream { get { return outputStream; } }

        #region overrides
        /// <summary>
        /// Implementation of the MemoryStreamWriter interface 
        /// </summary>
        public override long Length
        {
            get
            {
                Debug.Assert(streamLength == outputStream.Length);
                return base.Length + streamLength;
            }
        }
        /// <summary>
        /// Implementation of the IStreamWriter interface 
        /// </summary>
        public override StreamLabel GetLabel()
        {
            long len = Length;
            if (len != (uint)len)
            {
                throw new NotSupportedException("Streams larger than 4 GB.  You need to use /MaxEventCount to limit the size.");
            }

            return (StreamLabel)len;
        }
        /// <summary>
        /// Implementation of the MemoryStreamWriter interface 
        /// </summary>
        public override void Clear()
        {
            outputStream.SetLength(0);
            streamLength = 0;
        }
        /// <summary>
        /// Implementation of the MemoryStreamWriter interface 
        /// </summary>
        public override MemoryStreamReader GetReader() { throw new InvalidOperationException(); }
        /// <summary>
        /// Implementation of the MemoryStreamWriter interface 
        /// </summary>
        public override byte[] GetBytes() { throw new InvalidOperationException(); }
        #endregion 
        #region private
        internal /*protected*/  override void MakeSpace()
        {
            Debug.Assert(endPosition > bytes.Length - sizeof(long));
            outputStream.Write(bytes, 0, endPosition);
            streamLength += endPosition;
            endPosition = 0;
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
                if (!leaveOpen)
                {
                    outputStream.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private const int defaultBufferSize = 1024 * 8 - sizeof(long);
        private Stream outputStream;
        private bool leaveOpen;
        private long streamLength;

        #endregion
    }
}
