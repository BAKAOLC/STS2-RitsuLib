using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Models.Capabilities
{
    internal static class ModelSavedDataJson
    {
        public static readonly JsonSerializerOptions Options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                IncludeFields = true,
                WriteIndented = false,
            };

            foreach (var converter in MegaCritSerializerContext.Default.Options.Converters)
                options.Converters.Add(converter);

            return options;
        }
    }
}
