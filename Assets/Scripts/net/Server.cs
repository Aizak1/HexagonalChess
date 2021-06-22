using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace net {
    public class Server : MonoBehaviour {
        public const int PORT = 6666;

        private List<TcpClient> clients;

        private TcpListener listener;
        private bool isServerProcesing;

        public void Init() {
            clients = new List<TcpClient>();

            try {
                listener = new TcpListener(IPAddress.Loopback, PORT);
                listener.Start();

                StartListening();
                isServerProcesing = true;

            } catch (Exception ex) {

                Debug.LogError($"Socket error: {ex.Message}");
                return;
            }
        }

        private void StartListening() {
            listener.BeginAcceptTcpClient(AcceptTcpClient, listener);
        }
        private void AcceptTcpClient(IAsyncResult ar) {
            TcpListener listener = (TcpListener)ar.AsyncState;

            var tcpClient = listener.EndAcceptTcpClient(ar);
            clients.Add(tcpClient);
            Debug.Log("Client conencted");

            StartListening();
        }
    }
}