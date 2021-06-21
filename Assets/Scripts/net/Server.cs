using System.Collections;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System.Net;

public class Server : MonoBehaviour
{
    public const int PORT = 6441;

    private List<ServerClient> clients;
    private List<ServerClient> clientsToDisconnect;

    private TcpListener listener;
    private bool isServerProcesing;

    public void Init() {
        clients = new List<ServerClient>();
        clientsToDisconnect = new List<ServerClient>();

        try {
            listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();

            StartListening();
            isServerProcesing = true;

        } catch (Exception ex) {

            Debug.LogError($"Socket error: {ex.Message}");
            return;
        }
    }

    private void StartListening() {
        var res = listener.BeginAcceptTcpClient(AcceptTcpClient, listener);
    }

    private void AcceptTcpClient(IAsyncResult asyncResult) {
        TcpListener listener = (TcpListener)asyncResult.AsyncState;
        ServerClient serverClient = new ServerClient(listener.EndAcceptTcpClient(asyncResult));
        clients.Add(serverClient);

        StartListening();

        Debug.Log("User has connected to the server");
    }
}

public class ServerClient {
    public string clientName;
    public TcpClient tcp;

    public ServerClient(TcpClient tcp) {
        this.tcp = tcp;
    }
}
