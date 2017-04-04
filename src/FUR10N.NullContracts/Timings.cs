using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FUR10N.NullContracts
{
    public static class Timings
    {
#if DEBUG && NET46
        private static Dictionary<TimingOperation, Timing> Entries = new Dictionary<TimingOperation, Timing>();

        private static readonly object lockObject = new object();

        private static Timer timer;

        private static bool changed;
#endif

        static Timings()
        {
#if DEBUG && NET46
            //if (System.IO.File.Exists("j:\\timings.txt"))
            //{
            //    var file = System.IO.File.ReadAllText("j:\\timings.txt");
            //    if (!string.IsNullOrEmpty(file))
            //    {
            //        Entries = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<TimingOperation, Timing>>(file);
            //    }
            //}
            timer?.Dispose();
            timer = new Timer(Save, null, TimeSpan.FromMilliseconds(5000), TimeSpan.FromMilliseconds(5000));
#endif
        }

        public static void Update(TimingOperation op, long value)
        {
#if DEBUG && NET46
            lock (lockObject)
            {
                changed = true;
                if (Entries.TryGetValue(op, out var timing))
                {
                    timing.Values.Add(value);
                }
                else
                {
                    Entries.Add(op, new Timing(op, value));
                }
            }
#endif
        }

        public static void Save(object data)
        {
#if DEBUG && NET46
            //if (!changed)
            //{
            //    return;
            //}
            //lock (lockObject)
            //{
            //    if (Entries.Count == 0)
            //    {
            //        return;
            //    }
            //    var file = Newtonsoft.Json.JsonConvert.SerializeObject(Entries);
            //    System.IO.File.WriteAllText("J:\\timings.txt", file);

            //    var msg = string.Join("\n", Entries.Values);
            //    System.IO.File.WriteAllLines("j:\\output.txt", Entries.Select(i => i.Value.ToString()));
            //    changed = false;
            //}
#endif
        }
    }

    public class Timing
    {
        public TimingOperation Operation { get; }

        public List<long> Values { get; } = new List<long>();

        public double Average => Values.Average();

        public long TimeSpent => Values.Aggregate((i, j) => i + j);

        public Timing(TimingOperation op, long value)
        {
            Operation = op;
            Values.Add(value);
        }

        public override string ToString()
        {
            var average = Values.Average();
            return $"{string.Format("{0,-20}", Operation.ToString())}:Time = {TimeSpent}ms, Count = {Values.Count}, Average = {Math.Round(average, 2)}ms";
        }
    }

    public enum TimingOperation
    {
        Total,

        CodeBlockAnlyzer,

        ClassAnalyzer,

        MethodAnlyzer,

        ExpressionToCondition,

        FindBranch,

        IsAlwaysAssigned,

        SymbolLookup
    }
}
