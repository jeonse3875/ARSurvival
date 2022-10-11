using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using TMPro;

public class UIMgr : MonoBehaviour
{
    public LANMgr lANMgr;
    public TMP_Text textClientCount;
    public TMP_Text textLog;
    public TMP_Text textLogInUpdate;
    public TMP_Text textIP;

    private void Start()
    {
        lANMgr.clientCountSubject
        .DistinctUntilChanged()
        .Subscribe(SetClientCountIcon);

        lANMgr.ipSubject
        .Subscribe(ip => textIP.text = ip);

        CloudAnchorMgr.Singleton.logSubject
        .Subscribe(WriteLog);

        CloudAnchorMgr.Singleton.logInUpdateSubject
        .DistinctUntilChanged()
        .Subscribe(WriteLogInUpdate);
    }

    private void SetClientCountIcon(int count)
    {
        textClientCount.text = count.ToString();
    }

    private void WriteLog(string msg)
    {
        textLog.text = $"{msg}\n{textLog.text}";
    }

    private void WriteLogInUpdate(string msg)
    {
        textLogInUpdate.text = $"{msg}\n{textLog.text}";
    }
}
