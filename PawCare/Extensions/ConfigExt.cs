namespace PawCare.Extensions;

public static class ConfigExt
{
    // Loads KEY=VALUE lines (e.g. Secret.env) into configuration.
    public static void AddSecretsEnv(this IConfigurationBuilder config, string root, string file = "Secret.env")
    {
        var path = Path.Combine(root, file);
        if (!File.Exists(path)) return;

        config.AddInMemoryCollection(
            File.ReadAllLines(path)
                .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
                .Select(l => l.Split('=', 2))
                .ToDictionary(p => p[0].Trim().Replace("__", ":"),
                              p => (string?)p[1].Trim().Trim('"')));
    }
}