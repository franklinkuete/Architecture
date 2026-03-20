using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Helpers;

public class DateFrUniversalHelper : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(DateOnly) || typeToConvert == typeof(DateTime);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(DateOnly))
            return new InnerDateOnlyConverter();

        if (typeToConvert == typeof(DateTime))
            return new InnerDateTimeConverter();

        throw new NotSupportedException($"Type {typeToConvert} non supporté");
    }

    private class InnerDateOnlyConverter : JsonConverter<DateOnly>
    {
        private const string Format = "dd-MM-yyyy";
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format));
    }

    private class InnerDateTimeConverter : JsonConverter<DateTime>
    {
        private const string FormatDate = "dd-MM-yyyy";
        private const string FormatDateTime = "dd-MM-yyyy HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str!, FormatDateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParseExact(str!, FormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            throw new JsonException($"Format invalide. Attendu : {FormatDate} ou {FormatDateTime}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.TimeOfDay == TimeSpan.Zero)
                writer.WriteStringValue(value.ToString(FormatDate));
            else
                writer.WriteStringValue(value.ToString(FormatDateTime));
        }
    }
}
