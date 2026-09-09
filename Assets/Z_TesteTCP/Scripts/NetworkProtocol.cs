using System.Globalization;

public struct PlayerInputMessage
{
    public int Sequence;
    public float MoveX;
    public float MoveZ;
    public float LookX;
    public float LookY;
    public bool Jump;
    public bool Fire;

    public PlayerInputMessage(int sequence, float moveX, float moveZ, float lookX, float lookY, bool jump, bool fire)
    {
        Sequence = sequence;
        MoveX = moveX;
        MoveZ = moveZ;
        LookX = lookX;
        LookY = lookY;
        Jump = jump;
        Fire = fire;
    }
}

public struct PlayerStateMessage
{
    public long Tick;
    public int PlayerId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public float Yaw;
    public float Pitch;
    public int Health;

    public PlayerStateMessage(long tick, int playerId, float positionX, float positionY, float positionZ, float yaw, float pitch, int health)
    {
        Tick = tick;
        PlayerId = playerId;
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        Yaw = yaw;
        Pitch = pitch;
        Health = health;
    }
}

public static class NetworkProtocol
{
    public const string Hello = "HELLO";
    public const string Welcome = "WELCOME";
    public const string Input = "INPUT";
    public const string State = "STATE";
    public const string Joined = "JOINED";
    public const string Despawn = "DESPAWN";
    public const string PlayerCount = "PLAYERCOUNT";
    public const string Hit = "HIT";
    public const string Died = "DIED";
    public const string Ping = "PING";
    public const string Pong = "PONG";
    public const string Bye = "BYE";
    public const string Full = "FULL";

    public static string[] Split(string message)
    {
        return message.Split('|');
    }

    public static string CreateHello(string playerName)
    {
        return Hello + "|" + SanitizeText(playerName);
    }

    public static string CreateWelcome(int playerId)
    {
        return Welcome + "|" + playerId;
    }

    public static string CreatePlayerCount(int playerCount)
    {
        return PlayerCount + "|" + playerCount;
    }

    public static string CreateJoined(int playerId)
    {
        return Joined + "|" + playerId;
    }

    public static string CreateDespawn(int playerId)
    {
        return Despawn + "|" + playerId;
    }

    public static string CreatePong(string timestamp)
    {
        return Pong + "|" + timestamp;
    }

    public static string CreateBye()
    {
        return Bye;
    }

    public static string CreateFull(string message)
    {
        return Full + "|" + SanitizeText(message);
    }

    public static string CreateHit(int attackerId, int victimId, int health)
    {
        return Hit + "|" + attackerId + "|" + victimId + "|" + health;
    }

    public static string CreateDied(int attackerId, int victimId)
    {
        return Died + "|" + attackerId + "|" + victimId;
    }

    public static string CreateInput(int sequence, float moveX, float moveZ, float lookX, float lookY, bool jump, bool fire)
    {
        return string.Format(CultureInfo.InvariantCulture, "INPUT|{0}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5}|{6}", sequence, moveX, moveZ, lookX, lookY, jump ? 1 : 0, fire ? 1 : 0);
    }

    public static bool TryParseInput(string[] parts, out PlayerInputMessage input)
    {
        input = default;

        if (parts.Length < 8 || parts[0] != Input)
        {
            return false;
        }

        int sequence;
        float moveX;
        float moveZ;
        float lookX;
        float lookY;

        if (!int.TryParse(parts[1], out sequence))
        {
            return false;
        }

        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out moveX))
        {
            return false;
        }

        if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out moveZ))
        {
            return false;
        }

        if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out lookX))
        {
            return false;
        }

        if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out lookY))
        {
            return false;
        }

        input = new PlayerInputMessage(sequence, moveX, moveZ, lookX, lookY, parts[6] == "1", parts[7] == "1");
        return true;
    }

    public static string CreateState(long tick, int playerId, float positionX, float positionY, float positionZ, float yaw, float pitch, int health)
    {
        return string.Format(CultureInfo.InvariantCulture, "STATE|{0}|{1}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}|{7}", tick, playerId, positionX, positionY, positionZ, yaw, pitch, health);
    }

    public static bool TryParseState(string[] parts, out PlayerStateMessage state)
    {
        state = default;

        if (parts.Length < 9 || parts[0] != State)
        {
            return false;
        }

        long tick;
        int playerId;
        float positionX;
        float positionY;
        float positionZ;
        float yaw;
        float pitch;
        int health;

        if (!long.TryParse(parts[1], out tick))
        {
            return false;
        }

        if (!int.TryParse(parts[2], out playerId))
        {
            return false;
        }

        if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out positionX))
        {
            return false;
        }

        if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out positionY))
        {
            return false;
        }

        if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out positionZ))
        {
            return false;
        }

        if (!float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out yaw))
        {
            return false;
        }

        if (!float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out pitch))
        {
            return false;
        }

        if (!int.TryParse(parts[8], out health))
        {
            return false;
        }

        state = new PlayerStateMessage(tick, playerId, positionX, positionY, positionZ, yaw, pitch, health);
        return true;
    }

    private static string SanitizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value.Replace("|", "_").Replace("\r", " ").Replace("\n", " ");
    }
}
