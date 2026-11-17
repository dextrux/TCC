
using System;
using System.Collections.Generic;
using CoreDomain.Scripts.Services.NetworkService;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using UnityEngine;

public class EOSMirrorLobbyService : INetworkLobbyService
{
    private const string RoomCodeAttribute = "id_jogo";
    private const int CodeLength = 6;
    private const int MaxCodeAttempts = 10;
    private const int DefaultMaxPlayers = 2;
    private const uint DiagnosticSearchLimit = 100;

    private enum LobbyOperation
    {
        None,
        CreatingCode,
        CreatingLobby,
        FindingLobby,
        JoiningLobby,
        ConnectingHost,
        ConnectingClient
    }

    private readonly EOSLobby _eosLobby;
    private readonly EOSMirrorNetworkManager _networkManager;
    private readonly INetworkService _networkService;
    private readonly System.Random _random = new System.Random();

    private LobbyOperation _operation = LobbyOperation.None;

    private string _pendingCode = "";
    private string _lobbyCode = "";
    private string _hostProductUserId = "";
    private string _status = "Aguardando Epic Online Services...";

    private int _codeAttempts;
    private int _connectedPlayers;

    private bool _isHost;
    private bool _isConnected;
    private bool _gameStarted;
    private bool _fallbackSearchActive;

    public bool IsInitialized => _networkService.IsInitialized;
    public bool IsBusy => _operation != LobbyOperation.None;
    public bool IsConnected => _isConnected;
    public bool IsHost => _isHost;

    public string LobbyCode => _lobbyCode;
    public string HostProductUserId => _hostProductUserId;
    public string Status => _status;

    public int ConnectedPlayers => _connectedPlayers;
    public int MaxPlayers => DefaultMaxPlayers;

    public event Action LobbyChanged;
    public event Action GameStarted;

    public EOSMirrorLobbyService(EOSLobby eosLobby, EOSMirrorNetworkManager networkManager, INetworkService networkService)
    {
        _eosLobby = eosLobby;
        _networkManager = networkManager;
        _networkService = networkService;

        _eosLobby.CreateLobbySucceeded += OnLobbyCreated;
        _eosLobby.CreateLobbyFailed += OnLobbyCreateFailed;
        _eosLobby.FindLobbiesSucceeded += OnLobbiesFound;
        _eosLobby.FindLobbiesFailed += OnLobbyFindFailed;
        _eosLobby.JoinLobbySucceeded += OnLobbyJoined;
        _eosLobby.JoinLobbyFailed += OnLobbyJoinFailed;
    }

    public void CreateLobby()
    {
        if (!CanBeginOperation())
        {
            return;
        }

        _isHost = false;
        _codeAttempts = 0;
        _fallbackSearchActive = false;
        _operation = LobbyOperation.CreatingCode;

        SetStatus("Procurando um código disponível...");

        Debug.Log("[Lobby] Criando sala. Product User ID local: " + _networkService.LocalProductUserId);

        SearchNewCode();
    }

    public void JoinLobby(string code)
    {
        if (!CanBeginOperation())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            SetStatus("Digite o código da sala.");
            return;
        }

        _isHost = false;
        _fallbackSearchActive = false;
        _pendingCode = code.Trim().ToUpperInvariant();
        _operation = LobbyOperation.FindingLobby;

        SetStatus("Procurando sala " + _pendingCode + "...");

        Debug.Log("[Lobby] Procurando código: " + _pendingCode);
        Debug.Log("[Lobby] Product User ID local: " + _networkService.LocalProductUserId);

