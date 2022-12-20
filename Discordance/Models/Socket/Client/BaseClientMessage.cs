using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discordance.Enums;

namespace Discordance.Models.Socket.Client;

public struct BaseClientMessage
{
    public ClientMessageType Type { get; init; }

    [JsonConverter(typeof(UlongJsonConverter))]
    public ulong GuildId { get; init; }

    [JsonConverter(typeof(UlongJsonConverter))]
    public ulong UserId { get; init; }

    [JsonConverter(typeof(StringClientMessageTypeJsonConverter))]
    public IClientMessage Payload { get; init; }
}

public class StringClientMessageTypeJsonConverter : JsonConverter<IClientMessage>
{
    public override IClientMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var text = jsonDoc.RootElement.GetRawText();
        var type = jsonDoc.RootElement.GetProperty("Type").GetInt32();
        var typeEnum = (ClientMessageType) type;

        return typeEnum switch
        {
            ClientMessageType.PlayQuery => JsonSerializer.Deserialize<PlayQueryMessage>(text, options),
            ClientMessageType.PlayQueueIndex => JsonSerializer.Deserialize<PlayQueueIndexMessage>(text, options),
            ClientMessageType.SeekPosition => JsonSerializer.Deserialize<SeekPositionMessage>(text, options),
            ClientMessageType.SetVolume => JsonSerializer.Deserialize<SetVolumeMessage>(text, options),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, IClientMessage value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class UlongJsonConverter : JsonConverter<ulong>
{
    public override ulong Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return ulong.Parse(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ulong value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}