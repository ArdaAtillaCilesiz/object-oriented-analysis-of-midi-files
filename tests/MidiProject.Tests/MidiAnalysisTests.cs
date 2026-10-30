using System.Globalization;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiProject.Classes;
using Xunit;

public class MidiAnalysisTests : IDisposable
{
    private const int MenuTimeoutMs = 5000;

    private readonly string _folder = Directory.CreateTempSubdirectory("midi-tests").FullName;

    public MidiAnalysisTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private MIDIReader Load(params TrackChunk[] tracks)
    {
        var path = Path.Combine(_folder, $"{Guid.NewGuid()}.mid");
        new MidiFile(tracks).Write(path);
        return new MIDIReader(path);
    }

    private static MidiEvent[] Played(int pitch, int velocity = 100, int channel = 0)
    {
        return new MidiEvent[]
        {
            new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity) { Channel = (FourBitNumber)channel },
            new NoteOffEvent((SevenBitNumber)pitch, (SevenBitNumber)0) { Channel = (FourBitNumber)channel, DeltaTime = 480 },
        };
    }

    private static TrackChunk Chunk(params MidiEvent[][] events)
    {
        return new TrackChunk(events.SelectMany(e => e));
    }

    private static string Capture(Action<TextWriter> analysis)
    {
        var output = new StringWriter();
        analysis(output);
        return output.ToString();
    }

    [Fact]
    public void Silent_note_on_ends_a_note_instead_of_counting_as_a_new_one()
    {
        var reader = Load(new TrackChunk(
            new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100),
            new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)0) { DeltaTime = 480 }));

        Assert.Contains("Total Notes: 1", Capture(reader.AnalyzeNotes));
        Assert.Contains("C4: 1 time\n", Capture(reader.AnalyzeNoteFrequencies).ReplaceLineEndings("\n"));
        Assert.Contains("Min Velocity: 100", Capture(reader.AnalyzeTempoAndDynamics));
    }

    [Fact]
    public void File_without_notes_is_reported_instead_of_failing()
    {
        var reader = Load(new TrackChunk());

        var report = Capture(reader.Analyze);

        Assert.Contains("Total Notes: 0", report);
        Assert.Contains("No notes in this file.", report);
    }

    [Fact]
    public void Track_without_notes_is_reported()
    {
        var reader = Load(new TrackChunk(new SequenceTrackNameEvent("Tempo Map")));

        Assert.Contains("Tempo Map: no notes", Capture(reader.AnalyzeTracks));
    }

    [Fact]
    public void Default_tempo_is_shown_when_the_file_has_no_tempo_event()
    {
        var reader = Load(Chunk(Played(60)));

        Assert.Contains("Initial: 120.0 BPM", Capture(reader.AnalyzeTempoAndDynamics));
    }

    [Fact]
    public void Tempo_changes_are_listed_with_their_time_and_the_first_one_is_not_repeated()
    {
        var reader = Load(new TrackChunk(
            new SetTempoEvent(300_000),
            new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100),
            new SetTempoEvent(500_000) { DeltaTime = 480 },
            new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0)));

        var report = Capture(reader.AnalyzeTempoAndDynamics);

        Assert.Contains("Initial: 200.0 BPM", report);
        // Varsayılan çözünürlük çeyrek nota başına 96 tick: 480 tick, 200 BPM'de 1,5 saniye eder.
        Assert.Contains("At 0:01.500: 120.0 BPM", report);
        Assert.Equal(1, report.Split("200.0 BPM").Length - 1);
    }

    [Fact]
    public void Note_frequencies_follow_pitch_order()
    {
        var reader = Load(Chunk(Played(72), Played(70), Played(60)));

        var report = Capture(reader.AnalyzeNoteFrequencies);

        Assert.True(report.IndexOf("C4") < report.IndexOf("A#4"));
        Assert.True(report.IndexOf("A#4") < report.IndexOf("C5"));
    }

    [Fact]
    public void Channels_are_numbered_from_one_and_sorted()
    {
        var reader = Load(Chunk(Played(60, channel: 9), Played(62, channel: 0)));

        var report = Capture(reader.AnalyzeChannels);

        Assert.Contains("Total Notes in Channel 1: 1", report);
        Assert.Contains("Total Notes in Channel 10: 1", report);
        Assert.True(report.IndexOf("Channel 1 ") < report.IndexOf("Channel 10 "));
    }

    [Fact]
    public void Notes_show_octave_and_clock_time()
    {
        var reader = Load(Chunk(Played(70)));

        Assert.Contains("Note: A#4, Time: 0:00.000", Capture(reader.AnalyzeNotes));
    }

    [Fact]
    public void Velocity_summary_is_calculated_from_all_notes()
    {
        var reader = Load(Chunk(Played(60, velocity: 80), Played(62, velocity: 100)));

        var report = Capture(reader.AnalyzeTempoAndDynamics);

        Assert.Contains("Average Velocity: 90.00", report);
        Assert.Contains("Max Velocity: 100", report);
        Assert.Contains("Min Velocity: 80", report);
    }

    [Fact]
    public void Each_track_is_analyzed_separately()
    {
        var named = Chunk(new MidiEvent[] { new SequenceTrackNameEvent("Piano") }, Played(60), Played(72));
        var unnamed = Chunk(Played(40, channel: 1));
        var reader = Load(named, unnamed, new TrackChunk());

        var report = Capture(reader.AnalyzeTracks);

        Assert.Equal(new[] { 2, 1, 0 }, reader.Tracks.Select(t => t.NoteCount));
        Assert.Contains("Piano: 2 notes, channels 1, range C4-C5", report);
        Assert.Contains("Track 2: 1 note, channels 2, range E2-E2", report);
        Assert.Contains("Track 3: no notes", report);
    }

    [Fact]
    public void Saved_report_contains_every_section()
    {
        var reader = Load(Chunk(Played(60)));
        var path = Path.Combine(_folder, "report.txt");

        reader.SaveAnalysisToFile(path);

        var report = File.ReadAllText(path);
        foreach (var section in new[] { "Notes (first", "Channel Analysis:", "Note Frequencies:", "Tempo:", "Dynamics:", "Track Analysis:" })
        {
            Assert.Contains(section, report);
        }
    }

    [Fact]
    public async Task Menu_stops_when_input_ends()
    {
        var reader = Load(Chunk(Played(60)));
        var output = new StringWriter();

        var menu = Task.Run(() => Menu.Run(reader, new StringReader(""), output));
        var finished = await Task.WhenAny(menu, Task.Delay(MenuTimeoutMs));

        Assert.Same(menu, finished);
        Assert.Contains("Thank you", output.ToString());
    }

    [Fact]
    public void Menu_keeps_running_after_an_invalid_choice_and_shows_options_again()
    {
        var reader = Load(Chunk(Played(60)));
        var output = new StringWriter();

        Menu.Run(reader, new StringReader("9\n1\nEXIT\n"), output);

        var text = output.ToString();
        Assert.Contains("Invalid choice", text);
        Assert.Contains("Total Notes: 1", text);
        Assert.Equal(3, text.Split("Please select an analysis type:").Length - 1);
    }

    [Fact]
    public void Missing_file_raises_an_error_the_program_can_report()
    {
        Assert.ThrowsAny<IOException>(() => new MIDIReader(Path.Combine(_folder, "missing.mid")));
    }

    [Fact]
    public void Invalid_file_raises_a_midi_error()
    {
        var path = Path.Combine(_folder, "broken.mid");
        File.WriteAllText(path, "this is not a midi file");

        Assert.ThrowsAny<MidiException>(() => new MIDIReader(path));
    }

    [Fact]
    public void Empty_file_raises_the_error_the_program_handles()
    {
        var path = Path.Combine(_folder, "empty.mid");
        File.WriteAllText(path, "");

        Assert.ThrowsAny<ArgumentException>(() => new MIDIReader(path));
    }
}
