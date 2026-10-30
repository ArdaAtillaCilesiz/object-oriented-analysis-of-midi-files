using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using InteractionNote = Melanchall.DryWetMidi.Interaction.Note;

namespace MidiProject.Classes
{
    public class MIDIReader : BaseMIDI
    {
        private const int PreviewCount = 10;

        private readonly MidiFile _midiFile;
        private readonly TempoMap _tempoMap;
        private readonly List<InteractionNote> _notes;

        public IReadOnlyList<Track> Tracks { get; }

        public MIDIReader(string fileName) : base(fileName)
        {
            _midiFile = MidiFile.Read(fileName);
            _tempoMap = _midiFile.GetTempoMap();
            _notes = _midiFile.GetNotes().ToList();
            Tracks = _midiFile.GetTrackChunks()
                .Select((chunk, index) => new Track(fileName, index + 1, chunk))
                .ToList();
        }

        public override void Analyze(TextWriter output)
        {
            AnalyzeNotes(output);
            output.WriteLine();
            AnalyzeChannels(output);
            output.WriteLine();
            AnalyzeNoteFrequencies(output);
            output.WriteLine();
            AnalyzeTempoAndDynamics(output);
            output.WriteLine();
            AnalyzeTracks(output);
        }

        public void AnalyzeNotes(TextWriter output)
        {
            output.WriteLine($"Notes (first {PreviewCount}):");
            foreach (var note in _notes.Take(PreviewCount))
            {
                output.WriteLine($"Note: {GetNoteName(note.NoteNumber)}, Time: {FormatTime(note.Time)}");
            }

            output.WriteLine($"Total Notes: {_notes.Count}");
        }

        public void AnalyzeChannels(TextWriter output)
        {
            output.WriteLine("Channel Analysis:");
            foreach (var channel in _notes.GroupBy(n => GetChannelNumber(n.Channel)).OrderBy(g => g.Key))
            {
                var preview = channel.Take(PreviewCount).Select(n => GetNoteName(n.NoteNumber));
                output.WriteLine($"Channel {channel.Key} (first {PreviewCount} notes): {string.Join(", ", preview)}");
                output.WriteLine($"Total Notes in Channel {channel.Key}: {channel.Count()}");
            }
        }

        public void AnalyzeNoteFrequencies(TextWriter output)
        {
            output.WriteLine("Note Frequencies:");
            foreach (var pitch in _notes.GroupBy(n => n.NoteNumber).OrderBy(g => g.Key))
            {
                var count = pitch.Count();
                output.WriteLine($"{GetNoteName(pitch.Key)}: {count} {(count == 1 ? "time" : "times")}");
            }
        }

        public void AnalyzeTempoAndDynamics(TextWriter output)
        {
            output.WriteLine("Tempo:");
            output.WriteLine($"Initial: {FormatTempo(_tempoMap.GetTempoAtTime(new MidiTimeSpan(0)))}");
            foreach (var change in _tempoMap.GetTempoChanges().Where(c => c.Time > 0))
            {
                output.WriteLine($"At {FormatTime(change.Time)}: {FormatTempo(change.Value)}");
            }

            output.WriteLine($"Duration: {FormatTime((TimeSpan)_midiFile.GetDuration<MetricTimeSpan>())}");

            output.WriteLine();
            output.WriteLine("Dynamics:");
            if (_notes.Count == 0)
            {
                output.WriteLine("No notes in this file.");
                return;
            }

            var velocities = _notes.Select(n => (int)n.Velocity).ToList();
            output.WriteLine($"Average Velocity: {velocities.Average():F2}");
            output.WriteLine($"Max Velocity: {velocities.Max()}");
            output.WriteLine($"Min Velocity: {velocities.Min()}");
        }

        public void AnalyzeTracks(TextWriter output)
        {
            output.WriteLine("Track Analysis:");
            if (Tracks.Count == 0)
            {
                output.WriteLine("No tracks in this file.");
            }

            foreach (var track in Tracks)
            {
                track.Analyze(output);
            }
        }

        public void SaveAnalysisToFile(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine($"Analysis of MIDI file: {FileName}");
            writer.WriteLine();
            Analyze(writer);
        }

        private string FormatTime(long ticks)
        {
            return FormatTime((TimeSpan)TimeConverter.ConvertTo<MetricTimeSpan>(ticks, _tempoMap));
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}.{time.Milliseconds:000}";
        }

        private static string FormatTempo(Tempo tempo)
        {
            return $"{tempo.BeatsPerMinute:F1} BPM";
        }
    }
}
