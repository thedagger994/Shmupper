using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shmupper
{
    [Serializable]
    public class ScoreEntry
    {
        public string Name = "AAA";
        public int Score;
        public int Floor = 1;
        public int Threat;
        public string Date = "";
    }

    [Serializable]
    public class ScoreTable
    {
        public List<ScoreEntry> Entries = new List<ScoreEntry>();
    }

    /// The cabinet's memory. Ten places, a record holder, and enough seeded names that a fresh
    /// install still shows a full board - an empty top ten reads as broken rather than new.
    public static class SaveSystem
    {
        public const int TableSize = 10;

        static ScoreTable _table;
        static string FilePath => Path.Combine(Application.persistentDataPath, "shmupper_scores.json");

        public static ScoreTable Table
        {
            get
            {
                if (_table == null) Load();
                return _table;
            }
        }

        public static int Record => Table.Entries.Count > 0 ? Table.Entries[0].Score : 0;

        public static string RecordHolder => Table.Entries.Count > 0 ? Table.Entries[0].Name : "---";

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    _table = JsonUtility.FromJson<ScoreTable>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Shmupper: could not read the score table (" + e.Message + "). Starting a fresh one.");
                _table = null;
            }

            if (_table == null || _table.Entries == null || _table.Entries.Count == 0)
            {
                _table = SeedTable();
                Save();
            }

            Sort();
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_table, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Shmupper: could not write the score table (" + e.Message + ").");
            }
        }

        public static bool Qualifies(int score)
        {
            if (score <= 0) return false;
            if (Table.Entries.Count < TableSize) return true;
            return score > Table.Entries[Table.Entries.Count - 1].Score;
        }

        public static int Submit(string name, int score, int floor, int threat)
        {
            var entry = new ScoreEntry
            {
                Name = Sanitise(name),
                Score = score,
                Floor = floor,
                Threat = threat,
                Date = DateTime.Now.ToString("yyyy-MM-dd")
            };

            Table.Entries.Add(entry);
            Sort();

            while (Table.Entries.Count > TableSize) Table.Entries.RemoveAt(Table.Entries.Count - 1);
            Save();

            return Table.Entries.IndexOf(entry);
        }

        public static string Sanitise(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "AAA";

            name = name.ToUpperInvariant();
            var chars = new char[3];

            for (int i = 0; i < 3; i++)
            {
                char c = i < name.Length ? name[i] : 'A';
                chars[i] = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ? c : 'A';
            }

            return new string(chars);
        }

        static void Sort()
        {
            Table.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        }

        static ScoreTable SeedTable()
        {
            var table = new ScoreTable();
            string[] names = { "GUN", "MAG", "EMB", "RUN", "HEX", "ASH", "VOW", "ORB", "PYR", "KEY" };
            int[] scores = { 92000, 78500, 65000, 54000, 44500, 36000, 28500, 21000, 14500, 8000 };
            int[] floors = { 9, 8, 7, 6, 5, 5, 4, 3, 2, 2 };

            for (int i = 0; i < TableSize; i++)
            {
                table.Entries.Add(new ScoreEntry
                {
                    Name = names[i],
                    Score = scores[i],
                    Floor = floors[i],
                    Threat = 14 - i,
                    Date = "--"
                });
            }

            return table;
        }
    }
}
