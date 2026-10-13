using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenMeter.Internal;

/// <summary>
/// One tool fee in the catalog: either a bare number (the price per 1,000 calls) or <c>{ "price": …, "unit": "…" }</c>.
/// </summary>
[JsonConverter(typeof(ToolFeeJsonConverter))]
internal sealed class ToolFeeJson
{
    public decimal Price { get; set; }

    /// <summary>The unit as written; null for the bare-number form (per call).</summary>
    public string? Unit { get; set; }
}

internal sealed class ToolFeeJsonConverter : JsonConverter<ToolFeeJson>
{
    public override ToolFeeJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return new ToolFeeJson { Price = reader.GetDecimal() };
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A tool fee is a number or an object with \"price\" and \"unit\".");

        var fee = new ToolFeeJson();
        var hasPrice = false;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();
            reader.Read();
            switch (name)
            {
                case "price": fee.Price = reader.GetDecimal(); hasPrice = true; break;
                case "unit": fee.Unit = reader.GetString(); break;
                default: throw new JsonException($"Unknown tool fee property \"{name}\".");
            }
        }
        if (!hasPrice)
            throw new JsonException("A tool fee object has no \"price\".");
        return fee;
    }

    public override void Write(Utf8JsonWriter writer, ToolFeeJson value, JsonSerializerOptions options)
    {
        if (value.Unit is null)
        {
            writer.WriteNumberValue(value.Price);
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber("price", value.Price);
        writer.WriteString("unit", value.Unit);
        writer.WriteEndObject();
    }
}
