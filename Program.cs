using System.Globalization;
using Melanchall.DryWetMidi.Common;
using MidiProject.Classes;

class Program
{
    private const string DefaultFile = "midifiles/example.mid";

    static int Main(string[] args)
    {
        // Ondalık ayraç bilgisayarın diline göre değişmesin.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var filePath = args.Length > 0 ? args[0] : DefaultFile;

        try
        {
            Menu.Run(new MIDIReader(filePath), Console.In, Console.Out);
            return 0;
        }
        // Kütüphane boş dosyada MidiException yerine ArgumentException atıyor.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or MidiException or ArgumentException)
        {
            Console.Error.WriteLine($"Could not read MIDI file '{filePath}': {ex.Message}");
            return 1;
        }
    }
}