        SearchLobby(_pendingCode);
    }

    public void StartGame()
    {
        if (!_isHost || !_isConnected || !NetworkServer.active)
        {
            SetStatus("Somente o host conectado pode iniciar a partida.");
            return;
        }

        if (_gameStarted)
        {
            return;
        }

        int players = _networkManager.GetConnectedPlayerCount();

        if (players != DefaultMaxPlayers)
        {
            SetStatus("Aguardando jogadores: " + players + "/" + DefaultMaxPlayers);
            return;
        }

        SetStatus("Iniciando partida...");

        NetworkServer.SendToAll(new LobbyStartGameMessage());
    }

    public void LeaveLobby()
    {
        if (_eosLobby != null && _eosLobby.ConnectedToLobby)
        {
            _eosLobby.LeaveLobby();
        }

        _networkService.Disconnect();

        ResetLobby("Você saiu da sala.");
    }

    public void HandleMirrorConnected()
    {
        if (_operation != LobbyOperation.ConnectingHost && _operation != LobbyOperation.ConnectingClient)
        {
            return;
        }

        _isConnected = true;
        _operation = LobbyOperation.None;

        if (_isHost)
        {
            _connectedPlayers = _networkManager.GetConnectedPlayerCount();

            SetStatus("Sala criada. Aguardando jogadores...");

            _networkManager.BroadcastLobbyPlayerCount();
        }
        else
        {
            SetStatus("Conectado à sala. Aguardando o host...");
        }

        Debug.Log("[Lobby] Conexão Mirror estabelecida. Host: " + _isHost);
    }

    public void HandleMirrorDisconnected()
    {
        if (!_isConnected && !IsBusy)
        {
            return;
        }

        if (_eosLobby != null && _eosLobby.ConnectedToLobby)
        {
            _eosLobby.LeaveLobby();
        }

        ResetLobby("Conexão encerrada.");
    }

    public void HandlePlayerCount(int count)
    {
        if (!_isConnected || _gameStarted)
        {
            return;
        }

        _connectedPlayers = count;

        if (_isHost)
        {
            SetStatus(count == DefaultMaxPlayers ? "Todos conectados. Você pode iniciar a partida." : "Aguardando jogadores...");
        }
        else
        {
            SetStatus("Conectado à sala. Aguardando o host...");
        }
    }

    public void HandleGameStarted()
    {
        if (!_isConnected || _gameStarted)
        {
            return;
        }

        _gameStarted = true;

        SetStatus("Carregando GYM...");

        GameStarted?.Invoke();
    }

    private bool CanBeginOperation()
    {
        if (IsBusy || _isConnected || NetworkClient.active || NetworkServer.active)
        {
            SetStatus("Já existe uma operação ou conexão em andamento.");
            return false;
        }

        if (!_networkService.IsInitialized)
        {
            SetStatus("Epic Online Services ainda não está inicializado.");
            return false;
        }

        if (_eosLobby == null)
        {
            SetStatus("EOSLobby não está configurado.");
            return false;
        }

        return true;
    }

    private void SearchNewCode()
    {
        if (_codeAttempts >= MaxCodeAttempts)
        {
            Fail("Não foi possível gerar um código disponível.");
            return;
        }

        _codeAttempts++;
        _pendingCode = GenerateCode();

        SearchLobby(_pendingCode);
    }

    private string GenerateCode()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        char[] result = new char[CodeLength];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = characters[_random.Next(characters.Length)];
        }

        return new string(result);
    }

    private void SearchLobby(string code)
    {
        LobbySearchSetParameterOptions parameter = new LobbySearchSetParameterOptions
        {
            ComparisonOp = ComparisonOp.Equal,
            Parameter = new AttributeData
            {
                Key = RoomCodeAttribute,
                Value = code
            }
        };

        _eosLobby.FindLobbies(1, new LobbySearchSetParameterOptions[] { parameter });
    }

    private void SearchPublicLobbies()
    {
        if (_operation != LobbyOperation.FindingLobby)
        {
            return;
        }

        _fallbackSearchActive = true;

        SetStatus("Verificando salas públicas disponíveis...");

        Debug.LogWarning("[Lobby] Busca por código sem resultados. Iniciando pesquisa diagnóstica sem filtro personalizado.");

        _eosLobby.FindLobbies(DiagnosticSearchLimit);
    }

    private void OnLobbiesFound(List<LobbyDetails> lobbies)
    {
        int resultCount = lobbies != null ? lobbies.Count : 0;

        Debug.Log("[Lobby] Pesquisa EOS concluída. Resultados: " + resultCount + " | Operação: " + _operation + " | Busca alternativa: " + _fallbackSearchActive);

        if (_operation == LobbyOperation.CreatingCode)
        {
            if (resultCount > 0)
            {
                SearchNewCode();
                return;
            }

            CreateEOSLobby();
            return;
        }

        if (_operation != LobbyOperation.FindingLobby)
        {
            return;
        }

        if (!_fallbackSearchActive)
        {
            if (resultCount == 0)
            {
                SearchPublicLobbies();
                return;
            }

            JoinEOSLobby(lobbies[0]);
            return;
        }

        LobbyDetails matchingLobby = FindMatchingLobby(lobbies, _pendingCode);

        if (matchingLobby != null)
        {
            Debug.Log("[Lobby] Sala encontrada na pesquisa alternativa: " + _pendingCode);

            JoinEOSLobby(matchingLobby);
            return;
        }

        Fail("Nenhuma sala encontrada com esse código.");

        Debug.LogWarning("[Lobby] A sala não foi encontrada nas duas pesquisas. Confira o Product User ID, Deployment ID e se o host permanece conectado.");
    }

    private LobbyDetails FindMatchingLobby(List<LobbyDetails> lobbies, string expectedCode)
    {
        if (lobbies == null)
        {
            return null;
        }

        int loggedLobbies = 0;

        foreach (LobbyDetails details in lobbies)
        {
            if (details == null)
            {
                continue;
            }

            string foundCode = "";

            bool hasRoomCode = TryGetRoomCode(details, out foundCode);

            if (loggedLobbies < 10)
            {
                Debug.Log("[Lobby] Sala pública encontrada. Código: " + (hasRoomCode ? foundCode : "(atributo ausente)") + " | Lobby ID: " + GetEOSLobbyId(details));

                loggedLobbies++;
            }

            if (hasRoomCode && string.Equals(foundCode, expectedCode, StringComparison.OrdinalIgnoreCase))
            {
                return details;
            }
        }

        return null;
    }

    private bool TryGetRoomCode(LobbyDetails details, out string code)
    {
        code = "";

        if (details == null)
        {
            return false;
        }

        LobbyDetailsCopyAttributeByKeyOptions options = new LobbyDetailsCopyAttributeByKeyOptions
        {
            AttrKey = RoomCodeAttribute
        };

        Result result = details.CopyAttributeByKey(ref options, out Epic.OnlineServices.Lobby.Attribute? attribute);

        if (result != Result.Success || !attribute.HasValue || !attribute.Value.Data.HasValue)
        {
            return false;
        }

        AttributeData attributeData = attribute.Value.Data.Value;

        if (attributeData.Value.ValueType != AttributeType.String)
        {
            return false;
        }

        code = attributeData.Value.AsUtf8.ToString();

        return !string.IsNullOrWhiteSpace(code);
    }

    private string GetEOSLobbyId(LobbyDetails details)
    {
        if (details == null)
        {
            return "";
        }

        LobbyDetailsCopyInfoOptions options = new LobbyDetailsCopyInfoOptions();

        Result result = details.CopyInfo(ref options, out LobbyDetailsInfo? info);

        if (result != Result.Success || !info.HasValue)
        {
            return "";
        }

        return info.Value.LobbyId.ToString();
    }

    private void CreateEOSLobby()
    {
        _operation = LobbyOperation.CreatingLobby;

        SetStatus("Criando sala...");

        AttributeData[] attributes = new AttributeData[]
        {
            new AttributeData
            {
                Key = RoomCodeAttribute,
                Value = _pendingCode
            }
        };

        _eosLobby.CreateLobby((uint)DefaultMaxPlayers, LobbyPermissionLevel.Publicadvertised, false, attributes);
    }

    private void JoinEOSLobby(LobbyDetails details)
    {
        LobbyDetailsCopyInfoOptions options = new LobbyDetailsCopyInfoOptions();

        Result result = details.CopyInfo(ref options, out LobbyDetailsInfo? info);

        if (result != Result.Success || !info.HasValue || info.Value.LobbyOwnerUserId == null)
        {
            Fail("Não foi possível identificar o host da sala.");
            return;
        }

        _hostProductUserId = info.Value.LobbyOwnerUserId.ToString();

        Debug.Log("[Lobby] Código encontrado: " + _pendingCode);
        Debug.Log("[Lobby] EOS Lobby ID: " + info.Value.LobbyId);
        Debug.Log("[Lobby] Host Product User ID: " + _hostProductUserId);
        Debug.Log("[Lobby] Client Product User ID: " + _networkService.LocalProductUserId);

        if (_hostProductUserId == _networkService.LocalProductUserId)
        {
            Fail("O host e o cliente possuem o mesmo Product User ID do EOS.");
            return;
        }

        _fallbackSearchActive = false;
        _operation = LobbyOperation.JoiningLobby;

        SetStatus("Entrando na sala...");

        _eosLobby.JoinLobby(details);
    }

    private void OnLobbyCreated(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        if (_operation != LobbyOperation.CreatingLobby)
        {
            return;
        }

        _lobbyCode = _pendingCode;
        _hostProductUserId = _networkService.LocalProductUserId;
        _isHost = true;
        _operation = LobbyOperation.ConnectingHost;

        _networkManager.maxConnections = DefaultMaxPlayers;

        Debug.Log("[Lobby] Sala EOS criada com sucesso.");
        Debug.Log("[Lobby] Código criado: " + _lobbyCode);
        Debug.Log("[Lobby] Host Product User ID: " + _hostProductUserId);

        if (_eosLobby.ConnectedLobbyDetails != null)
        {
            string publishedCode;

            bool hasPublishedCode = TryGetRoomCode(_eosLobby.ConnectedLobbyDetails, out publishedCode);

            Debug.Log("[Lobby] EOS Lobby ID criado: " + GetEOSLobbyId(_eosLobby.ConnectedLobbyDetails));

            if (hasPublishedCode)
            {
                Debug.Log("[Lobby] Código publicado no EOS: " + publishedCode);

                if (publishedCode != _lobbyCode)
                {
                    Debug.LogWarning("[Lobby] O código publicado é diferente do código exibido na interface.");
                }
            }
            else
            {
                Debug.LogWarning("[Lobby] Não foi possível confirmar o atributo id_jogo no lobby criado.");
            }
        }

        SetStatus("Iniciando servidor Mirror...");

        _networkService.StartHost();
    }

    private void OnLobbyCreateFailed(string error)
    {
        if (_operation != LobbyOperation.CreatingLobby)
        {
            return;
        }

        Fail("Falha ao criar sala: " + error);
    }

    private void OnLobbyJoined(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        if (_operation != LobbyOperation.JoiningLobby)
        {
            return;
        }

        _lobbyCode = _pendingCode;
        _operation = LobbyOperation.ConnectingClient;

        SetStatus("Conectando ao servidor...");

        _networkService.StartClient(_hostProductUserId);
    }

    private void OnLobbyJoinFailed(string error)
    {
        if (_operation != LobbyOperation.JoiningLobby)
        {
            return;
        }

        Fail("Falha ao entrar na sala: " + error);
    }

    private void OnLobbyFindFailed(string error)
    {
        if (_operation != LobbyOperation.CreatingCode && _operation != LobbyOperation.FindingLobby)
        {
            return;
        }

        Fail("Falha ao procurar sala: " + error);
    }

    private void Fail(string message)
    {
        _operation = LobbyOperation.None;
        _fallbackSearchActive = false;

        SetStatus(message);

        Debug.LogWarning("[EOSMirrorLobbyService] " + message);
    }

    private void ResetLobby(string message)
    {
        _operation = LobbyOperation.None;

        _isConnected = false;
        _isHost = false;
        _gameStarted = false;
        _fallbackSearchActive = false;

        _connectedPlayers = 0;
        _codeAttempts = 0;

        _pendingCode = "";
        _lobbyCode = "";
        _hostProductUserId = "";

        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        _status = message;

        LobbyChanged?.Invoke();
    }
}
