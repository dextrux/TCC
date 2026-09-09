using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PublicIpResolver : MonoBehaviour
{
    public string PublicIp { get; private set; } = "";
    public string StatusMessage { get; private set; } = "";
    public bool IsBusy { get; private set; }

    public void Refresh()
    {
        if (IsBusy)
        {
            return;
        }

        StartCoroutine(FetchPublicIp());
    }

    public void Clear()
    {
        StopAllCoroutines();

        PublicIp = "";
        StatusMessage = "";
        IsBusy = false;
    }

    private IEnumerator FetchPublicIp()
    {
        IsBusy = true;
        PublicIp = "";
        StatusMessage = "Buscando IP público...";

        using (UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org"))
        {
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                PublicIp = request.downloadHandler.text.Trim();
                StatusMessage = "IP público encontrado.";
                Debug.Log("Public IP: " + PublicIp);
            }
            else
            {
                StatusMessage = "Não foi possível descobrir o IP público.";
                Debug.LogWarning("Failed to get public IP: " + request.error);
            }
        }

        IsBusy = false;
    }
}
