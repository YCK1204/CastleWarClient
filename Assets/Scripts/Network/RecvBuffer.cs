using System;

public class RecvBuffer
{
    private ArraySegment<byte> _buffer;
    private int _writePos = 0;
    private int _readPos = 0;
    
    public int ReadSize { get => _writePos - _readPos; }
    public int WriteSize { get => _buffer.Count - _writePos; }
    public ArraySegment<byte> ReadSegment
    {
        get => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, ReadSize);
    }

    public ArraySegment<byte> WriteSegment
    {
        get => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, WriteSize);
    }
    
    public RecvBuffer(ushort size)
    {
        _buffer = new ArraySegment<byte>(new byte[size], 0, size);
    }

    public bool OnWrite(int size)
    {
        if (size > WriteSize)
            return false;
        _writePos += size;
        return true;
    }

    public bool OnRead(int size)
    {
        if (size > ReadSize)
            return false;
        _readPos += size;
        return true;
    }

    public void Clean()
    {
        int readSize = ReadSize;
        if (_readPos == _writePos)
        {
            _writePos = 0;
            _readPos = 0;
        }
        else
        {
            Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, readSize);
            _readPos = 0;
            _writePos = readSize;
        }
    }
}