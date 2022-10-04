using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Sockets;

public class LANMgr : MonoBehaviour
{
    private bool connection = false;
    public GameObject hostButton;
    public GameObject clientButton;
    
    public TMP_Text text_Status;
    public TMP_InputField inputF_IP;
    private string currentIP;

    public GameObject gameStatusMgrPrefab;

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var gameStatusMgrObj = Instantiate(gameStatusMgrPrefab, Vector3.zero,Quaternion.identity);
        gameStatusMgrObj.GetComponent<NetworkObject>().Spawn();
    }

    public void StartHost()
    {
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
        //UIMgr.Singleton.startGameButton.SetActive(true);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = inputF_IP.text;
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
                text_Status.text = currentIP;
                status += "ConnectedClients: \n";
                int clientCount = NetworkManager.Singleton.ConnectedClients.Count;

                // for (int i =1; i <= UIMgr.Singleton.userIcons.Count; i++)
                // {
                //     UIMgr.Singleton.userIcons[i-1].SetActive(i<=clientCount);
                // }

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
