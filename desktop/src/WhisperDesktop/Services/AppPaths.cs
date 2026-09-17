namespace WhisperDesktop.Services;

public static class AppPaths
{
    public static string AppDataRoot { get; } =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhisperDesktop");

    public static string ModelsDirectory { get; } = System.IO.Path.Combine(AppDataRoot, "models");

    public static string TempDirectory { get; } = System.IO.Path.Combine(AppDataRoot, "temp");

    public static string DefaultOutputDirectory { get; } =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Transcripciones");

    public static void EnsureDirectories()
    {
        System.IO.Directory.CreateDirectory(ModelsDirectory);
        System.IO.Directory.CreateDirectory(TempDirectory);
        System.IO.Directory.CreateDirectory(DefaultOutputDirectory);
    }
}
