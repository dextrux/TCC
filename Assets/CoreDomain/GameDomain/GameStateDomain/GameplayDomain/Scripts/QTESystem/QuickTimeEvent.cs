
using UnityEngine;

public class QuickTimeEvent : MonoBehaviour
{
    [Header("Configuração do QTE")]
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private float timePerKey = 1f;

    [Header("Referências")]
    [SerializeField] private QuickTimeEventUI qteUI;

    private readonly KeyCode[] possibleKeys =
    {
        KeyCode.W,
        KeyCode.A,
        KeyCode.S,
        KeyCode.D
    };

    private bool qteActive;

    private KeyCode currentKey;

    private int currentStep;

    private float remainingTime;

    private void Update()
    {
        // Inicia o QTE pressionando F.
        if (!qteActive)
        {
            if (Input.GetKeyDown(KeyCode.F))
                StartQTE();

            return;
        }

        // Diminui o tempo restante.
        remainingTime -= Time.deltaTime;

        // Atualiza a barra de tempo.
        qteUI.UpdateTimer(remainingTime / timePerKey);

        // Verifica se o tempo acabou.
        if (remainingTime <= 0f)
        {
            FinishQTE(false);
            return;
        }

        CheckInput();
    }

    private void StartQTE()
    {
        if (qteUI == null)
        {
            Debug.LogError(
                "QuickTimeEventUI não foi configurado!"
            );

            return;
        }

        currentStep = 0;
        qteActive = true;

        GenerateNextKey();
    }

    private void GenerateNextKey()
    {
        int randomIndex =
            Random.Range(0, possibleKeys.Length);

        currentKey = possibleKeys[randomIndex];

        remainingTime = timePerKey;

        qteUI.ShowLetter(currentKey.ToString());

        Debug.Log("Próxima tecla: " + currentKey);
    }

    private void CheckInput()
    {
        for (int i = 0; i < possibleKeys.Length; i++)
        {
            if (Input.GetKeyDown(possibleKeys[i]))
            {
                if (possibleKeys[i] == currentKey)
                {
                    CorrectInput();
                }
                else
                {
                    FinishQTE(false);
                }

                return;
            }
        }
    }

    private void CorrectInput()
    {
        currentStep++;

        Debug.Log(
            "Acertou! Etapa: " + currentStep
        );

        if (currentStep >= sequenceLength)
        {
            FinishQTE(true);
            return;
        }

        GenerateNextKey();
    }

    private void FinishQTE(bool success)
    {
        qteActive = false;

        if (success)
        {
            qteUI.ShowResult("SUCESSO!");

            Debug.Log("QTE concluído com sucesso!");
        }
        else
        {
            qteUI.ShowResult("FALHOU!");

            Debug.Log("QTE falhou!");
        }
    }
}
