using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace net {
    public class Server : MonoBehaviour {
        public const int PORT = 6321;
        public const string DEFAULT_IP = "127.0.0.1";

        private const int CLIENTS_AMOUNT_TO_START = 2;

        private List<TcpClient> clients;

        public TcpListener listener;
        private bool isServerProcesing;

        public void Init() {
            clients = new List<TcpClient>();

            try {
                listener = new TcpListener(IPAddress.Any, PORT);
                listener.Start();

                StartListening();
                isServerProcesing = false;

            } catch (Exception ex) {

                Debug.LogError($"Socket error: {ex.Message}");
                return;
            }
        }

        private void Update() {
            if (!isServerProcesing) {
                return;
            }

            foreach (var client in clients) {
                NetworkStream stream = client.GetStream();
                if (stream.DataAvailable) {
                    StreamReader reader = new StreamReader(stream, true);
                    string data = reader.ReadLine();
                    if (data != null) {
                        SendDataFromClient(client, data);
                    }
                }
            }
        }

        private void SendDataFromClient(TcpClient clientWhoSends, string data) {
            TcpClient clientForSend = new TcpClient();
            foreach (var item in clients) {
                if (item != clientWhoSends) {
                    clientForSend = item;
                }
            }
            if(!clientForSend.Connected) {
                return;
            }

            try {
                StreamWriter writer = new StreamWriter(clientForSend.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            } catch (Exception ex) {
                Debug.Log("Write error : " + ex.Message);
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
            if (clients.Count != CLIENTS_AMOUNT_TO_START) {
                StartListening();
                return;
            }

            isServerProcesing = true;

            foreach (var item in clients) {
                try {

                    StreamWriter writer = new StreamWriter(item.GetStream());
                    writer.WriteLine(Client.START_COMMAND);
                    writer.Flush();

                } catch (Exception ex) {
                    Debug.Log("Write error : " + ex.Message);
                    return;
                }
            }
        }
        private void OnDestroy() {
            if (clients.Count != 0) {
                var host = clients[0];
                SendDataFromClient(host,Client.DISCONNECT_COMMAND);
            }
            listener.Stop();
        }
    }
}