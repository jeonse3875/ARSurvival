using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;
using UniRx;

public class LANMgr : MonoBehaviour
{
    private bool connection = false;
    public GameObject hostButton;
    public GameObject clientButton;
    public TMP_InputField inputF_IP;
    private string currentIP;

    public Subject<int> clientCountSubject = new Subject<int>();
    public Subject<string> ipSubject = new Subject<string>();

    public GameObject gameStatusMgrPrefab;

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var gameStatusMgrObj = Instantiate(gameStatusMgrPrefab, Vector3.zero, Quaternion.identity);
        gameStatusMgrObj.GetComponent<NetworkObject>().Spawn();
    }

    public void StartHost()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            "127.0.0.1",
            (ushort)7777,
            "0.0.0.0"
        );
        connection = NetworkManager.Singleton.StartHost();
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                currentIP = ip.ToString();
                break;
            }
        }
    }

    public void StartClient()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            inputF_IP.text,
            (ushort)7777
        );
        connection = NetworkManager.Singleton.StartClient();
    }

    private void ActivateStartButtons()
    {
        hostButton.SetActive(true);
        clientButton.SetActive(true);
    }

    private void DeactivateStartButtons()
    {
        hostButton.SetActive(false);
        clientButton.SetActive(false);
    }

    private void Update()
    {
        DeactivateStartButtons();

        string status = "";
        status += $"Connection: {connection.ToString()}\n";
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            ActivateStartButtons();
        }
        else
        {
            var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            status += "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name + "\n";
            status += "Mode: " + mode + "\n";

            if (NetworkManager.Singleton.IsServer)
            {
                status += $"IP: {currentIP}\n";
                ipSubject.OnNext(currentIP);
                status += "ConnectedClients: \n";
                int clientCount = NetworkManager.Singleton.ConnectedClients.Count;

                clientCountSubject.OnNext(clientCount);

                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    status += $"uid: {uid.ToString()}\n";
                }
            }
            else
            {
                //client
            }
        }
    }
}
