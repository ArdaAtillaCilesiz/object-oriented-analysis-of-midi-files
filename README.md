# Object-Oriented Analysis of MIDI Files

MIDI dosyalarını nesne yönelimli bir yapıyla çözümleyen C# konsol uygulaması. Notaları, kanalları, nota sıklığını, tempo ve dinamikleri, parçaları raporlar. MIDI okuma için [DryWetMIDI](https://github.com/melanchall/drywetmidi) kullanır.

## Gereksinimler

.NET 9 SDK

## Çalıştırma

Depodaki örnek dosyayla:

```bash
dotnet run --project MidiProject.csproj
```

Kendi dosyanla:

```bash
dotnet run --project MidiProject.csproj -- yol/dosya.mid
```

Menüden analiz seçilir, `exit` ile çıkılır. 6. seçenek tüm analizleri çalışılan klasöre `analysis.txt` olarak kaydeder.

| Seçenek | Analiz |
|---|---|
| 1 | İlk 10 nota, zamanları ve toplam nota sayısı |
| 2 | Kanal başına nota sayısı (kanallar müzik yazılımlarındaki gibi 1-16 numaralıdır) |
| 3 | Her perdenin kaç kez çalındığı, perde sırasıyla |
| 4 | Başlangıç temposu, tempo değişimleri, süre, ortalama/en yüksek/en düşük hız (velocity) |
| 5 | Parça başına ad, nota sayısı, kanallar ve perde aralığı |
| 6 | Hepsini dosyaya kaydetme |

## Örnek çıktı

`midifiles/example.mid` için 4. ve 5. seçenekler:

```
Tempo:
Initial: 200.0 BPM
Duration: 1:17.625

Dynamics:
Average Velocity: 104.53
Max Velocity: 110
Min Velocity: 75

Track Analysis:
Piano Template: no notes
Right Hand: 560 notes, channels 1, range F3-C6
Left Hand: 698 notes, channels 2, range D1-F3
Staff-1: 195 notes, channels 3, range A3-C6
```

## Nesne yönelimli yapı

| Sınıf | Görevi |
|---|---|
| `BaseMIDI` | Soyut temel sınıf. Dosya adını tutar, `Analyze(TextWriter)` metodunu zorunlu kılar |
| `MIDIReader` | Dosyayı bir kez okur, dosya geneli analizleri yapar, parçalarını `Track` nesneleri olarak tutar |
| `Track` | Tek parçanın analizini yapar |
| `Menu` | Kullanıcıyla konuşan menü döngüsü |

`MIDIReader` ve `Track`, `BaseMIDI`'den türer ve `Analyze` metodunu kendine göre yazar. `MIDIReader.Analyze`, parçaların `Analyze` metodunu çağırır. Analizler bir `TextWriter`'a yazdığı için ekran çıktısı ve dosya raporu aynı kodu kullanır.

## Testler

```bash
dotnet test MidiProject.sln
```
