using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using InteractionNote = Melanchall.DryWetMidi.Interaction.Note;

namespace MidiProject.Classes
{
    public class Track : BaseMIDI
    {
        private readonly List<InteractionNote> _notes;

        public string TrackName { get; }
        public int NoteCount => _notes.Count;

        public Track(string fileName, int trackNumber, TrackChunk chunk) : base(fileName)
        {
            var name = chunk.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text;
            TrackName = string.IsNullOrWhiteSpace(name) ? $"Track {trackNumber}" : name.Trim();
            _notes = chunk.GetNotes().ToList();
        }

        public override void Analyze(TextWriter output)
        {
            if (_notes.Count == 0)
            {
                output.WriteLine($"{TrackName}: no notes");
                return;
            }

            var channels = _notes.Select(n => GetChannelNumber(n.Channel)).Distinct().Order();
            var lowest = GetNoteName(_notes.Min(n => n.NoteNumber));
            var highest = GetNoteName(_notes.Max(n => n.NoteNumber));

            var noteLabel = _notes.Count == 1 ? "note" : "notes";
            output.WriteLine($"{TrackName}: {_notes.Count} {noteLabel}, channels {string.Join(", ", channels)}, range {lowest}-{highest}");
        }
    }
}
