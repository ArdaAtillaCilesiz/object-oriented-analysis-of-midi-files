namespace MidiProject.Classes
{
    public static class Menu
    {
        private const string ReportFile = "analysis.txt";

        public static void Run(MIDIReader reader, TextReader input, TextWriter output)
        {
            output.WriteLine("Welcome to the MIDI Analyzer!");
            output.WriteLine($"File: {reader.FileName}");

            while (true)
            {
                WriteOptions(output);
                output.Write("Enter your choice (or 'exit' to quit): ");

                // Girdi kapanınca ReadLine null döner; çıkılmazsa menü sonsuza kadar döner.
                var choice = input.ReadLine()?.Trim().ToLowerInvariant();
                output.WriteLine();
                if (choice is null or "exit")
                {
                    break;
                }

                switch (choice)
                {
                    case "1":
                        reader.AnalyzeNotes(output);
                        break;
                    case "2":
                        reader.AnalyzeChannels(output);
                        break;
                    case "3":
                        reader.AnalyzeNoteFrequencies(output);
                        break;
                    case "4":
                        reader.AnalyzeTempoAndDynamics(output);
                        break;
                    case "5":
                        reader.AnalyzeTracks(output);
                        break;
                    case "6":
                        SaveReport(reader, output);
                        break;
                    default:
                        output.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }

            output.WriteLine("Thank you for using the MIDI Analyzer!");
        }

        private static void WriteOptions(TextWriter output)
        {
            output.WriteLine();
            output.WriteLine("Please select an analysis type:");
            output.WriteLine("1. Note and Timing Analysis");
            output.WriteLine("2. Channel Analysis");
            output.WriteLine("3. Note Frequency Analysis");
            output.WriteLine("4. Tempo and Dynamics Analysis");
            output.WriteLine("5. Track Analysis");
            output.WriteLine("6. Save All Analyses to File");
        }

        private static void SaveReport(MIDIReader reader, TextWriter output)
        {
            try
            {
                reader.SaveAnalysisToFile(ReportFile);
                output.WriteLine($"Analysis saved to {ReportFile}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                output.WriteLine($"Error saving analysis: {ex.Message}");
            }
        }
    }
}
