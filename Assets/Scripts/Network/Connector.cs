using System;
using System.Net;
using System.Net.Sockets;

public class Connector
{
    private Socket _socket;
    private Action<Socket> _sessionFactory;

    public void Connect(Action<Socket> sessionFactory, IPAddress addr, int port)
    {
        _sessionFactory = sessionFactory;
        IPEndPoint endPoint = new(addr, port);
        _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;

        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
        e.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleted);
        e.RemoteEndPoint = endPoint;
        RegisterConnect(e);
    }

    private void RegisterConnect(SocketAsyncEventArgs e)
    {
        bool pending = _socket.ConnectAsync(e);

        if (!pending)
        {
            OnConnectCompleted(null, e);
        }
    }

    private void OnConnectCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            _sessionFactory.Invoke(e.ConnectSocket);
            Console.WriteLine("Connected");
        }
        else
        {
            
        }
    }
}