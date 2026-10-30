using Melanchall.DryWetMidi.Common;
using MusicTheoryNote = Melanchall.DryWetMidi.MusicTheory.Note;

namespace MidiProject.Classes
{
    public abstract class BaseMIDI
    {
        public string FileName { get; }

        protected BaseMIDI(string fileName)
        {
            FileName = fileName;
        }

        public abstract void Analyze(TextWriter output);

        protected static string GetNoteName(SevenBitNumber noteNumber)
        {
            return MusicTheoryNote.Get(noteNumber).ToString();
        }

        // MIDI kanalları dosyada 0-15 olarak saklanır, müzik yazılımları 1-16 gösterir.
        protected static int GetChannelNumber(FourBitNumber channel)
        {
            return channel + 1;
        }
    }
}
